namespace SkyRoc.Tests.Testing.PostgreSql;

/// <summary>
///     检测报告与溯源链路生成结果，区分本轮新增和已存在记录。
/// </summary>
internal sealed record DemoDataTraceabilityGenerationResult(
    int CreatedInspectionReports,
    int ReusedInspectionReports,
    int CreatedInspectionReportGoods,
    int ReusedInspectionReportGoods,
    int CreatedInspectionAttachments,
    int ReusedInspectionAttachments,
    int CreatedTraceSaleOrders,
    int ReusedTraceSaleOrders,
    int CreatedTraceSaleOrderDetails,
    int ReusedTraceSaleOrderDetails,
    int CreatedTraceSaleOrderAuditLogs,
    int ReusedTraceSaleOrderAuditLogs,
    int CreatedTraceStockOuts,
    int ReusedTraceStockOuts,
    int CreatedTraceStockOutDetails,
    int ReusedTraceStockOutDetails,
    int CreatedTraceStockOutLedgers,
    int ReusedTraceStockOutLedgers,
    int CreatedTraceRecords,
    int ReusedTraceRecords);
