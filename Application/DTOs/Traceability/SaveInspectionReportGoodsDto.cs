using Domain.Entities.Traceability;
namespace Application.DTOs.Traceability;
/// <summary>创建或编辑报告时的送检商品输入。</summary>
public class SaveInspectionReportGoodsDto
{
    /// <summary>来源采购入库商品明细主键。</summary>
    public Guid StockInDetailId { get; set; }
    /// <summary>送检数量，按来源入库单位计量且必须大于零。</summary>
    public decimal SampleQuantity { get; set; }
    /// <summary>商品行检测结论。</summary>
    public InspectionConclusion Conclusion { get; set; }
    /// <summary>商品行检测说明。</summary>
    public string? Remark { get; set; }
}
