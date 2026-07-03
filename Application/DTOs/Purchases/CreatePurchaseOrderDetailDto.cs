namespace Application.DTOs.Purchases;

/// <summary>
/// 手工创建采购单时的商品行请求，不接受采购计划来源占用。
/// </summary>
public class CreatePurchaseOrderDetailDto
{
    /// <summary>
    /// 采购商品主键。
    /// </summary>
    public Guid GoodsId { get; set; }

    /// <summary>
    /// 采购单位主键，必须属于所选商品。
    /// </summary>
    public Guid PurchaseUnitId { get; set; }

    /// <summary>
    /// 业务需求数量，按采购单位计量；省略时等于采购数量。
    /// </summary>
    public decimal? RequiredQuantity { get; set; }

    /// <summary>
    /// 本单采购数量，按采购单位计量且必须大于零。
    /// </summary>
    public decimal PurchaseQuantity { get; set; }

    /// <summary>
    /// 采购单价，币种沿用系统业务币种且不得为负数。
    /// </summary>
    public decimal PurchasePrice { get; set; }

    /// <summary>
    /// 商品生产日期，仅记录自然日；未知时可为空。
    /// </summary>
    public DateOnly? ProductDate { get; set; }

    /// <summary>
    /// 仅针对当前采购商品行的备注。
    /// </summary>
    public string? Remark { get; set; }
}
