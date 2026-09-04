using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using HttpsMessenger.Models;
using HttpsMessenger.Services.Mes;

namespace HttpsMessenger.Services;

/// <summary>
/// 基于 TcpListener + SslStream 的简易 HTTPS 消息服务端。
/// 支持接收 POST /api/message 与 GET /api/ping，并按 SQLite 规则生成响应字段。
/// </summary>
public sealed class HttpsChatServer : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private X509Certificate2? _certificate;
    private Task? _acceptLoop;
    private Func<IReadOnlyList<FieldRule>>? _ruleProvider;

    public bool IsRunning { get; private set; }
    public int Port { get; private set; }
    public string DisplayName { get; set; } = Environment.MachineName;

    public event Action<ChatMessage>? MessageReceived;
    public event Action<string>? StatusChanged;
    public event Action<Exception>? ErrorOccurred;
    public event Action<CommunicationTrace>? TraceCompleted;

    public void SetRuleProvider(Func<IReadOnlyList<FieldRule>> provider)
        => _ruleProvider = provider;

    public void Start(int port, X509Certificate2 certificate)
    {
        if (IsRunning)
        {
            throw new InvalidOperationException("服务已在运行中。");
        }

        if (port is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port), "端口必须在 1~65535 之间。");
        }

        _certificate = certificate ?? throw new ArgumentNullException(nameof(certificate));
        Port = port;
        _cts = new CancellationTokenSource();
        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();
        IsRunning = true;
        RaiseStatus($"HTTPS 服务已启动：https://0.0.0.0:{port}/");
        _acceptLoop = Task.Run(() => AcceptLoopAsync(_cts.Token));
    }

    public void Stop()
    {
        if (!IsRunning)
        {
            return;
        }

        try
        {
            _cts?.Cancel();
            _listener?.Stop();
            _acceptLoop?.Wait(TimeSpan.FromSeconds(2));
        }
        catch
        {
            // 停止阶段忽略次要异常
        }
        finally
        {
            IsRunning = false;
            RaiseStatus("HTTPS 服务已停止。");
        }
    }

    private async Task AcceptLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var client = await _listener!.AcceptTcpClientAsync(token).ConfigureAwait(false);
                _ = Task.Run(() => HandleClientAsync(client, token), token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(ex);
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken token)
    {
        var remote = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
        var trace = new CommunicationTrace
        {
            Direction = "Server",
            Title = $"接收来自 {remote} 的 HTTPS 请求",
            StartedAt = DateTime.Now
        };

        using (client)
        {
            try
            {
                trace.AddStep("TCP 接受连接", $"已接受客户端连接：{remote}");
                await using var network = client.GetStream();
                await using var ssl = new SslStream(network, leaveInnerStreamOpen: false);

                trace.AddStep("TLS 握手开始", "执行 AuthenticateAsServer（TLS 1.2/1.3）");
                await ssl.AuthenticateAsServerAsync(
                    new SslServerAuthenticationOptions
                    {
                        ServerCertificate = _certificate!,
                        EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                        ClientCertificateRequired = false
                    },
                    token).ConfigureAwait(false);

                trace.AddStep("TLS 握手完成",
                    $"协议={ssl.SslProtocol}，证书主题={_certificate!.Subject}");

                var (method, path, headers, body) = await ReadHttpRequestAsync(ssl, token).ConfigureAwait(false);
                if (method is null || path is null)
                {
                    trace.AddStep("解析请求失败", "请求行无效");
                    await WriteHttpAsync(ssl, 400, "Bad Request", """{"success":false,"message":"无效请求"}""", token)
                        .ConfigureAwait(false);
                    trace.Success = false;
                    trace.Error = "无效请求";
                    trace.ResponseStatusCode = 400;
                    return;
                }

                trace.RequestMethod = method;
                trace.RequestUrl = path;
                trace.RequestHeaders = string.Join(Environment.NewLine, headers.Select(kv => $"{kv.Key}: {kv.Value}"));
                trace.RequestBody = body;
                trace.AddStep("读取 HTTP 请求", $"{method} {path}，Body 长度={Encoding.UTF8.GetByteCount(body)} 字节");

                await DispatchAsync(ssl, method, path, body, headers, trace, token).ConfigureAwait(false);
                trace.Success = true;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                trace.Success = false;
                trace.Error = ex.Message;
                trace.AddStep("异常", ex.Message);
                ErrorOccurred?.Invoke(ex);
            }
            finally
            {
                trace.FinishedAt = DateTime.Now;
                TraceCompleted?.Invoke(trace);
            }
        }
    }

    private static async Task<(string? Method, string? Path, Dictionary<string, string> Headers, string Body)>
        ReadHttpRequestAsync(SslStream ssl, CancellationToken token)
    {
        var buffer = new MemoryStream();
        var chunk = new byte[4096];
        var headerEnd = -1;

        while (headerEnd < 0)
        {
            var n = await ssl.ReadAsync(chunk.AsMemory(0, chunk.Length), token).ConfigureAwait(false);
            if (n == 0)
            {
                break;
            }

            buffer.Write(chunk, 0, n);
            headerEnd = IndexOfHeaderEnd(buffer.GetBuffer(), (int)buffer.Length);
            if (buffer.Length > 1024 * 1024)
            {
                throw new InvalidOperationException("请求头过大。");
            }
        }

        if (headerEnd < 0)
        {
            return (null, null, new Dictionary<string, string>(), string.Empty);
        }

        var raw = buffer.GetBuffer();
        var headerText = Encoding.ASCII.GetString(raw, 0, headerEnd);
        var remaining = (int)buffer.Length - (headerEnd + 4);

        var lines = headerText.Split(new[] { "\r\n" }, StringSplitOptions.None);
        if (lines.Length == 0)
        {
            return (null, null, new Dictionary<string, string>(), string.Empty);
        }

        var requestParts = lines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (requestParts.Length < 2)
        {
            return (null, null, new Dictionary<string, string>(), string.Empty);
        }

        var method = requestParts[0].ToUpperInvariant();
        var path = requestParts[1];
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            var idx = line.IndexOf(':');
            if (idx > 0)
            {
                headers[line[..idx].Trim()] = line[(idx + 1)..].Trim();
            }
        }

        var contentLength = 0;
        if (headers.TryGetValue("Content-Length", out var lengthText))
        {
            _ = int.TryParse(lengthText, out contentLength);
        }

        var bodyBytes = new byte[Math.Max(contentLength, 0)];
        var copied = 0;
        if (remaining > 0 && contentLength > 0)
        {
            copied = Math.Min(remaining, contentLength);
            Buffer.BlockCopy(raw, headerEnd + 4, bodyBytes, 0, copied);
        }

        while (copied < contentLength)
        {
            var n = await ssl.ReadAsync(bodyBytes.AsMemory(copied, contentLength - copied), token)
                .ConfigureAwait(false);
            if (n == 0)
            {
                break;
            }

            copied += n;
        }

        var body = contentLength > 0 ? Encoding.UTF8.GetString(bodyBytes, 0, copied) : string.Empty;
        return (method, path, headers, body);
    }

    private static int IndexOfHeaderEnd(byte[] data, int length)
    {
        for (var i = 0; i < length - 3; i++)
        {
            if (data[i] == (byte)'\r' && data[i + 1] == (byte)'\n'
                && data[i + 2] == (byte)'\r' && data[i + 3] == (byte)'\n')
            {
                return i;
            }
        }

        return -1;
    }

    private async Task DispatchAsync(
        SslStream ssl,
        string method,
        string path,
        string body,
        Dictionary<string, string> headers,
        CommunicationTrace trace,
        CancellationToken token)
    {
        if (method == "GET" && path.StartsWith("/api/ping", StringComparison.OrdinalIgnoreCase))
        {
            var ping = new ApiResponse
            {
                Success = true,
                Message = $"pong from {DisplayName}",
                ServerTime = DateTime.Now
            };
            var json = JsonSerializer.Serialize(ping, JsonOptions);
            trace.AddStep("处理接口", "匹配 GET /api/ping");
            await WriteHttpAsync(ssl, 200, "OK", json, token).ConfigureAwait(false);
            trace.ResponseStatusCode = 200;
            trace.ResponseBody = json;
            trace.AddStep("返回响应", "HTTP 200 + JSON pong");
            return;
        }

        if (method == "GET" && (path == "/" || path.StartsWith("/index", StringComparison.OrdinalIgnoreCase)))
        {
            var html =
                $"<html><body><h1>HttpsMessenger</h1><p>服务运行中，本机名称：{WebUtility.HtmlEncode(DisplayName)}</p></body></html>";
            await WriteHttpAsync(ssl, 200, "OK", html, token, "text/html; charset=utf-8").ConfigureAwait(false);
            trace.ResponseStatusCode = 200;
            trace.ResponseBody = html;
            trace.AddStep("返回主页", "HTTP 200 text/html");
            return;
        }

        if (method == "POST" && path.StartsWith("/api/message", StringComparison.OrdinalIgnoreCase))
        {
            ChatMessage? message;
            try
            {
                message = JsonSerializer.Deserialize<ChatMessage>(body, JsonOptions);
                trace.AddStep("解析 JSON", "请求体反序列化为 ChatMessage 成功");
            }
            catch (Exception ex)
            {
                var bad = new ApiResponse { Success = false, Message = "JSON 解析失败：" + ex.Message };
                var badJson = JsonSerializer.Serialize(bad, JsonOptions);
                await WriteHttpAsync(ssl, 400, "Bad Request", badJson, token).ConfigureAwait(false);
                trace.ResponseStatusCode = 400;
                trace.ResponseBody = badJson;
                trace.AddStep("解析失败", ex.Message);
                return;
            }

            if (message is null || string.IsNullOrWhiteSpace(message.Content))
            {
                var empty = new ApiResponse { Success = false, Message = "消息内容不能为空" };
                var emptyJson = JsonSerializer.Serialize(empty, JsonOptions);
                await WriteHttpAsync(ssl, 400, "Bad Request", emptyJson, token).ConfigureAwait(false);
                trace.ResponseStatusCode = 400;
                trace.ResponseBody = emptyJson;
                return;
            }

            if (string.IsNullOrWhiteSpace(message.Sender))
            {
                message.Sender = "远程客户端";
            }

            if (message.Timestamp == default)
            {
                message.Timestamp = DateTime.Now;
            }

            MessageReceived?.Invoke(message);

            var rules = _ruleProvider?.Invoke() ?? Array.Empty<FieldRule>();
            trace.AddStep("加载规则", $"从 SQLite 加载启用规则 {rules.Count(r => r.IsEnabled)} 条");

            var (apiResponse, status, matched) = FieldRuleMatcher.Match(body, message, rules);
            trace.AddStep("规则匹配",
                matched.Count == 0
                    ? "未匹配到自定义字段规则，返回默认响应"
                    : $"匹配规则：{string.Join("、", matched.Select(m => m.Name))}");

            var json = JsonSerializer.Serialize(apiResponse, JsonOptions);
            var reason = status == 200 ? "OK" : "Processed";
            await WriteHttpAsync(ssl, status, reason, json, token).ConfigureAwait(false);
            trace.ResponseStatusCode = status;
            trace.ResponseBody = json;
            trace.ResponseHeaders = $"Content-Type: application/json; charset=utf-8{Environment.NewLine}Content-Length: {Encoding.UTF8.GetByteCount(json)}";
            trace.AddStep("返回 HTTPS 响应", $"HTTP {status}，自定义字段数={apiResponse.CustomFields.Count}");
            return;
        }

        // MES 模拟接口（POST form-urlencoded jsonData）
        if (method == "POST" && MesMockHandler.TryHandle(method, path, body, out var mesJson, out var mesStatus))
        {
            trace.AddStep("MES 模拟接口", $"{path} jsonData 表单处理");
            await WriteHttpAsync(ssl, mesStatus, mesStatus == 200 ? "OK" : "Error", mesJson, token)
                .ConfigureAwait(false);
            trace.ResponseStatusCode = mesStatus;
            trace.ResponseBody = mesJson;
            trace.AddStep("返回 MES JSON", $"HTTP {mesStatus}");
            return;
        }

        var notFound = new ApiResponse
        {
            Success = false,
            Message = $"未找到接口：{method} {path}"
        };
        var nfJson = JsonSerializer.Serialize(notFound, JsonOptions);
        await WriteHttpAsync(ssl, 404, "Not Found", nfJson, token).ConfigureAwait(false);
        trace.ResponseStatusCode = 404;
        trace.ResponseBody = nfJson;
        trace.AddStep("未匹配路由", $"{method} {path}");
    }

    private static async Task WriteHttpAsync(
        SslStream ssl,
        int statusCode,
        string reason,
        string body,
        CancellationToken token,
        string contentType = "application/json; charset=utf-8")
    {
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        var header =
            $"HTTP/1.1 {statusCode} {reason}\r\n" +
            $"Content-Type: {contentType}\r\n" +
            $"Content-Length: {bodyBytes.Length}\r\n" +
            "Connection: close\r\n" +
            "Access-Control-Allow-Origin: *\r\n" +
            "\r\n";

        var headerBytes = Encoding.ASCII.GetBytes(header);
        await ssl.WriteAsync(headerBytes, token).ConfigureAwait(false);
        await ssl.WriteAsync(bodyBytes, token).ConfigureAwait(false);
        await ssl.FlushAsync(token).ConfigureAwait(false);
    }

    private void RaiseStatus(string text) => StatusChanged?.Invoke(text);

    public void Dispose()
    {
        Stop();
        _cts?.Dispose();
        _certificate = null;
    }
}
