namespace Application.DTOs.Purchases;

/// <summary>
/// 采购计划商品明细 DTO，携带采购单位和按采购单位计量的数量。
/// </summary>
public class PurchasePlanDetailDto : BaseDto
{
    /// <summary>
    /// 所属采购计划主键。
    /// </summary>
    public Guid PurchasePlanId { get; set; }

    /// <summary>
    /// 关联商品主键。
    /// </summary>
    public Guid GoodsId { get; set; }

    /// <summary>
    /// 业务发生时的商品名称快照。
    /// </summary>
    public string GoodsName { get; set; } = string.Empty;

    /// <summary>
    /// 业务发生时的商品编码快照。
    /// </summary>
    public string GoodsCode { get; set; } = string.Empty;

    /// <summary>
    /// 采购计量单位主键。
    /// </summary>
    public Guid PurchaseUnitId { get; set; }

    /// <summary>
    /// 计划生成时的采购单位名称快照。
    /// </summary>
    public string PurchaseUnitName { get; set; } = string.Empty;

    /// <summary>
    /// 来源订单需求数量，按采购单位计量。
    /// </summary>
    public decimal RequiredQuantity { get; set; }

    /// <summary>
    /// 计划采购数量，按采购单位计量。
    /// </summary>
    public decimal PlannedQuantity { get; set; }

    /// <summary>
    /// 已生成采购单的数量，按采购单位计量。
    /// </summary>
    public decimal PurchasedQuantity { get; set; }

    /// <summary>
    /// 业务备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 来源销售订单明细关系集合。
    /// </summary>
    public List<PurchasePlanOrderRelationDto> OrderRelations { get; set; } = [];
}
