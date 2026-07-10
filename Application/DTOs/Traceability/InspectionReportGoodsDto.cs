using Domain.Entities.Traceability;
namespace Application.DTOs.Traceability;
/// <summary>检测报告商品响应，展示入库商品、批次、送检数量和单品结论快照。</summary>
public class InspectionReportGoodsDto : BaseDto
{
    /// <summary>来源采购入库明细主键。</summary>
    public Guid StockInDetailId { get; set; }
    /// <summary>商品主键。</summary>
    public Guid GoodsId { get; set; }
    /// <summary>商品名称快照。</summary>
    public string GoodsName { get; set; } = string.Empty;
    /// <summary>商品编码快照。</summary>
    public string GoodsCode { get; set; } = string.Empty;
    /// <summary>商品分类名称快照。</summary>
    public string? GoodsTypeName { get; set; }
    /// <summary>送检单位主键。</summary>
    public Guid GoodsUnitId { get; set; }
    /// <summary>送检单位名称快照。</summary>
    public string GoodsUnitName { get; set; } = string.Empty;
    /// <summary>送检数量。</summary>
    public decimal SampleQuantity { get; set; }
    /// <summary>入库批次号。</summary>
    public string BatchNo { get; set; } = string.Empty;
    /// <summary>商品行检测结论。</summary>
    public InspectionConclusion Conclusion { get; set; }
    /// <summary>商品行检测说明。</summary>
    public string? Remark { get; set; }
}
