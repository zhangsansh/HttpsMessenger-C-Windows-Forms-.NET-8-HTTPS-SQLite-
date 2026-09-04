using System.Text;
using System.Text.Json;
using HttpsMessenger.Localization;
using HttpsMessenger.Models;
using HttpsMessenger.Services;

namespace HttpsMessenger;

public partial class MainForm : Form
{
    private readonly HttpsChatServer _server = new();
    private readonly HttpsChatClient _client = new();
    private readonly SqliteStore _store = new();
    private readonly AppLogger _logger = new();
    private readonly List<string> _sendLines = new();
    private readonly List<string> _recvLines = new();
    private readonly object _msgLock = new();
    private readonly List<(long Id, string ProcessText)> _historyIndex = new();

    private X509Certificate2Wrapper? _cert;
    private int _sendCount;
    private int _recvCount;

    private static readonly JsonSerializerOptions PrettyJson = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public MainForm()
    {
        Lang.LoadPreference();
        InitializeComponent();
        WireEvents();
        LoadDefaults();
        ApplyUiLanguage();
        Lang.LanguageChanged += () =>
        {
            if (IsDisposed)
            {
                return;
            }

            if (InvokeRequired)
            {
                BeginInvoke(ApplyUiLanguage);
            }
            else
            {
                ApplyUiLanguage();
            }
        };
    }

    private void WireEvents()
    {
        _server.SetRuleProvider(() => _store.GetEnabledRules());
        _server.MessageReceived += OnServerMessageReceived;
        _server.StatusChanged += text => AppendSystem(text);
        _server.ErrorOccurred += ex => AppendSystem("Server error: " + ex.Message, isError: true);
        _server.TraceCompleted += OnServerTraceCompleted;
        _logger.EntryAdded += OnLogEntryAdded;

        btnStartServer.Click += async (_, _) => await StartServerAsync();
        btnStopServer.Click += (_, _) => StopServer();
        btnSend.Click += async (_, _) => await SendMessageAsync();
        btnPing.Click += async (_, _) => await PingPeerAsync();
        btnClearSend.Click += (_, _) => ClearSendWindow();
        btnClearRecv.Click += (_, _) => ClearRecvWindow();
        btnExportMessages.Click += (_, _) => ExportMessages();
        btnRegenCert.Click += (_, _) => RegenerateCertificate();
        btnOpenHelp.Click += (_, _) => OpenHelp();
        menuOpenHelp.Click += (_, _) => OpenHelp();
        menuAbout.Click += (_, _) => ShowAbout();
        menuLangZh.Click += (_, _) => Lang.SetLanguage(AppLanguage.Chinese);
        menuLangEn.Click += (_, _) => Lang.SetLanguage(AppLanguage.English);
        menuLangBi.Click += (_, _) => Lang.SetLanguage(AppLanguage.Bilingual);

        btnRuleAdd.Click += (_, _) => AddRule();
        btnRuleEdit.Click += (_, _) => EditSelectedRule();
        btnRuleDelete.Click += (_, _) => DeleteSelectedRule();
        btnRuleRefresh.Click += (_, _) => RefreshRulesGrid();
        dgvRules.CellDoubleClick += (_, _) => EditSelectedRule();

        btnHistoryRefresh.Click += (_, _) => RefreshHistory();
        btnHistoryClear.Click += (_, _) => ClearHistory();
        lstHistory.SelectedIndexChanged += (_, _) => ShowSelectedHistory();

        cmbLogLevel.SelectedIndexChanged += (_, _) => RefreshLogView();
        btnLogRefresh.Click += (_, _) => RefreshLogView();
        btnLogClear.Click += (_, _) => ClearMemoryLogs();
        btnLogExport.Click += (_, _) => ExportRuntimeLog();
        btnLogOpenFolder.Click += (_, _) =>
        {
            try
            {
                _logger.OpenLogFolder();
            }
            catch (Exception ex)
            {
                MessageBox.Show(Lang.T("OpenLogFailed") + ex.Message, Lang.T("Error"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        txtInput.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.SuppressKeyPress = true;
                await SendMessageAsync();
            }
        };

        // 窗口尺寸变化时微调分隔条比例，避免过小面板
        Resize += (_, _) => EnsureSplitters();
        Shown += (_, _) => EnsureSplitters();

        FormClosing += (_, _) =>
        {
            _logger.Info("Application exit");
            StopServer();
            _client.Dispose();
            _server.Dispose();
            _cert?.Dispose();
        };
    }

    private void EnsureSplitters()
    {
        try
        {
            if (splitBottom.Width > 200)
            {
                splitBottom.SplitterDistance = Math.Clamp(splitBottom.SplitterDistance, 300, splitBottom.Width - 180);
            }

            if (splitMessages.Width > 200)
            {
                splitMessages.SplitterDistance = Math.Clamp(splitMessages.SplitterDistance, 180, splitMessages.Width - 180);
            }
        }
        catch
        {
            // ignore during layout
        }
    }

    private void ApplyUiLanguage()
    {
        Text = Lang.T("AppTitle");
        menuLanguage.Text = Lang.T("MenuLanguage");
        menuLangZh.Text = Lang.T("LangZh");
        menuLangEn.Text = Lang.T("LangEn");
        menuLangBi.Text = Lang.T("LangBi");
        menuLangZh.Checked = Lang.Current == AppLanguage.Chinese;
        menuLangEn.Checked = Lang.Current == AppLanguage.English;
        menuLangBi.Checked = Lang.Current == AppLanguage.Bilingual;
        menuHelp.Text = Lang.T("MenuHelp");
        menuOpenHelp.Text = Lang.T("MenuOpenHelp");
        menuAbout.Text = Lang.T("MenuAbout");

        tabComm.Text = Lang.T("TabComm");
        tabRules.Text = Lang.T("TabRules");
        tabHistory.Text = Lang.T("TabHistory");
        tabLog.Text = Lang.T("TabLog");

        grpServer.Text = Lang.T("GrpServer");
        lblLocalName.Text = Lang.T("LocalName");
        lblListenPort.Text = Lang.T("Port");
        btnStartServer.Text = Lang.T("StartServer");
        btnStopServer.Text = Lang.T("StopServer");
        btnRegenCert.Text = Lang.T("RegenCert");

        grpPeer.Text = Lang.T("GrpPeer");
        lblPeerUrl.Text = Lang.T("PeerUrl");
        btnPing.Text = Lang.T("Ping");
        chkIgnoreCert.Text = Lang.T("IgnoreCert");
        chkAutoReply.Text = Lang.T("AutoReply");
        lblReqField.Text = Lang.T("ReqField");
        lblReqValue.Text = Lang.T("ReqValue");
        txtReqField.PlaceholderText = Lang.T("PhReqField");
        txtReqValue.PlaceholderText = Lang.T("PhReqValue");

        grpSend.Text = Lang.T("GrpSend");
        grpRecv.Text = Lang.T("GrpRecv");
        grpProcess.Text = Lang.T("GrpProcess");
        lblResponseTitle.Text = Lang.T("ResponseJson");
        btnSend.Text = Lang.T("Send");
        btnClearSend.Text = Lang.T("ClearSend");
        btnClearRecv.Text = Lang.T("ClearRecv");
        btnExportMessages.Text = Lang.T("ExportMessages");
        btnOpenHelp.Text = Lang.T("Help");
        txtInput.PlaceholderText = Lang.T("PhInput");

        lblRuleHint.Text = Lang.T("RuleHint");
        btnRuleAdd.Text = Lang.T("Add");
        btnRuleEdit.Text = Lang.T("Edit");
        btnRuleDelete.Text = Lang.T("Delete");
        btnRuleRefresh.Text = Lang.T("Refresh");

        btnHistoryRefresh.Text = Lang.T("RefreshHistory");
        btnHistoryClear.Text = Lang.T("ClearHistory");

        lblLogLevel.Text = Lang.T("LogLevel");
        var prevLevel = cmbLogLevel.SelectedIndex;
        cmbLogLevel.Items.Clear();
        cmbLogLevel.Items.AddRange(new object[] { Lang.T("All"), "INFO", "WARN", "ERROR", "DEBUG" });
        cmbLogLevel.SelectedIndex = prevLevel >= 0 && prevLevel < cmbLogLevel.Items.Count ? prevLevel : 0;
        btnLogRefresh.Text = Lang.T("Refresh");
        btnLogClear.Text = Lang.T("LogClear");
        btnLogExport.Text = Lang.T("LogExport");
        btnLogOpenFolder.Text = Lang.T("LogOpenFolder");
        lblLogPath.Text = Lang.T("LogFile") + _logger.TodayLogPath;
        lblDbPath.Text = Lang.T("Sqlite") + _store.DatabasePath;

        UpdateUiState();
        UpdateMessageCount();
        ShowProcessPlaceholder();
        RefreshRulesGrid();
        _logger.Info($"UI language => {Lang.Current}");
    }

    private void LoadDefaults()
    {
        txtLocalName.Text = Environment.MachineName;
        txtListenPort.Text = "8443";
        txtPeerUrl.Text = "https://127.0.0.1:8443";
        txtReqField.Text = "demoKey";
        txtReqValue.Text = "demoValue";
        chkIgnoreCert.Checked = true;
        chkAutoReply.Checked = false;
        EnsureCertificate();
        UpdateUiState();
        RefreshRulesGrid();
        RefreshHistory();
        RefreshLogView();
        UpdateMessageCount();
        ShowProcessPlaceholder();

        _logger.Info("Application started");
        _logger.Info("SQLite: " + _store.DatabasePath);
        AppendSystem(Lang.T("Welcome1"));
        AppendSystem(Lang.T("Welcome2"));
    }

    private void ShowProcessPlaceholder()
    {
        rtbProcess.Text = Lang.T("ProcessPlaceholder");
        rtbResponse.Text = Lang.T("ResponsePlaceholder");
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.F1)
        {
            OpenHelp();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void EnsureCertificate()
    {
        try
        {
            _cert?.Dispose();
            var raw = CertificateHelper.LoadOrCreate();
            _cert = new X509Certificate2Wrapper(raw);
            txtCertInfo.Text = CertificateHelper.Describe(raw);
            AppendSystem($"证书就绪：{raw.Thumbprint}");
        }
        catch (Exception ex)
        {
            txtCertInfo.Text = "证书加载失败：" + ex.Message;
            AppendSystem("证书错误：" + ex.Message, isError: true);
        }
    }

    private void RegenerateCertificate()
    {
        if (_server.IsRunning)
        {
            MessageBox.Show(Lang.T("CertStopFirst"), Lang.T("Prompt"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            if (File.Exists(CertificateHelper.DefaultCertPath))
            {
                File.Delete(CertificateHelper.DefaultCertPath);
            }

            var cert = CertificateHelper.CreateSelfSigned("CN=HttpsMessenger-Local");
            CertificateHelper.ExportPfx(cert, CertificateHelper.DefaultCertPath, CertificateHelper.DefaultPassword);
            var cerPath = Path.Combine(CertificateHelper.DefaultCertDirectory, "server.cer");
            CertificateHelper.ExportCer(cert, cerPath);
            cert.Dispose();
            EnsureCertificate();
            AppendSystem($"已重新生成证书，并导出公钥：{cerPath}");
        }
        catch (Exception ex)
        {
            AppendSystem("生成证书失败：" + ex.Message, isError: true);
            MessageBox.Show(Lang.T("GenFailed") + ex.Message, Lang.T("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task StartServerAsync()
    {
        if (_cert?.Certificate is null)
        {
            MessageBox.Show(Lang.T("CertUnavailable"), Lang.T("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (!int.TryParse(txtListenPort.Text.Trim(), out var port) || port is < 1 or > 65535)
        {
            MessageBox.Show(Lang.T("InvalidPort"), Lang.T("Prompt"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            _server.DisplayName = string.IsNullOrWhiteSpace(txtLocalName.Text)
                ? Environment.MachineName
                : txtLocalName.Text.Trim();
            await Task.Run(() => _server.Start(port, _cert.Certificate));
            UpdateUiState();
            _logger.Info($"HTTPS 服务启动，端口 {port}");
        }
        catch (Exception ex)
        {
            AppendSystem("启动失败：" + ex.Message, isError: true);
            MessageBox.Show(Lang.T("StartFailed") + ex.Message, Lang.T("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void StopServer()
    {
        var wasRunning = _server.IsRunning;
        _server.Stop();
        UpdateUiState();
        if (wasRunning)
        {
            _logger.Info("HTTPS 服务已停止");
        }
    }

    private async Task SendMessageAsync()
    {
        var content = txtInput.Text.Trim();
        if (string.IsNullOrEmpty(content))
        {
            return;
        }

        var peer = txtPeerUrl.Text.Trim();
        if (string.IsNullOrEmpty(peer))
        {
            MessageBox.Show(Lang.T("PeerRequired"), Lang.T("Prompt"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _client.IgnoreCertificateErrors = chkIgnoreCert.Checked;
        var message = new ChatMessage
        {
            Sender = string.IsNullOrWhiteSpace(txtLocalName.Text) ? Environment.MachineName : txtLocalName.Text.Trim(),
            Content = content,
            Timestamp = DateTime.Now,
            Type = string.Equals(content, "ping", StringComparison.OrdinalIgnoreCase) ? "ping" : "text",
            RequestField = string.IsNullOrWhiteSpace(txtReqField.Text) ? null : txtReqField.Text.Trim(),
            RequestValue = string.IsNullOrWhiteSpace(txtReqValue.Text) ? null : txtReqValue.Text
        };

        btnSend.Enabled = false;
        try
        {
            // 发送窗口只显示回复；请求细节等写入“全部响应信息”
            AppendRecvLine(
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] → 发送请求 {message.Sender}: {message.Content}" +
                (string.IsNullOrWhiteSpace(message.RequestField) ? "" : $" [{message.RequestField}={message.RequestValue}]"),
                Color.DarkBlue);
            txtInput.Clear();
            _logger.Info($"发送消息 → {peer} | content={content}");

            var (result, trace) = await _client.SendMessageWithTraceAsync(peer, message);
            ShowTrace(trace);
            ShowResponse(result);
            AppendFullResponseToRecv(result, trace);
            AppendReplyOnlyToSend(result, trace);
            _store.SaveCommunicationLog(trace);
            RefreshHistory();

            if (result.Success)
            {
                _logger.Info($"发送成功：{result.Message}");
                if (result.CustomFields.Count > 0)
                {
                    _logger.Info("响应自定义字段：" +
                                 string.Join("；", result.CustomFields.Select(kv => $"{kv.Key}={kv.Value}")));
                }
            }
            else
            {
                _logger.Error($"发送失败：{result.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.Error("发送异常：" + ex.Message);
            AppendRecvLine($"[{DateTime.Now:HH:mm:ss}] [错误] {ex.Message}", Color.Firebrick);
        }
        finally
        {
            btnSend.Enabled = true;
            txtInput.Focus();
        }
    }

    private async Task PingPeerAsync()
    {
        var peer = txtPeerUrl.Text.Trim();
        if (string.IsNullOrEmpty(peer))
        {
            MessageBox.Show(Lang.T("PeerRequired"), Lang.T("Prompt"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _client.IgnoreCertificateErrors = chkIgnoreCert.Checked;
        btnPing.Enabled = false;
        try
        {
            _logger.Info("开始连通测试：" + peer);
            AppendRecvLine($"[{DateTime.Now:HH:mm:ss}] [Ping] GET /api/ping → {peer}", Color.DarkBlue);

            var (result, trace) = await _client.PingWithTraceAsync(peer);
            ShowTrace(trace);
            ShowResponse(result);
            AppendFullResponseToRecv(result, trace);
            AppendReplyOnlyToSend(result, trace);
            _store.SaveCommunicationLog(trace);
            RefreshHistory();
            _logger.Write(result.Success ? "INFO" : "ERROR",
                result.Success ? $"连通成功：{result.Message}" : $"连通失败：{result.Message}");
        }
        catch (Exception ex)
        {
            _logger.Error("连通异常：" + ex.Message);
            AppendRecvLine($"[{DateTime.Now:HH:mm:ss}] [Ping 错误] {ex.Message}", Color.Firebrick);
        }
        finally
        {
            btnPing.Enabled = true;
        }
    }

    private void OnServerMessageReceived(ChatMessage message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => OnServerMessageReceived(message));
            return;
        }

        // 入站请求显示在“全部响应信息”
        AppendRecvLine(
            $"[{message.Timestamp:yyyy-MM-dd HH:mm:ss}] ← 收到请求 {message.Sender}: {message.Content}",
            Color.DarkOliveGreen);
        _logger.Info($"服务端收到消息：{message.Sender} => {message.Content}");

        if (chkAutoReply.Checked && !string.Equals(message.Type, "auto-reply", StringComparison.OrdinalIgnoreCase))
        {
            _ = AutoReplyAsync(message);
        }
    }

    private void OnServerTraceCompleted(CommunicationTrace trace)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => OnServerTraceCompleted(trace));
            return;
        }

        ShowTrace(trace);
        if (!string.IsNullOrWhiteSpace(trace.ResponseBody))
        {
            rtbResponse.Text = PrettyMaybe(trace.ResponseBody);
            _logger.Debug($"服务端应答 HTTP {trace.ResponseStatusCode}");
        }

        try
        {
            _store.SaveCommunicationLog(trace);
            RefreshHistory();
        }
        catch (Exception ex)
        {
            _logger.Warn("通讯历史写入失败：" + ex.Message);
        }
    }

    private async Task AutoReplyAsync(ChatMessage incoming)
    {
        var peer = txtPeerUrl.Text.Trim();
        if (string.IsNullOrEmpty(peer))
        {
            return;
        }

        var reply = new ChatMessage
        {
            Sender = string.IsNullOrWhiteSpace(txtLocalName.Text) ? Environment.MachineName : txtLocalName.Text.Trim(),
            Content = $"[自动回复] 已收到：{Truncate(incoming.Content, 40)}",
            Timestamp = DateTime.Now,
            Type = "auto-reply"
        };

        try
        {
            _client.IgnoreCertificateErrors = chkIgnoreCert.Checked;
            var (result, trace) = await _client.SendMessageWithTraceAsync(peer, reply);
            if (result.Success)
            {
                AppendRecvLine(
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] → 自动回复: {reply.Content}",
                    Color.DarkBlue);
                ShowTrace(trace);
                ShowResponse(result);
                AppendFullResponseToRecv(result, trace);
                AppendReplyOnlyToSend(result, trace);
                _logger.Info("自动回复已发送");
            }
        }
        catch (Exception ex)
        {
            _logger.Warn("自动回复失败：" + ex.Message);
        }
    }

    private void ShowTrace(CommunicationTrace trace)
    {
        rtbProcess.Text = trace.FormatProcessText();
        rtbProcess.SelectionStart = 0;
        rtbProcess.ScrollToCaret();
        _logger.Debug($"{trace.Direction} 过程：{trace.Title} => {(trace.Success ? "成功" : "失败")}");
    }

    private void ShowResponse(ApiResponse response)
    {
        rtbResponse.Text = JsonSerializer.Serialize(response, PrettyJson);
    }

    private static string PrettyMaybe(string text)
    {
        try
        {
            using var doc = JsonDocument.Parse(text);
            return JsonSerializer.Serialize(doc.RootElement, PrettyJson);
        }
        catch
        {
            return text;
        }
    }

    #region Send / Recv windows

    /// <summary>
    /// 发送信息窗口：仅显示简洁回复摘要。
    /// 格式示例：
    /// [2026-08-13 18:17:11] 响应摘要  HTTP 200 成功
    /// 回复信息:1234567890
    /// </summary>
    private void AppendReplyOnlyToSend(ApiResponse response, CommunicationTrace trace)
    {
        var statusText = response.Success ? Lang.T("Success") : Lang.T("Fail");
        var replyText = ExtractReplyText(response);
        var text =
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {Lang.T("ResponseSummary")}  HTTP {trace.ResponseStatusCode} {statusText}" +
            Environment.NewLine +
            $"{Lang.T("ReplyInfo")}:{replyText}";

        AppendSendLine(text, response.Success ? Color.SteelBlue : Color.Firebrick);
    }

    /// <summary>
    /// 全部响应信息窗口：原始响应、匹配规则、自定义字段等完整内容。
    /// </summary>
    private void AppendFullResponseToRecv(ApiResponse response, CommunicationTrace trace)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ===== 全部响应信息 =====");
        sb.AppendLine($"HTTP {trace.ResponseStatusCode}  {(response.Success ? Lang.T("Success") : Lang.T("Fail"))}");
        sb.AppendLine($"message: {response.Message}");
        if (response.MatchedRules.Count > 0)
        {
            sb.AppendLine("matchedRules: " + string.Join("、", response.MatchedRules));
        }

        if (response.CustomFields.Count > 0)
        {
            sb.AppendLine("customFields:");
            foreach (var kv in response.CustomFields)
            {
                sb.AppendLine($"  {kv.Key} = {kv.Value}");
            }
        }

        if (response.Echo is not null)
        {
            sb.AppendLine($"echo.content: {response.Echo.Content}");
        }

        sb.AppendLine("----- 原始响应正文 -----");
        sb.AppendLine(string.IsNullOrWhiteSpace(trace.ResponseBody)
            ? "(无响应正文)"
            : PrettyMaybe(trace.ResponseBody));

        AppendRecvLine(sb.ToString().TrimEnd(), response.Success ? Color.DarkGreen : Color.Firebrick);
    }

    private static string ExtractReplyText(ApiResponse response)
    {
        // 优先 reply 字段；否则用 echo 原文（例如发送的数字）；再否则 message / 其它自定义字段
        if (response.CustomFields.TryGetValue("reply", out var reply) && !string.IsNullOrWhiteSpace(reply))
        {
            return reply;
        }

        foreach (var kv in response.CustomFields)
        {
            if (string.Equals(kv.Key, "reply", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(kv.Value))
            {
                return kv.Value;
            }
        }

        if (!string.IsNullOrWhiteSpace(response.Echo?.Content))
        {
            return response.Echo.Content;
        }

        if (!string.IsNullOrWhiteSpace(response.Message))
        {
            return response.Message;
        }

        if (response.CustomFields.Count > 0)
        {
            return string.Join("; ", response.CustomFields.Select(kv => $"{kv.Key}={kv.Value}"));
        }

        return "(无回复内容)";
    }

    private void AppendSendLine(string line, Color color)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => AppendSendLine(line, color));
            return;
        }

        lock (_msgLock)
        {
            _sendLines.Add(line);
            _sendCount++;
        }

        AppendToBox(rtbSend, line, color);
        UpdateMessageCount();
    }

    private void AppendRecvLine(string line, Color color)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => AppendRecvLine(line, color));
            return;
        }

        lock (_msgLock)
        {
            _recvLines.Add(line);
            _recvCount++;
        }

        AppendToBox(rtbRecv, line, color);
        UpdateMessageCount();
    }

    private static void AppendToBox(RichTextBox box, string line, Color color)
    {
        box.SelectionStart = box.TextLength;
        box.SelectionLength = 0;
        box.SelectionColor = color;
        box.AppendText(line + Environment.NewLine + Environment.NewLine);
        box.SelectionColor = box.ForeColor;
        box.ScrollToCaret();
    }

    private void AppendSystem(string text, bool isError = false)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => AppendSystem(text, isError));
            return;
        }

        _logger.Write(isError ? "ERROR" : "INFO", text);
        // 系统信息写入“全部响应信息”，不占用发送窗口
        AppendRecvLine(
            $"[{DateTime.Now:HH:mm:ss}] [系统] {text}",
            isError ? Color.Firebrick : Color.DimGray);
    }

    private void ClearSendWindow()
    {
        lock (_msgLock)
        {
            _sendLines.Clear();
            _sendCount = 0;
        }

        rtbSend.Clear();
        UpdateMessageCount();
        _logger.Info("已清空发送信息窗口");
    }

    private void ClearRecvWindow()
    {
        lock (_msgLock)
        {
            _recvLines.Clear();
            _recvCount = 0;
        }

        rtbRecv.Clear();
        UpdateMessageCount();
        _logger.Info("已清空响应信息窗口");
    }

    private void ExportMessages()
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
            FileName = $"HttpsMessenger_Messages_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        string[] send;
        string[] recv;
        lock (_msgLock)
        {
            send = _sendLines.ToArray();
            recv = _recvLines.ToArray();
        }

        var sb = new StringBuilder();
        sb.AppendLine("===== 发送信息 =====");
        foreach (var line in send)
        {
            sb.AppendLine(line);
        }

        sb.AppendLine();
        sb.AppendLine("===== 响应信息 =====");
        foreach (var line in recv)
        {
            sb.AppendLine(line);
        }

        File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
        _logger.Info("消息已导出：" + dialog.FileName);
        MessageBox.Show(Lang.T("ExportOk"), Lang.T("Prompt"), MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void UpdateMessageCount()
    {
        lblMessageCount.Text = Lang.Tf("MsgCount", _sendCount, _recvCount);
    }

    #endregion

    #region Runtime Log

    private void OnLogEntryAdded(LogEntry entry)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(() => OnLogEntryAdded(entry));
            return;
        }

        var filter = cmbLogLevel.SelectedItem?.ToString() ?? "全部";
        if (!string.Equals(filter, entry.Level, StringComparison.OrdinalIgnoreCase)
            && cmbLogLevel.SelectedIndex > 0)
        {
            return;
        }

        AppendToBox(rtbLog, entry.ToLine(), entry.GetColor());
        lblLogPath.Text = "日志文件：" + _logger.TodayLogPath;
    }

    private void RefreshLogView()
    {
        var filter = cmbLogLevel.SelectedItem?.ToString() ?? "全部";
        var entries = _logger.GetEntries(filter);
        rtbLog.Clear();
        foreach (var entry in entries)
        {
            AppendToBox(rtbLog, entry.ToLine(), entry.GetColor());
        }

        lblLogPath.Text = "日志文件：" + _logger.TodayLogPath;
    }

    private void ClearMemoryLogs()
    {
        if (MessageBox.Show(Lang.T("ConfirmClearLog"), Lang.T("Confirm"), MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            != DialogResult.Yes)
        {
            return;
        }

        _logger.ClearMemory();
        rtbLog.Clear();
        _logger.Info("Memory log cleared");
    }

    private void ExportRuntimeLog()
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "日志文件 (*.log)|*.log|文本文件 (*.txt)|*.txt",
            FileName = $"HttpsMessenger_Runtime_{DateTime.Now:yyyyMMdd_HHmmss}.log"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var entries = _logger.GetEntries("全部");
        File.WriteAllLines(dialog.FileName, entries.Select(e => e.ToLine()), Encoding.UTF8);
        _logger.Info("运行日志已导出：" + dialog.FileName);
        MessageBox.Show(Lang.T("ExportOk"), Lang.T("Prompt"), MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    #endregion

    #region Rules CRUD

    private void RefreshRulesGrid()
    {
        var rules = _store.GetAllRules();
        dgvRules.DataSource = null;
        dgvRules.DataSource = rules.Select(r => new
        {
            r.Id,
            Name = r.Name,
            ReqField = r.RequestField,
            Match = r.MatchMode,
            ReqValue = r.RequestValue,
            ResField = r.ResponseField,
            ResValue = r.ResponseValue,
            Status = r.HttpStatus,
            ResMsg = r.ResponseMessage,
            Enabled = r.IsEnabled ? Lang.T("Yes") : Lang.T("No"),
            Remark = r.Remark
        }).ToList();

        if (dgvRules.Columns.Count > 0)
        {
            void SetHeader(string name, string key)
            {
                if (dgvRules.Columns.Contains(name))
                {
                    dgvRules.Columns[name]!.HeaderText = Lang.T(key);
                }
            }

            SetHeader("Id", "ColId");
            SetHeader("Name", "ColName");
            SetHeader("ReqField", "ColReqField");
            SetHeader("Match", "ColMatch");
            SetHeader("ReqValue", "ColReqValue");
            SetHeader("ResField", "ColResField");
            SetHeader("ResValue", "ColResValue");
            SetHeader("Status", "ColStatus");
            SetHeader("ResMsg", "ColResMsg");
            SetHeader("Enabled", "ColEnabled");
            SetHeader("Remark", "ColRemark");
        }
    }

    private long? GetSelectedRuleId()
    {
        if (dgvRules.CurrentRow is null || dgvRules.CurrentRow.Index < 0)
        {
            return null;
        }

        var val = dgvRules.CurrentRow.Cells["Id"].Value;
        return val is null ? null : Convert.ToInt64(val);
    }

    private void AddRule()
    {
        using var dlg = new RuleEditForm();
        if (dlg.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _store.AddRule(dlg.Rule);
        RefreshRulesGrid();
        _logger.Info($"已新增规则：{dlg.Rule.Name}");
    }

    private void EditSelectedRule()
    {
        var id = GetSelectedRuleId();
        if (id is null)
        {
            MessageBox.Show(Lang.T("TipSelectRule"), Lang.T("Prompt"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var existing = _store.GetRule(id.Value);
        if (existing is null)
        {
            MessageBox.Show(Lang.T("TipRuleMissing"), Lang.T("Prompt"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            RefreshRulesGrid();
            return;
        }

        using var dlg = new RuleEditForm(existing);
        if (dlg.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _store.UpdateRule(dlg.Rule);
        RefreshRulesGrid();
        _logger.Info($"已修改规则：{dlg.Rule.Name}");
    }

    private void DeleteSelectedRule()
    {
        var id = GetSelectedRuleId();
        if (id is null)
        {
            MessageBox.Show(Lang.T("TipSelectRule"), Lang.T("Prompt"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (MessageBox.Show(Lang.T("ConfirmDeleteRule"), Lang.T("Confirm"), MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            != DialogResult.Yes)
        {
            return;
        }

        _store.DeleteRule(id.Value);
        RefreshRulesGrid();
        _logger.Warn($"已删除规则 Id={id.Value}");
    }

    #endregion

    #region History

    private void RefreshHistory()
    {
        var logs = _store.GetRecentLogs(80);
        _historyIndex.Clear();
        lstHistory.Items.Clear();
        foreach (var log in logs)
        {
            var title = $"[{log.CreatedAt:MM-dd HH:mm:ss}] {log.Direction} {(log.Success ? "OK" : "FAIL")} {log.Title}";
            _historyIndex.Add((log.Id, log.ProcessText));
            lstHistory.Items.Add(title);
        }
    }

    private void ShowSelectedHistory()
    {
        var idx = lstHistory.SelectedIndex;
        if (idx < 0 || idx >= _historyIndex.Count)
        {
            rtbHistoryDetail.Clear();
            return;
        }

        rtbHistoryDetail.Text = _historyIndex[idx].ProcessText;
    }

    private void ClearHistory()
    {
        if (MessageBox.Show(Lang.T("ConfirmClearHistory"), Lang.T("Confirm"), MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            != DialogResult.Yes)
        {
            return;
        }

        _store.ClearLogs();
        RefreshHistory();
        rtbHistoryDetail.Clear();
        _logger.Warn("通讯历史已清空");
    }

    #endregion

    private void OpenHelp()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Help", "帮助文档.html"),
            Path.Combine(Application.StartupPath, "Help", "帮助文档.html"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Help", "帮助文档.html"))
        };

        var path = candidates.FirstOrDefault(File.Exists);
        if (path is null)
        {
            MessageBox.Show(Lang.T("HelpMissing"), Lang.T("Help"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }

    private void ShowAbout()
    {
        MessageBox.Show(
            "HttpsMessenger 1.3\r\n\r\n" +
            "• Resizable window / 可调整窗口大小\r\n" +
            "• Language: 中文 / English / 中英对照\r\n" +
            "• Split send/response windows\r\n" +
            "• Runtime log + SQLite rules\r\n\r\n" +
            $"DB: {_store.DatabasePath}\r\n" +
            $"Log: {_logger.LogDirectory}",
            Lang.T("AboutTitle"),
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void UpdateUiState()
    {
        var running = _server.IsRunning;
        btnStartServer.Enabled = !running;
        btnStopServer.Enabled = running;
        txtListenPort.Enabled = !running;
        lblServerStatus.Text = running
            ? Lang.Tf("ServerRunning", _server.Port)
            : Lang.T("ServerStopped");
        lblServerStatus.ForeColor = running ? Color.SeaGreen : Color.Firebrick;
    }

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max] + "...";

    private sealed class X509Certificate2Wrapper : IDisposable
    {
        public System.Security.Cryptography.X509Certificates.X509Certificate2 Certificate { get; }

        public X509Certificate2Wrapper(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate)
        {
            Certificate = certificate;
        }

        public void Dispose() => Certificate.Dispose();
    }
}
