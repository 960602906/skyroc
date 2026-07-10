using Domain.Entities.Traceability;

namespace Application.DTOs.Traceability;

/// <summary>二维码公开检测报告信息，不包含任何内部主键、入库单号、附件内部标识或审计字段。</summary>
public class PublicInspectionReportDto
{
    /// <summary>检测报告业务编号。</summary>
    public string InspectionNo { get; set; } = string.Empty;
    /// <summary>检测机构名称。</summary>
    public string InspectionOrg { get; set; } = string.Empty;
    /// <summary>检测完成时间（UTC）。</summary>
    public DateTime InspectTime { get; set; }
    /// <summary>报告整单检测结论。</summary>
    public InspectionConclusion Conclusion { get; set; }
    /// <summary>报告商品结论列表，仅返回消费者展示所需商品与批次快照。</summary>
    public List<PublicInspectionReportGoodsDto> Goods { get; set; } = [];
}
