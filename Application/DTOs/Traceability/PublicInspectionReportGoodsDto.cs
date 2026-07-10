using Domain.Entities.Traceability;

namespace Application.DTOs.Traceability;

/// <summary>二维码公开检测报告商品信息，不包含来源入库行或商品档案内部主键。</summary>
public class PublicInspectionReportGoodsDto
{
    /// <summary>商品名称快照。</summary>
    public string GoodsName { get; set; } = string.Empty;
    /// <summary>商品编码快照。</summary>
    public string GoodsCode { get; set; } = string.Empty;
    /// <summary>商品分类名称快照。</summary>
    public string? GoodsTypeName { get; set; }
    /// <summary>送检数量，按送检单位计量。</summary>
    public decimal SampleQuantity { get; set; }
    /// <summary>送检单位名称快照。</summary>
    public string GoodsUnitName { get; set; } = string.Empty;
    /// <summary>入库批次号快照。</summary>
    public string BatchNo { get; set; } = string.Empty;
    /// <summary>商品行检测结论。</summary>
    public InspectionConclusion Conclusion { get; set; }
}
