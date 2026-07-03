namespace Application.DTOs.Purchases;

/// <summary>
/// 手工新增采购计划商品明细 DTO。
/// </summary>
public class CreatePurchasePlanDetailDto
{
    /// <summary>
    /// 关联商品主键。
    /// </summary>
    public Guid GoodsId { get; set; }

    /// <summary>
    /// 采购计量单位主键，必须属于所选商品。
    /// </summary>
    public Guid PurchaseUnitId { get; set; }

    /// <summary>
    /// 计划采购数量，按采购单位计量，必须大于零。
    /// </summary>
    public decimal PlannedQuantity { get; set; }

    /// <summary>
    /// 来源订单需求数量，按采购单位计量；手工新增时可留空并回退为计划数量。
    /// </summary>
    public decimal? RequiredQuantity { get; set; }

    /// <summary>
    /// 业务备注。
    /// </summary>
    public string? Remark { get; set; }
}
