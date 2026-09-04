using System.Text.Json.Serialization;

namespace HttpsMessenger.Models;

/// <summary>
/// HTTPS 通讯中交换的消息模型。
/// </summary>
public sealed class ChatMessage
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [JsonPropertyName("sender")]
    public string Sender { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.Now;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";

    /// <summary>
    /// 自定义请求字段名（可与 SQLite 规则中的 RequestField 对应）。
    /// </summary>
    [JsonPropertyName("requestField")]
    public string? RequestField { get; set; }

    /// <summary>
    /// 自定义请求字段值。
    /// </summary>
    [JsonPropertyName("requestValue")]
    public string? RequestValue { get; set; }

    public override string ToString()
        => $"[{Timestamp:HH:mm:ss}] {Sender}: {Content}";
}

/// <summary>
/// 服务端统一响应格式（基础字段 + 动态字段）。
/// </summary>
public sealed class ApiResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("serverTime")]
    public DateTime ServerTime { get; set; } = DateTime.Now;

    [JsonPropertyName("echo")]
    public ChatMessage? Echo { get; set; }

    [JsonPropertyName("matchedRules")]
    public List<string> MatchedRules { get; set; } = new();

    [JsonPropertyName("customFields")]
    public Dictionary<string, string> CustomFields { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// SQLite 中存储的“请求字段 → 响应字段”规则。
/// </summary>
public sealed class FieldRule
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RequestField { get; set; } = "content";
    public string MatchMode { get; set; } = "Equals"; // Equals / Contains / Any
    public string RequestValue { get; set; } = string.Empty;
    public string ResponseField { get; set; } = "reply";
    public string ResponseValue { get; set; } = string.Empty;
    public int HttpStatus { get; set; } = 200;
    public string ResponseMessage { get; set; } = "OK";
    public bool IsEnabled { get; set; } = true;
    public string Remark { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// 一次 HTTPS 通讯过程中的步骤。
/// </summary>
public sealed class TraceStep
{
    public int Order { get; set; }
    public string Phase { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public DateTime Time { get; set; } = DateTime.Now;
}

/// <summary>
/// 完整通讯过程追踪结果。
/// </summary>
public sealed class CommunicationTrace
{
    public string Direction { get; set; } = "Client"; // Client / Server
    public string Title { get; set; } = string.Empty;
    public List<TraceStep> Steps { get; set; } = new();
    public string RequestMethod { get; set; } = string.Empty;
    public string RequestUrl { get; set; } = string.Empty;
    public string RequestHeaders { get; set; } = string.Empty;
    public string RequestBody { get; set; } = string.Empty;
    public int ResponseStatusCode { get; set; }
    public string ResponseHeaders { get; set; } = string.Empty;
    public string ResponseBody { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Error { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.Now;
    public DateTime FinishedAt { get; set; } = DateTime.Now;

    public void AddStep(string phase, string detail)
    {
        Steps.Add(new TraceStep
        {
            Order = Steps.Count + 1,
            Phase = phase,
            Detail = detail,
            Time = DateTime.Now
        });
    }

    public string FormatProcessText()
    {
        var lines = new List<string>
        {
            $"【{Direction}】{Title}",
            $"开始时间：{StartedAt:yyyy-MM-dd HH:mm:ss.fff}",
            $"结束时间：{FinishedAt:yyyy-MM-dd HH:mm:ss.fff}",
            $"结果：{(Success ? "成功" : "失败")}" + (string.IsNullOrEmpty(Error) ? "" : $"（{Error}）"),
            "",
            "===== HTTPS 通讯过程 ====="
        };

        foreach (var step in Steps.OrderBy(s => s.Order))
        {
            lines.Add($"{step.Order}. [{step.Time:HH:mm:ss.fff}] {step.Phase}");
            lines.Add($"   {step.Detail}");
        }

        lines.Add("");
        lines.Add("===== 请求信息 =====");
        lines.Add($"{RequestMethod} {RequestUrl}");
        if (!string.IsNullOrWhiteSpace(RequestHeaders))
        {
            lines.Add(RequestHeaders.TrimEnd());
        }

        lines.Add("");
        lines.Add(RequestBody);

        lines.Add("");
        lines.Add("===== 响应信息 =====");
        lines.Add($"HTTP {ResponseStatusCode}");
        if (!string.IsNullOrWhiteSpace(ResponseHeaders))
        {
            lines.Add(ResponseHeaders.TrimEnd());
        }

        lines.Add("");
        lines.Add(ResponseBody);
        return string.Join(Environment.NewLine, lines);
    }
}
