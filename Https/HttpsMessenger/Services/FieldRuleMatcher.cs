using System.Text.Json;
using HttpsMessenger.Models;

namespace HttpsMessenger.Services;

/// <summary>
/// 根据请求 JSON 匹配 SQLite 中的字段规则，生成响应自定义字段。
/// </summary>
public static class FieldRuleMatcher
{
    public static (ApiResponse Response, int StatusCode, List<FieldRule> Matched) Match(
        string requestJson,
        ChatMessage? message,
        IReadOnlyList<FieldRule> rules)
    {
        var response = new ApiResponse
        {
            Success = true,
            Message = "消息已接收",
            ServerTime = DateTime.Now,
            Echo = message
        };

        var matched = new List<FieldRule>();
        var status = 200;

        Dictionary<string, string> flat;
        try
        {
            flat = FlattenJson(requestJson);
        }
        catch
        {
            flat = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (message is not null)
            {
                flat["id"] = message.Id;
                flat["sender"] = message.Sender;
                flat["content"] = message.Content;
                flat["type"] = message.Type;
                if (!string.IsNullOrWhiteSpace(message.RequestField))
                {
                    flat["requestField"] = message.RequestField!;
                }

                if (!string.IsNullOrWhiteSpace(message.RequestValue))
                {
                    flat["requestValue"] = message.RequestValue!;
                }
            }
        }

        // 若同时提供了 requestField/requestValue，则把该自定义键值也加入匹配字典
        if (message is not null
            && !string.IsNullOrWhiteSpace(message.RequestField)
            && message.RequestValue is not null)
        {
            flat[message.RequestField!] = message.RequestValue;
        }

        foreach (var rule in rules.Where(r => r.IsEnabled))
        {
            if (!TryGetField(flat, rule.RequestField, out var actual))
            {
                continue;
            }

            if (!IsMatch(rule.MatchMode, actual, rule.RequestValue))
            {
                continue;
            }

            matched.Add(rule);
            var value = ExpandPlaceholders(rule.ResponseValue);
            response.CustomFields[rule.ResponseField] = value;
            response.MatchedRules.Add(rule.Name);
            status = rule.HttpStatus;
            if (!string.IsNullOrWhiteSpace(rule.ResponseMessage))
            {
                response.Message = rule.ResponseMessage;
            }
        }

        return (response, status, matched);
    }

    private static bool TryGetField(Dictionary<string, string> flat, string field, out string value)
    {
        if (flat.TryGetValue(field, out value!))
        {
            return true;
        }

        // 兼容大小写与常见别名
        foreach (var kv in flat)
        {
            if (string.Equals(kv.Key, field, StringComparison.OrdinalIgnoreCase))
            {
                value = kv.Value;
                return true;
            }
        }

        value = string.Empty;
        return false;
    }

    private static bool IsMatch(string mode, string actual, string expected)
    {
        mode = (mode ?? "Equals").Trim();
        if (string.Equals(mode, "Any", StringComparison.OrdinalIgnoreCase))
        {
            return !string.IsNullOrWhiteSpace(actual);
        }

        if (string.Equals(mode, "Contains", StringComparison.OrdinalIgnoreCase))
        {
            return actual.Contains(expected ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(actual, expected ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private static string ExpandPlaceholders(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return value
            .Replace("{now}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), StringComparison.OrdinalIgnoreCase)
            .Replace("{utc}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"), StringComparison.OrdinalIgnoreCase)
            .Replace("{machine}", Environment.MachineName, StringComparison.OrdinalIgnoreCase);
    }

    private static Dictionary<string, string> FlattenJson(string json)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(json))
        {
            return result;
        }

        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Object)
        {
            return result;
        }

        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            result[prop.Name] = prop.Value.ValueKind switch
            {
                JsonValueKind.String => prop.Value.GetString() ?? "",
                JsonValueKind.Number => prop.Value.ToString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => "",
                _ => prop.Value.ToString()
            };
        }

        return result;
    }
}
