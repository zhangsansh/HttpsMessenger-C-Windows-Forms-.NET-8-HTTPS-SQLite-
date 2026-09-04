using System.Net.Http.Headers;
using System.Net.Security;
using System.Text;
using System.Text.Json;
using HttpsMessenger.Models.Mes;

namespace HttpsMessenger.Services.Mes;

/// <summary>
/// MES HTTPS POST 客户端：Head/Body 分离，表单提交 jsonData。
/// Content-Type: application/x-www-form-urlencoded
/// </summary>
public sealed class MesApiClient : IDisposable
{
    private HttpClient? _httpClient;
    private MesConfig _config;

    public MesConfig Config => _config;

    public MesApiClient(MesConfig config)
    {
        _config = config;
        RecreateClient();
    }

    public void UpdateConfig(MesConfig config)
    {
        _config = config;
        RecreateClient();
    }

    private void RecreateClient()
    {
        _httpClient?.Dispose();
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = _config.IgnoreCertificateErrors
                ? static (_, _, _, _) => true
                : static (_, _, _, e) => e == SslPolicyErrors.None
        };
        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(Math.Clamp(_config.TimeoutSeconds, 5, 300))
        };
    }

    public async Task<MesCallResult> PostAsync(string apiName, object jsonPayload, CancellationToken token = default)
    {
        var jsonData = MesJsonHelper.Serialize(jsonPayload);
        return await PostJsonDataAsync(apiName, jsonData, token).ConfigureAwait(false);
    }

    public async Task<MesCallResult> PostJsonDataAsync(string apiName, string jsonData, CancellationToken token = default)
    {
        var endpoint = _config.GetEndpoint(apiName);
        var url = BuildUrl(_config.BaseUrl, endpoint);

        // 表单形参提交，禁止拼接到 URL
        var formBody = "jsonData=" + Uri.EscapeDataString(jsonData);
        using var content = new StringContent(formBody, Encoding.UTF8, "application/x-www-form-urlencoded");

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.TryAddWithoutValidation("User-Agent", "HttpsMessenger-MES/1.0");

        try
        {
            using var response = await _httpClient!.SendAsync(request, token).ConfigureAwait(false);
            var responseText = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

            MesApiResponse? api;
            try
            {
                api = MesJsonHelper.Deserialize<MesApiResponse>(responseText);
            }
            catch
            {
                api = new MesApiResponse
                {
                    Status = response.IsSuccessStatusCode ? "true" : "false",
                    Result = responseText
                };
            }

            api ??= new MesApiResponse { Status = "false", Result = "空响应" };

            return new MesCallResult
            {
                Success = response.IsSuccessStatusCode && api.IsSuccess,
                HttpStatusCode = (int)response.StatusCode,
                RequestUrl = url,
                RequestJsonData = jsonData,
                ResponseRaw = responseText,
                Response = api,
                ApiName = apiName
            };
        }
        catch (Exception ex)
        {
            return new MesCallResult
            {
                Success = false,
                RequestUrl = url,
                RequestJsonData = jsonData,
                ResponseRaw = ex.Message,
                Response = new MesApiResponse { Status = "false", Result = ex.Message },
                ApiName = apiName,
                IsNetworkError = true,
                ErrorMessage = ex.Message
            };
        }
    }

    public static string BuildUrl(string baseUrl, string path)
    {
        baseUrl = (baseUrl ?? "").Trim().TrimEnd('/');
        path = (path ?? "").Trim();
        if (!path.StartsWith('/'))
        {
            path = "/" + path;
        }

        if (!baseUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            baseUrl = "https://" + baseUrl;
        }

        return baseUrl + path;
    }

    public void Dispose() => _httpClient?.Dispose();
}

public sealed class MesCallResult
{
    public string ApiName { get; set; } = "";
    public bool Success { get; set; }
    public bool IsNetworkError { get; set; }
    public int HttpStatusCode { get; set; }
    public string RequestUrl { get; set; } = "";
    public string RequestJsonData { get; set; } = "";
    public string ResponseRaw { get; set; } = "";
    public MesApiResponse Response { get; set; } = new();
    public string ErrorMessage { get; set; } = "";

    public string UserMessage =>
        Success ? (Response.Result ?? "OK") : (Response.Result ?? ErrorMessage ?? "失败");
}
