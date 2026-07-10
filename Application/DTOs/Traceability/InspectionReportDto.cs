using Domain.Entities.Traceability;
namespace Application.DTOs.Traceability;
/// <summary>检测报告响应，包含采购入库快照、送检商品和附件清单。</summary>
public class InspectionReportDto : BaseDto
{
    /// <summary>检测报告业务编号。</summary>
    public string InspectionNo { get; set; } = string.Empty;
    /// <summary>来源采购入库单主键。</summary>
    public Guid StockInOrderId { get; set; }
    /// <summary>来源采购入库单编号快照。</summary>
    public string InNo { get; set; } = string.Empty;
    /// <summary>收货仓库主键。</summary>
    public Guid WareId { get; set; }
    /// <summary>收货仓库名称快照。</summary>
    public string WareName { get; set; } = string.Empty;
    /// <summary>供货供应商主键。</summary>
    public Guid? SupplierId { get; set; }
    /// <summary>供货供应商名称快照。</summary>
    public string? SupplierName { get; set; }
    /// <summary>检测机构名称。</summary>
    public string InspectionOrg { get; set; } = string.Empty;
    /// <summary>商品抽样时间（UTC）。</summary>
    public DateTime? SampleTime { get; set; }
    /// <summary>检测完成时间（UTC）。</summary>
    public DateTime InspectTime { get; set; }
    /// <summary>报告整单检测结论。</summary>
    public InspectionConclusion Conclusion { get; set; }
    /// <summary>报告业务备注。</summary>
    public string? Remark { get; set; }
    /// <summary>报告商品明细。</summary>
    public List<InspectionReportGoodsDto> Goods { get; set; } = [];
    /// <summary>报告附件。</summary>
    public List<InspectionAttachmentDto> Attachments { get; set; } = [];
}
