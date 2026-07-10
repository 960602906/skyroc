using Domain.Entities.Traceability;
namespace Application.DTOs.Traceability;
/// <summary>创建或编辑检测报告请求，商品明细必须全部来自同一已审核采购入库单。</summary>
public class SaveInspectionReportDto
{
    /// <summary>来源采购入库单主键；编辑时不得更换来源。</summary>
    public Guid StockInOrderId { get; set; }
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
    /// <summary>送检商品明细。</summary>
    public List<SaveInspectionReportGoodsDto> Goods { get; set; } = [];
    /// <summary>已上传文件的元数据。</summary>
    public List<SaveInspectionAttachmentDto> Attachments { get; set; } = [];
}
