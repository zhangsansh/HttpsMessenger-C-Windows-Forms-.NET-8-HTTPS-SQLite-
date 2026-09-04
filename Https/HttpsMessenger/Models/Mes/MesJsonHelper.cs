using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HttpsMessenger.Models.Mes;

public static class MesJsonHelper
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = null,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    public static T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, Options);

    public static string NowTimestamp() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
}

public sealed class MesApiResponse
{
    public string Status { get; set; } = "";
    public string Result { get; set; } = "";
    public string TestResult { get; set; } = "";
    public string Remark { get; set; } = "";
    public List<JsonElement>? TestResultDetails { get; set; }

    public bool IsSuccess =>
        string.Equals(Status, "true", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Status, "True", StringComparison.OrdinalIgnoreCase);
}

public sealed class MesSubProductItem
{
    public string SubProductSn { get; set; } = "";
    public string Position { get; set; } = "";
}

public sealed class MesTestParamItem
{
    public string ParamCode { get; set; } = "";
    public string ParamName { get; set; } = "";
    public string ParamValue { get; set; } = "";
    public string ParamResult { get; set; } = "";
    public string ParamUnit { get; set; } = "";
}

public sealed class MesStepDataItem
{
    public string TrayNo { get; set; } = "";
    public string GroupCode { get; set; } = "";
    public string ChannelNo { get; set; } = "";
    public string BatchNo { get; set; } = "";
    public string Step { get; set; } = "";
    public string StepName { get; set; } = "";
    public string StartDate { get; set; } = "";
    public string EndDate { get; set; } = "";
    public string CirculatingNumber { get; set; } = "";
    public string TurnTime { get; set; } = "";
    public string EndElectricity { get; set; } = "";
    public string Capacity { get; set; } = "";
    public string Energy { get; set; } = "";
    public string ConstantCurrent { get; set; } = "";
    public string StartVol { get; set; } = "";
    public string MidVol { get; set; } = "";
    public string EndVol { get; set; } = "";
    public string ChargeElectricity { get; set; } = "";
    public string Marking { get; set; } = "";
    public string EndTemperature { get; set; } = "";
    public string Avgvol { get; set; } = "";
    public string UpLoadPath { get; set; } = "";
    public string MaxHousetemp { get; set; } = "";
    public string MinHousetemp { get; set; } = "";
    public string MaxCellTemperature { get; set; } = "";
    public string MinCellTemperature { get; set; } = "";
    public string StartCellTemperature { get; set; } = "";
    public string EndCellTemperature { get; set; } = "";
    public string StartFirstHousetemp { get; set; } = "";
    public string EndFirstHousetemp { get; set; } = "";
    public string StartAfterHousetemp { get; set; } = "";
    public string EndAfterHousetemp { get; set; } = "";
}
