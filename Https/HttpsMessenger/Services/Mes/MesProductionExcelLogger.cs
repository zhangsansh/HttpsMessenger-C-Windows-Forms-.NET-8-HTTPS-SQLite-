using ClosedXML.Excel;

namespace HttpsMessenger.Services.Mes;

/// <summary>
/// 生产数据 Excel 日志：仅在上传成功后写入。
/// </summary>
public sealed class MesProductionExcelLogger
{
    public string LogDirectory { get; }

    public MesProductionExcelLogger(string? directory = null)
    {
        LogDirectory = directory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HttpsMessenger",
            "production_logs");
        Directory.CreateDirectory(LogDirectory);
    }

    public string TodayFilePath => Path.Combine(LogDirectory, $"Production_{DateTime.Now:yyyyMMdd}.xlsx");

    public void LogUploadSuccess(string apiName, string productSn, string moNumber, string testResult,
        string requestJson, string responseJson)
    {
        var path = TodayFilePath;
        using var wb = File.Exists(path) ? new XLWorkbook(path) : new XLWorkbook();
        var ws = wb.Worksheets.FirstOrDefault(w => w.Name == "UploadLog")
                 ?? wb.Worksheets.Add("UploadLog");

        if (ws.LastRowUsed()?.RowNumber() is null or 0)
        {
            ws.Cell(1, 1).Value = "时间";
            ws.Cell(1, 2).Value = "接口";
            ws.Cell(1, 3).Value = "产品条码";
            ws.Cell(1, 4).Value = "制令单";
            ws.Cell(1, 5).Value = "结果";
            ws.Cell(1, 6).Value = "请求JSON";
            ws.Cell(1, 7).Value = "响应JSON";
            ws.Row(1).Style.Font.Bold = true;
        }

        var row = (ws.LastRowUsed()?.RowNumber() ?? 1) + 1;
        ws.Cell(row, 1).Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        ws.Cell(row, 2).Value = apiName;
        ws.Cell(row, 3).Value = productSn;
        ws.Cell(row, 4).Value = moNumber;
        ws.Cell(row, 5).Value = testResult;
        ws.Cell(row, 6).Value = requestJson;
        ws.Cell(row, 7).Value = responseJson;
        ws.Columns().AdjustToContents();
        wb.SaveAs(path);
    }

    public void LogProductionData(string productSn, string groupCode, string paramCode, string paramName,
        string paramValue, string paramResult, string paramUnit = "")
    {
        var path = TodayFilePath;
        using var wb = File.Exists(path) ? new XLWorkbook(path) : new XLWorkbook();
        var ws = wb.Worksheets.FirstOrDefault(w => w.Name == "ProductionData")
                 ?? wb.Worksheets.Add("ProductionData");

        if (ws.LastRowUsed()?.RowNumber() is null or 0)
        {
            ws.Cell(1, 1).Value = "时间";
            ws.Cell(1, 2).Value = "产品条码";
            ws.Cell(1, 3).Value = "工序";
            ws.Cell(1, 4).Value = "参数代码";
            ws.Cell(1, 5).Value = "参数名称";
            ws.Cell(1, 6).Value = "参数值";
            ws.Cell(1, 7).Value = "结果";
            ws.Cell(1, 8).Value = "单位";
            ws.Row(1).Style.Font.Bold = true;
        }

        var row = (ws.LastRowUsed()?.RowNumber() ?? 1) + 1;
        ws.Cell(row, 1).Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        ws.Cell(row, 2).Value = productSn;
        ws.Cell(row, 3).Value = groupCode;
        ws.Cell(row, 4).Value = paramCode;
        ws.Cell(row, 5).Value = paramName;
        ws.Cell(row, 6).Value = paramValue;
        ws.Cell(row, 7).Value = paramResult;
        ws.Cell(row, 8).Value = paramUnit;
        wb.SaveAs(path);
    }
}
