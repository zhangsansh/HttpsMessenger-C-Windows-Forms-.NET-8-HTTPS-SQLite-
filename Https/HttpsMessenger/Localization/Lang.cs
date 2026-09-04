namespace HttpsMessenger.Localization;

public enum AppLanguage
{
    Chinese = 0,
    English = 1,
    /// <summary>中英对照（语言结合）</summary>
    Bilingual = 2
}

/// <summary>
/// 简易多语言资源：中文 / English / 中英结合。
/// </summary>
public static class Lang
{
    public static AppLanguage Current { get; private set; } = AppLanguage.Chinese;

    public static event Action? LanguageChanged;

    private static readonly Dictionary<string, (string Zh, string En)> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["AppTitle"] = ("HttpsMessenger - HTTPS 通讯工具", "HttpsMessenger - HTTPS Messenger"),
        ["MenuHelp"] = ("帮助(&H)", "Help(&H)"),
        ["MenuOpenHelp"] = ("打开帮助文档(&O)", "Open Help(&O)"),
        ["MenuAbout"] = ("关于(&A)", "About(&A)"),
        ["MenuLanguage"] = ("语言(&L)", "Language(&L)"),
        ["LangZh"] = ("中文", "Chinese"),
        ["LangEn"] = ("English", "English"),
        ["LangBi"] = ("中英对照（语言结合）", "Bilingual (ZH+EN)"),

        ["TabComm"] = ("HTTPS 通讯", "HTTPS Communication"),
        ["TabRules"] = ("请求/响应字段规则", "Request/Response Rules"),
        ["TabHistory"] = ("通讯过程历史", "Communication History"),
        ["TabLog"] = ("运行日志", "Runtime Log"),

        ["GrpServer"] = ("本机 HTTPS 服务", "Local HTTPS Server"),
        ["LocalName"] = ("本机名称：", "Local Name:"),
        ["Port"] = ("端口：", "Port:"),
        ["StartServer"] = ("启动服务", "Start Server"),
        ["StopServer"] = ("停止服务", "Stop Server"),
        ["RegenCert"] = ("重建证书", "Regen Certificate"),

        ["GrpPeer"] = ("对端连接与请求字段", "Peer & Request Fields"),
        ["PeerUrl"] = ("对端 HTTPS 地址：", "Peer HTTPS URL:"),
        ["Ping"] = ("测试连通", "Test Ping"),
        ["IgnoreCert"] = ("忽略证书错误", "Ignore Cert Errors"),
        ["AutoReply"] = ("自动回复", "Auto Reply"),
        ["ReqField"] = ("自定义请求字段：", "Custom Request Field:"),
        ["ReqValue"] = ("字段值：", "Field Value:"),
        ["PhReqField"] = ("如 demoKey", "e.g. demoKey"),
        ["PhReqValue"] = ("对应请求值", "request value"),

        ["GrpSend"] = ("发送信息窗口（仅回复）", "Send Window (reply only)"),
        ["GrpRecv"] = ("全部响应信息", "All Response Info"),
        ["ReplyInfo"] = ("回复信息", "Reply"),
        ["ResponseSummary"] = ("响应摘要", "Response Summary"),
        ["Success"] = ("成功", "Success"),
        ["Fail"] = ("失败", "Failed"),
        ["GrpProcess"] = ("HTTPS 通讯全过程 / 响应 JSON", "HTTPS Process / Response JSON"),
        ["ResponseJson"] = ("本次响应 JSON：", "Response JSON:"),
        ["Send"] = ("发送", "Send"),
        ["ClearSend"] = ("清空发送", "Clear Send"),
        ["ClearRecv"] = ("清空响应", "Clear Response"),
        ["ExportMessages"] = ("导出消息", "Export Messages"),
        ["Help"] = ("帮助", "Help"),
        ["PhInput"] = ("输入发送内容，Enter 发送", "Type message, Enter to send"),

        ["RuleHint"] = (
            "在此管理 SQLite 中的“请求字段 → 响应字段”规则。服务端收到消息后会按规则匹配并写入响应 customFields。",
            "Manage SQLite request→response field rules. Matched fields are written into customFields."),
        ["Add"] = ("新增", "Add"),
        ["Edit"] = ("修改", "Edit"),
        ["Delete"] = ("删除", "Delete"),
        ["Refresh"] = ("刷新", "Refresh"),

        ["RefreshHistory"] = ("刷新历史", "Refresh History"),
        ["ClearHistory"] = ("清空历史", "Clear History"),

        ["LogLevel"] = ("级别筛选：", "Level Filter:"),
        ["LogClear"] = ("清空内存日志", "Clear Memory Log"),
        ["LogExport"] = ("导出日志", "Export Log"),
        ["LogOpenFolder"] = ("打开日志目录", "Open Log Folder"),
        ["LogFile"] = ("日志文件：", "Log File:"),
        ["Sqlite"] = ("SQLite：", "SQLite:"),

        ["ServerStopped"] = ("服务状态：已停止", "Server: Stopped"),
        ["ServerRunning"] = ("服务状态：运行中（端口 {0}）", "Server: Running (port {0})"),
        ["MsgCount"] = ("发送:{0} / 响应:{1}", "Sent:{0} / Resp:{1}"),

        ["ProcessPlaceholder"] = (
            "此处展示 HTTPS 通讯全过程：\r\n1. 准备请求\r\n2. TCP 连接\r\n3. TLS 握手\r\n4. 发送 HTTP 请求\r\n5. 接收并解析响应",
            "HTTPS process steps:\r\n1. Prepare request\r\n2. TCP connect\r\n3. TLS handshake\r\n4. Send HTTP\r\n5. Receive & parse"),
        ["ResponsePlaceholder"] = ("发送后显示响应 JSON。", "Response JSON appears after sending."),

        ["Welcome1"] = (
            "欢迎使用 HttpsMessenger：发送与响应已分窗显示，运行日志见“运行日志”页签。",
            "Welcome to HttpsMessenger: split send/response windows; see Runtime Log tab."),
        ["Welcome2"] = (
            "发送“你好/帮助/时间”可触发示例规则；可通过菜单切换中文/English/中英对照。",
            "Send hello/help/time to trigger sample rules; switch language via the Language menu."),

        ["TipSelectRule"] = ("请先选择一条规则。", "Please select a rule first."),
        ["TipRuleMissing"] = ("规则不存在或已被删除。", "Rule not found or already deleted."),
        ["ConfirmDeleteRule"] = ("确定删除选中规则吗？", "Delete the selected rule?"),
        ["ConfirmClearHistory"] = ("确定清空全部通讯历史吗？", "Clear all communication history?"),
        ["ConfirmClearLog"] = ("仅清空界面内存中的日志显示（不会删除磁盘日志文件）。继续？", "Clear in-memory log only (disk files kept). Continue?"),
        ["ExportOk"] = ("导出成功。", "Export succeeded."),
        ["Prompt"] = ("提示", "Info"),
        ["Error"] = ("错误", "Error"),
        ["Confirm"] = ("确认", "Confirm"),
        ["AboutTitle"] = ("关于", "About"),
        ["HelpMissing"] = ("未找到帮助文档。", "Help file not found."),
        ["CertStopFirst"] = ("请先停止服务再重新生成证书。", "Stop the server before regenerating the certificate."),
        ["InvalidPort"] = ("请输入有效端口（1~65535）。", "Enter a valid port (1~65535)."),
        ["PeerRequired"] = ("请填写对端 HTTPS 地址。", "Peer HTTPS URL is required."),
        ["CertUnavailable"] = ("证书不可用，无法启动 HTTPS 服务。", "Certificate unavailable; cannot start HTTPS server."),
        ["StartFailed"] = ("启动失败：", "Start failed: "),
        ["GenFailed"] = ("生成失败：", "Generation failed: "),
        ["OpenLogFailed"] = ("无法打开日志目录：", "Cannot open log folder: "),

        ["RuleAddTitle"] = ("新增请求/响应字段规则", "Add Request/Response Rule"),
        ["RuleEditTitle"] = ("编辑请求/响应字段规则", "Edit Request/Response Rule"),
        ["RuleName"] = ("规则名称：", "Rule Name:"),
        ["RequestField"] = ("请求字段名：", "Request Field:"),
        ["MatchMode"] = ("匹配方式：", "Match Mode:"),
        ["RequestValue"] = ("请求字段值：", "Request Value:"),
        ["ResponseField"] = ("响应字段名：", "Response Field:"),
        ["ResponseValue"] = ("响应字段值：", "Response Value:"),
        ["HttpStatus"] = ("HTTP 状态码：", "HTTP Status:"),
        ["ResponseMessage"] = ("响应消息：", "Response Message:"),
        ["Enabled"] = ("启用该规则", "Enable this rule"),
        ["Remark"] = ("备注：", "Remark:"),
        ["Save"] = ("保存", "Save"),
        ["Cancel"] = ("取消", "Cancel"),
        ["RuleRequired"] = ("规则名称、请求字段名、响应字段名不能为空。", "Name, request field and response field are required."),

        ["ColId"] = ("Id", "Id"),
        ["ColName"] = ("名称", "Name"),
        ["ColReqField"] = ("请求字段", "Req Field"),
        ["ColMatch"] = ("匹配方式", "Match"),
        ["ColReqValue"] = ("请求值", "Req Value"),
        ["ColResField"] = ("响应字段", "Res Field"),
        ["ColResValue"] = ("响应值", "Res Value"),
        ["ColStatus"] = ("状态码", "Status"),
        ["ColResMsg"] = ("响应消息", "Res Message"),
        ["ColEnabled"] = ("启用", "Enabled"),
        ["ColRemark"] = ("备注", "Remark"),
        ["Yes"] = ("是", "Yes"),
        ["No"] = ("否", "No"),
        ["All"] = ("全部", "All"),
    };

    public static void SetLanguage(AppLanguage language)
    {
        if (Current == language)
        {
            return;
        }

        Current = language;
        SavePreference();
        LanguageChanged?.Invoke();
    }

    public static string T(string key)
    {
        if (!Map.TryGetValue(key, out var pair))
        {
            return key;
        }

        return Current switch
        {
            AppLanguage.English => pair.En,
            AppLanguage.Bilingual => pair.Zh == pair.En ? pair.Zh : $"{pair.Zh} / {pair.En}",
            _ => pair.Zh
        };
    }

    public static string Tf(string key, params object[] args)
        => string.Format(T(key), args);

    private static string PrefPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "HttpsMessenger",
        "ui_language.txt");

    public static void LoadPreference()
    {
        try
        {
            if (!File.Exists(PrefPath))
            {
                return;
            }

            var text = File.ReadAllText(PrefPath).Trim();
            if (Enum.TryParse<AppLanguage>(text, true, out var lang))
            {
                Current = lang;
            }
        }
        catch
        {
            // ignore
        }
    }

    private static void SavePreference()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PrefPath)!);
            File.WriteAllText(PrefPath, Current.ToString());
        }
        catch
        {
            // ignore
        }
    }
}
