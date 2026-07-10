namespace Application.DTOs.Traceability;
/// <summary>二维码详情响应，组合商品溯源快照和不含内部标识的公开检测报告信息。</summary>
public class TraceQrCodeDto
{
    /// <summary>二维码公开展示的商品、批次、供应商和仓库信息，不包含订单、客户或内部主键。</summary>
    public PublicTraceQrRecordDto TraceRecord { get; set; } = new();
    /// <summary>来源批次关联的公开检测报告；未出具报告时为空。</summary>
    public PublicInspectionReportDto? InspectionReport { get; set; }
}
