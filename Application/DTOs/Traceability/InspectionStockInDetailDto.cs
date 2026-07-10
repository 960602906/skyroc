namespace Application.DTOs.Traceability;
/// <summary>采购入库商品选择响应，提供生成报告商品行所需的不可变快照。</summary>
public class InspectionStockInDetailDto
{
    /// <summary>采购入库商品明细主键。</summary>
    public Guid Id { get; set; }
    /// <summary>商品主键。</summary>
    public Guid GoodsId { get; set; }
    /// <summary>商品名称快照。</summary>
    public string GoodsName { get; set; } = string.Empty;
    /// <summary>商品编码快照。</summary>
    public string GoodsCode { get; set; } = string.Empty;
    /// <summary>商品分类名称快照。</summary>
    public string? GoodsTypeName { get; set; }
    /// <summary>入库单位主键。</summary>
    public Guid GoodsUnitId { get; set; }
    /// <summary>入库单位名称快照。</summary>
    public string GoodsUnitName { get; set; } = string.Empty;
    /// <summary>入库数量。</summary>
    public decimal Quantity { get; set; }
    /// <summary>入库批次号。</summary>
    public string BatchNo { get; set; } = string.Empty;
}
