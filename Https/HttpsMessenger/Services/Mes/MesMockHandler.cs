using System.Text;
using System.Text.Json;
using HttpsMessenger.Models.Mes;

namespace HttpsMessenger.Services.Mes;

/// <summary>
/// 本地 MES 接口模拟（用于联调测试），解析 form-urlencoded 的 jsonData。
/// </summary>
public static class MesMockHandler
{
    public static bool TryHandle(string method, string path, string body, out string jsonResponse, out int statusCode)
    {
        jsonResponse = "";
        statusCode = 404;

        if (!string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var api = ExtractApiName(path);
        if (api is null || !IsKnownApi(api))
        {
            return false;
        }

        var jsonData = ExtractJsonData(body);
        if (jsonData is null)
        {
            jsonResponse = MesJsonHelper.Serialize(new MesApiResponse
            {
                Status = "false",
                Result = "缺少 jsonData 表单参数"
            });
            statusCode = 400;
            return true;
        }

        jsonResponse = api switch
        {
            MesApiNames.LoginCheck => MockLogin(jsonData),
            MesApiNames.GetSpecifications => MockOk("获取成功", details: MockSpecDetails()),
            MesApiNames.MaterialControl => MockOk("物料校验成功"),
            MesApiNames.InStationCheck => MockOk("工序检查成功", details: new[] { new { productMode = "0" } }),
            MesApiNames.SubProductBinding => MockOk("绑定成功"),
            MesApiNames.ProductBinding => MockOk("上传成功"),
            MesApiNames.GetTestData => MockOk("获取成功", details: MockTestDataDetails()),
            MesApiNames.OutStationCheckData => MockOk("过站成功"),
            MesApiNames.GetCoordinateResult => MockOk("上传成功", details: MockCoordinateDetails()),
            MesApiNames.GetTaryBarcode => MockOk("获取成功", details: MockTrayDetails()),
            _ => MesJsonHelper.Serialize(new MesApiResponse { Status = "false", Result = $"未知接口 {api}" })
        };

        statusCode = 200;
        return true;
    }

    private static bool IsKnownApi(string api) =>
        api is MesApiNames.LoginCheck or MesApiNames.GetSpecifications or MesApiNames.MaterialControl
            or MesApiNames.InStationCheck or MesApiNames.SubProductBinding or MesApiNames.ProductBinding
            or MesApiNames.GetTestData or MesApiNames.OutStationCheckData or MesApiNames.GetCoordinateResult
            or MesApiNames.GetTaryBarcode;

    private static string? ExtractApiName(string path)
    {
        var p = path.TrimEnd('/');
        var idx = p.LastIndexOf('/');
        if (idx < 0)
        {
            return null;
        }

        var name = p[(idx + 1)..];
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    private static string? ExtractJsonData(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        // application/x-www-form-urlencoded
        var parts = body.Split('&');
        foreach (var part in parts)
        {
            var kv = part.Split('=', 2);
            if (kv.Length == 2 && string.Equals(kv[0], "jsonData", StringComparison.OrdinalIgnoreCase))
            {
                return Uri.UnescapeDataString(kv[1].Replace('+', ' '));
            }
        }

        // 直接 JSON body fallback
        if (body.TrimStart().StartsWith('{'))
        {
            return body;
        }

        return null;
    }

    private static string MockLogin(string jsonData)
    {
        using var doc = JsonDocument.Parse(jsonData);
        var root = doc.RootElement;
        var user = root.TryGetProperty("userNo", out var u) ? u.GetString() : "";
        if (string.IsNullOrWhiteSpace(user))
        {
            return MesJsonHelper.Serialize(new MesApiResponse
            {
                Status = "false",
                Result = "工号不存在或已失效"
            });
        }

        return MesJsonHelper.Serialize(new MesApiResponse
        {
            Status = "true",
            Result = Guid.NewGuid().ToString("N")[..8],
            TestResult = "登录成功",
            Remark = ""
        });
    }

    private static string MockOk(string msg, object? details = null)
    {
        var resp = new Dictionary<string, object>
        {
            ["status"] = "true",
            ["result"] = msg,
            ["testResult"] = msg,
            ["remark"] = ""
        };
        if (details is not null)
        {
            resp["testResultDetails"] = details;
        }

        return JsonSerializer.Serialize(resp);
    }

    private static object[] MockSpecDetails() =>
    [
        new { paramCode = "1001", paramName = "长度", paramFirstUpper = "100", paramFirstLower = "10", paramReTestUpper = "100", paramReTestLower = "10", paramUnit = "mm" },
        new { paramCode = "1002", paramName = "宽度", paramFirstUpper = "100", paramFirstLower = "10", paramReTestUpper = "100", paramReTestLower = "10", paramUnit = "mm" }
    ];

    private static object[] MockTestDataDetails() =>
    [
        new { productSn = "sn001", groupCode = "OCV1", paramCode = "1001", paramName = "电压", paramValue = "2000", deviceNo = "L1NJOCVMC00101" }
    ];

    private static object[] MockCoordinateDetails() =>
    [
        new { cellSn = "cell123", step = "1", sorting = "0", result = "0" }
    ];

    private static object[] MockTrayDetails() =>
    [
        new { productSn = "123456", position = "1", nextGroupCode = "COCV02" },
        new { productSn = "123457", position = "2", nextGroupCode = "COCV02" }
    ];
}
