using System.Net.Security;
using System.Text;
using System.Text.Json;
using HttpsMessenger.Models;

namespace HttpsMessenger.Services;

/// <summary>
/// HTTPS 客户端：发送请求、测试连通，并记录完整通讯过程。
/// </summary>
public sealed class HttpsChatClient : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private HttpClient? _httpClient;
    private bool _ignoreCertificateErrors = true;

    public bool IgnoreCertificateErrors
    {
        get => _ignoreCertificateErrors;
        set
        {
            if (_ignoreCertificateErrors == value && _httpClient is not null)
            {
                return;
            }

            _ignoreCertificateErrors = value;
            RecreateClient();
        }
    }

    public HttpsChatClient()
    {
        RecreateClient();
    }

    private void RecreateClient()
    {
        _httpClient?.Dispose();

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (request, cert, chain, errors) =>
            {
                if (_ignoreCertificateErrors)
                {
                    return true;
                }

                return errors == SslPolicyErrors.None;
            }
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(20)
        };
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("HttpsMessenger/1.0");
    }

    public async Task<(ApiResponse Response, CommunicationTrace Trace)> SendMessageWithTraceAsync(
        string baseUrl,
        ChatMessage message,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        var url = BuildUrl(baseUrl, "/api/message");
        var json = JsonSerializer.Serialize(message, JsonOptions);

        var trace = new CommunicationTrace
        {
            Direction = "Client",
            Title = $"发送 HTTPS 消息到 {url}",
            StartedAt = DateTime.Now,
            RequestMethod = "POST",
            RequestUrl = url,
            RequestHeaders = "Content-Type: application/json; charset=utf-8\r\nUser-Agent: HttpsMessenger/1.0",
            RequestBody = json
        };

        try
        {
            trace.AddStep("准备请求", "构造 ChatMessage JSON 请求体");
            trace.AddStep("解析目标地址", url);
            trace.AddStep("建立 TCP 连接", "HttpClient 开始连接远端主机");
            trace.AddStep("TLS 握手", _ignoreCertificateErrors
                ? "启用 TLS，并忽略自签名证书错误（开发模式）"
                : "启用 TLS，并严格校验服务器证书");

            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            trace.AddStep("发送 HTTP 请求", "POST /api/message");

            using var response = await _httpClient!.PostAsync(url, content, token).ConfigureAwait(false);
            var text = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

            trace.ResponseStatusCode = (int)response.StatusCode;
            trace.ResponseHeaders = string.Join(Environment.NewLine,
                response.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}")
                    .Concat(response.Content.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}")));
            trace.ResponseBody = text;
            trace.AddStep("接收 HTTP 响应", $"状态码={(int)response.StatusCode} {response.ReasonPhrase}");
            trace.AddStep("读取响应正文", $"长度={Encoding.UTF8.GetByteCount(text)} 字节");

            if (!response.IsSuccessStatusCode)
            {
                trace.Success = false;
                trace.Error = $"HTTP {(int)response.StatusCode}";
                trace.FinishedAt = DateTime.Now;
                return (new ApiResponse
                {
                    Success = false,
                    Message = $"HTTP {(int)response.StatusCode}: {Truncate(text, 300)}"
                }, trace);
            }

            try
            {
                var api = JsonSerializer.Deserialize<ApiResponse>(text, JsonOptions)
                          ?? new ApiResponse { Success = false, Message = "空响应" };
                trace.AddStep("解析响应 JSON",
                    api.CustomFields.Count > 0
                        ? $"成功；自定义字段：{string.Join(", ", api.CustomFields.Select(kv => kv.Key + "=" + kv.Value))}"
                        : "成功；无自定义响应字段");
                trace.Success = api.Success;
                trace.FinishedAt = DateTime.Now;
                return (api, trace);
            }
            catch (Exception ex)
            {
                trace.Success = false;
                trace.Error = "响应解析失败：" + ex.Message;
                trace.AddStep("解析失败", ex.Message);
                trace.FinishedAt = DateTime.Now;
                return (new ApiResponse
                {
                    Success = false,
                    Message = $"响应解析失败：{ex.Message}"
                }, trace);
            }
        }
        catch (Exception ex)
        {
            trace.Success = false;
            trace.Error = ex.Message;
            trace.AddStep("通讯异常", ex.Message);
            trace.FinishedAt = DateTime.Now;
            return (new ApiResponse { Success = false, Message = ex.Message }, trace);
        }
    }

    public async Task<(ApiResponse Response, CommunicationTrace Trace)> PingWithTraceAsync(
        string baseUrl,
        CancellationToken token = default)
    {
        var url = BuildUrl(baseUrl, "/api/ping");
        var trace = new CommunicationTrace
        {
            Direction = "Client",
            Title = $"HTTPS 连通测试 {url}",
            StartedAt = DateTime.Now,
            RequestMethod = "GET",
            RequestUrl = url,
            RequestHeaders = "User-Agent: HttpsMessenger/1.0",
            RequestBody = "(无请求体)"
        };

        try
        {
            trace.AddStep("解析目标地址", url);
            trace.AddStep("TCP + TLS", "建立安全连接");
            trace.AddStep("发送请求", "GET /api/ping");

            using var response = await _httpClient!.GetAsync(url, token).ConfigureAwait(false);
            var text = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
            trace.ResponseStatusCode = (int)response.StatusCode;
            trace.ResponseBody = text;
            trace.AddStep("收到响应", $"HTTP {(int)response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                trace.Success = false;
                trace.Error = $"HTTP {(int)response.StatusCode}";
                trace.FinishedAt = DateTime.Now;
                return (new ApiResponse { Success = false, Message = $"连通失败 HTTP {(int)response.StatusCode}" }, trace);
            }

            ApiResponse api;
            try
            {
                api = JsonSerializer.Deserialize<ApiResponse>(text, JsonOptions)
                      ?? new ApiResponse { Success = true, Message = text };
            }
            catch
            {
                api = new ApiResponse { Success = true, Message = text };
            }

            trace.Success = true;
            trace.FinishedAt = DateTime.Now;
            return (api, trace);
        }
        catch (Exception ex)
        {
            trace.Success = false;
            trace.Error = ex.Message;
            trace.AddStep("异常", ex.Message);
            trace.FinishedAt = DateTime.Now;
            return (new ApiResponse { Success = false, Message = ex.Message }, trace);
        }
    }

    public static string BuildUrl(string baseUrl, string path)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new ArgumentException("对端地址不能为空。", nameof(baseUrl));
        }

        baseUrl = baseUrl.Trim().TrimEnd('/');
        if (!baseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            && !baseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            baseUrl = "https://" + baseUrl;
        }

        return baseUrl + path;
    }

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max] + "...";

    public void Dispose() => _httpClient?.Dispose();
}
