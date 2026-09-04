using HttpsMessenger.Models.Mes;
using HttpsMessenger.Services;

namespace HttpsMessenger.Services.Mes;

/// <summary>
/// MES 业务编排：先上传后记录日志；离线入队；失败提示 MES 报错信息。
/// </summary>
public sealed class MesService
{
    private readonly AppLogger _appLogger;
    private readonly MesOfflineStore _offline;
    private readonly MesProductionExcelLogger _excel;
    private MesApiClient _client;
    private MesConfig _config;

    public MesSessionState Session { get; } = new();

    public event Action<MesCallResult>? CallCompleted;

    public MesService(AppLogger appLogger)
    {
        _appLogger = appLogger;
        _config = MesConfigService.Load();
        _client = new MesApiClient(_config);
        _offline = new MesOfflineStore();
        _excel = new MesProductionExcelLogger();
    }

    public MesConfig GetConfig() => _config;

    public void SaveConfig(MesConfig config)
    {
        _config = config;
        MesConfigService.Save(config);
        _client.UpdateConfig(config);
    }

    public int PendingOfflineCount => _offline.CountPending();

    public async Task<MesCallResult> LoginAsync(string userNo, string passWord, string deviceSn,
        string moNumber, string groupCode, CancellationToken token = default)
    {
        var payload = new Dictionary<string, string>
        {
            ["userNo"] = userNo ?? "",
            ["passWord"] = passWord ?? "",
            ["deviceSn"] = deviceSn ?? "",
            ["moNumber"] = moNumber ?? "",
            ["groupCode"] = groupCode ?? "",
            ["timeStamp"] = MesJsonHelper.NowTimestamp()
        };

        var result = await ExecuteAsync(MesApiNames.LoginCheck, payload, token: token).ConfigureAwait(false);
        if (result.Success)
        {
            Session.OperatorId = result.Response.Result ?? "";
            Session.DeviceSn = deviceSn;
            Session.MoNumber = moNumber;
            Session.GroupCode = groupCode;
            Session.UserNo = userNo;
            Session.LoginTime = DateTime.Now;
        }

        return result;
    }

    public Task<MesCallResult> GetSpecificationsAsync(CancellationToken token = default)
    {
        var payload = new Dictionary<string, string>
        {
            ["groupCode"] = Session.GroupCode,
            ["deviceSn"] = Session.DeviceSn,
            ["operatorId"] = Session.OperatorId,
            ["moNumber"] = Session.MoNumber,
            ["timeStamp"] = MesJsonHelper.NowTimestamp()
        };
        return ExecuteAsync(MesApiNames.GetSpecifications, payload, token: token);
    }

    public Task<MesCallResult> MaterialControlAsync(string batchCode, string assemblyNo, string batchCount,
        string loadItemFlag, CancellationToken token = default)
    {
        var payload = new Dictionary<string, string>
        {
            ["groupCode"] = Session.GroupCode,
            ["deviceSn"] = Session.DeviceSn,
            ["operatorId"] = Session.OperatorId,
            ["moNumber"] = Session.MoNumber,
            ["batchCode"] = batchCode ?? "",
            ["assemblyNo"] = assemblyNo ?? "",
            ["batchCount"] = batchCount ?? "",
            ["timeStamp"] = MesJsonHelper.NowTimestamp(),
            ["loadItemFlag"] = loadItemFlag ?? "1"
        };
        return ExecuteAsync(MesApiNames.MaterialControl, payload, token: token);
    }

    public Task<MesCallResult> InStationCheckAsync(string productSn, string isAssemblySn = "1",
        CancellationToken token = default)
    {
        var payload = new Dictionary<string, string>
        {
            ["groupCode"] = Session.GroupCode,
            ["deviceSn"] = Session.DeviceSn,
            ["moNumber"] = Session.MoNumber,
            ["timeStamp"] = MesJsonHelper.NowTimestamp(),
            ["operatorId"] = Session.OperatorId,
            ["IsAssemblySn"] = isAssemblySn ?? "1",
            ["productSn"] = productSn ?? ""
        };
        return ExecuteAsync(MesApiNames.InStationCheck, payload, productSn, token: token);
    }

    public Task<MesCallResult> SubProductBindingAsync(string productSn, List<MesSubProductItem> list,
        CancellationToken token = default)
    {
        var payload = new Dictionary<string, object>
        {
            ["operatorId"] = Session.OperatorId,
            ["productSn"] = productSn ?? "",
            ["timeStamp"] = MesJsonHelper.NowTimestamp(),
            ["productSnList"] = list
        };
        return ExecuteAsync(MesApiNames.SubProductBinding, payload, productSn, token: token);
    }

    public Task<MesCallResult> ProductBindingAsync(string productSn, List<MesSubProductItem> list,
        string testResult, CancellationToken token = default)
    {
        var payload = new Dictionary<string, object>
        {
            ["operatorId"] = Session.OperatorId,
            ["productSn"] = productSn ?? "",
            ["productSnList"] = list,
            ["groupCode"] = Session.GroupCode,
            ["testResult"] = testResult ?? "0"
        };
        return ExecuteAsync(MesApiNames.ProductBinding, payload, productSn, token: token);
    }

    public Task<MesCallResult> GetTestDataAsync(string sn, string snType, CancellationToken token = default)
    {
        var payload = new Dictionary<string, string>
        {
            ["groupCode"] = Session.GroupCode,
            ["operatorId"] = Session.OperatorId,
            ["sn"] = sn ?? "",
            ["snType"] = snType ?? "cell"
        };
        return ExecuteAsync(MesApiNames.GetTestData, payload, sn, token: token);
    }

    public Task<MesCallResult> OutStationCheckDataAsync(string productSn, string testResult,
        List<MesTestParamItem> testData, List<MesTestParamItem>? environment = null,
        List<MesStepDataItem>? stepData = null, string isAssemblySn = "1", CancellationToken token = default)
    {
        var payload = new Dictionary<string, object>
        {
            ["groupCode"] = Session.GroupCode,
            ["deviceSn"] = Session.DeviceSn,
            ["timeStamp"] = MesJsonHelper.NowTimestamp(),
            ["operatorId"] = Session.OperatorId,
            ["productSn"] = productSn ?? "",
            ["moNumber"] = Session.MoNumber,
            ["testResult"] = testResult ?? "0",
            ["IsAssemblySn"] = isAssemblySn ?? "1",
            ["testData"] = testData ?? new List<MesTestParamItem>(),
            ["environment"] = environment ?? new List<MesTestParamItem>(),
            ["stepData"] = stepData ?? new List<MesStepDataItem>()
        };

        return ExecuteAsync(MesApiNames.OutStationCheckData, payload, productSn, Session.MoNumber,
            logProduction: true, token: token);
    }

    public Task<MesCallResult> GetCoordinateResultAsync(string productSn, string snType,
        CancellationToken token = default)
    {
        var payload = new Dictionary<string, string>
        {
            ["groupCode"] = Session.GroupCode,
            ["deviceSn"] = Session.DeviceSn,
            ["timeStamp"] = MesJsonHelper.NowTimestamp(),
            ["operatorId"] = Session.OperatorId,
            ["snType"] = snType ?? "cell",
            ["productSn"] = productSn ?? ""
        };
        return ExecuteAsync(MesApiNames.GetCoordinateResult, payload, productSn, token: token);
    }

    public Task<MesCallResult> GetTaryBarcodeAsync(string taryNo, CancellationToken token = default)
    {
        var payload = new Dictionary<string, string>
        {
            ["groupCode"] = Session.GroupCode,
            ["deviceSn"] = Session.DeviceSn,
            ["taryNo"] = taryNo ?? "",
            ["timeStamp"] = MesJsonHelper.NowTimestamp()
        };
        return ExecuteAsync(MesApiNames.GetTaryBarcode, payload, token: token);
    }

    public async Task<int> RetryOfflineAsync(CancellationToken token = default)
    {
        var pending = _offline.GetPending();
        var ok = 0;
        foreach (var item in pending)
        {
            if (item.RetryCount >= _config.OfflineRetryMax)
            {
                continue;
            }

            var result = await _client.PostJsonDataAsync(item.ApiName, item.JsonData, token).ConfigureAwait(false);
            if (result.Success)
            {
                _offline.MarkUploaded(item.Id, result.ResponseRaw);
                _appLogger.Info($"[MES补传成功] {item.ApiName} Id={item.Id}");
                if (item.ApiName == MesApiNames.OutStationCheckData)
                {
                    _excel.LogUploadSuccess(item.ApiName, item.ProductSn, item.MoNumber,
                        result.Response.TestResult, item.JsonData, result.ResponseRaw);
                }

                ok++;
            }
            else
            {
                _offline.MarkFailed(item.Id, result.UserMessage, item.RetryCount + 1);
                _appLogger.Warn($"[MES补传失败] {item.ApiName} Id={item.Id} {result.UserMessage}");
            }
        }

        return ok;
    }

    private async Task<MesCallResult> ExecuteAsync(string apiName, object payload,
        string productSn = "", string moNumber = "", bool logProduction = false,
        CancellationToken token = default)
    {
        var json = MesJsonHelper.Serialize(payload);
        _appLogger.Info($"[MES请求] {apiName} 开始发送");
        var result = await _client.PostJsonDataAsync(apiName, json, token).ConfigureAwait(false);

        if (result.IsNetworkError)
        {
            var id = _offline.Enqueue(apiName, json, productSn, moNumber ?? Session.MoNumber);
            _appLogger.Warn($"[MES离线入队] {apiName} QueueId={id} {result.ErrorMessage}");
            result.Response.Result = $"网络异常，已加入离线补传队列(Id={id})：{result.ErrorMessage}";
        }
        else if (result.Success)
        {
            // 上传成功后再写日志
            _appLogger.Info($"[MES成功] {apiName} {result.UserMessage}");
            _excel.LogUploadSuccess(apiName, productSn, moNumber ?? Session.MoNumber,
                result.Response.TestResult, json, result.ResponseRaw);

            if (logProduction && payload is Dictionary<string, object> dict
                && dict.TryGetValue("testData", out var td) && td is List<MesTestParamItem> items)
            {
                foreach (var p in items)
                {
                    _excel.LogProductionData(productSn, Session.GroupCode, p.ParamCode, p.ParamName,
                        p.ParamValue, p.ParamResult, p.ParamUnit);
                }
            }
        }
        else
        {
            _appLogger.Error($"[MES失败] {apiName} {result.UserMessage}");
        }

        CallCompleted?.Invoke(result);
        return result;
    }
}
