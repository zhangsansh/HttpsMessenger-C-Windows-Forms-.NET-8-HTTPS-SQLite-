namespace HttpsMessenger.Services.Mes;

/// <summary>
/// MES 接口路径配置（可配置，不拼接至 URL 参数）。
/// </summary>
public sealed class MesConfig
{
    public string BaseUrl { get; set; } = "https://127.0.0.1:8443/mes";
    public bool IgnoreCertificateErrors { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 30;
    public int OfflineRetryMax { get; set; } = 5;
    public int ServerTimeToleranceSeconds { get; set; } = 30;

    public string LoginCheckPath { get; set; } = "/LoginCheck";
    public string GetSpecificationsPath { get; set; } = "/GetSpecifications";
    public string MaterialControlPath { get; set; } = "/MaterialControl";
    public string InStationCheckPath { get; set; } = "/InStationCheck";
    public string SubProductBindingPath { get; set; } = "/SubProductBinding";
    public string ProductBindingPath { get; set; } = "/ProductBinding";
    public string GetTestDataPath { get; set; } = "/GetTestData";
    public string OutStationCheckDataPath { get; set; } = "/OutStationCheckData";
    public string GetCoordinateResultPath { get; set; } = "/GetCoordinateResult";
    public string GetTaryBarcodePath { get; set; } = "/GetTaryBarcode";

    public string GetEndpoint(string apiName) => apiName switch
    {
        MesApiNames.LoginCheck => LoginCheckPath,
        MesApiNames.GetSpecifications => GetSpecificationsPath,
        MesApiNames.MaterialControl => MaterialControlPath,
        MesApiNames.InStationCheck => InStationCheckPath,
        MesApiNames.SubProductBinding => SubProductBindingPath,
        MesApiNames.ProductBinding => ProductBindingPath,
        MesApiNames.GetTestData => GetTestDataPath,
        MesApiNames.OutStationCheckData => OutStationCheckDataPath,
        MesApiNames.GetCoordinateResult => GetCoordinateResultPath,
        MesApiNames.GetTaryBarcode => GetTaryBarcodePath,
        _ => "/" + apiName
    };

    public static string ConfigPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "HttpsMessenger",
        "mes_config.json");
}

public static class MesApiNames
{
    public const string LoginCheck = "LoginCheck";
    public const string GetSpecifications = "GetSpecifications";
    public const string MaterialControl = "MaterialControl";
    public const string InStationCheck = "InStationCheck";
    public const string SubProductBinding = "SubProductBinding";
    public const string ProductBinding = "ProductBinding";
    public const string GetTestData = "GetTestData";
    public const string OutStationCheckData = "OutStationCheckData";
    public const string GetCoordinateResult = "GetCoordinateResult";
    public const string GetTaryBarcode = "GetTaryBarcode";
}

public static class MesConfigService
{
    public static MesConfig Load()
    {
        try
        {
            if (File.Exists(MesConfig.ConfigPath))
            {
                return System.Text.Json.JsonSerializer.Deserialize<MesConfig>(
                           File.ReadAllText(MesConfig.ConfigPath))
                       ?? new MesConfig();
            }
        }
        catch
        {
            // ignore
        }

        return new MesConfig();
    }

    public static void Save(MesConfig config)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(MesConfig.ConfigPath)!);
        File.WriteAllText(MesConfig.ConfigPath,
            System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            }));
    }
}

public sealed class MesSessionState
{
    public string OperatorId { get; set; } = "";
    public string DeviceSn { get; set; } = "";
    public string MoNumber { get; set; } = "";
    public string GroupCode { get; set; } = "";
    public string UserNo { get; set; } = "";
    public DateTime LoginTime { get; set; }

    public bool IsLoggedIn => !string.IsNullOrWhiteSpace(OperatorId);
}
