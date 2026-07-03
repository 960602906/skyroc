namespace Application.DTOs.Purchases;

/// <summary>
/// 采购单商品行的采购计划来源，记录被占用的计划数量。
/// </summary>
public class PurchaseOrderPlanRelationDto : BaseDto
{
    /// <summary>
    /// 所属采购单商品行主键。
    /// </summary>
    public Guid PurchaseOrderDetailId { get; set; }

    /// <summary>
    /// 来源采购计划商品明细主键。
    /// </summary>
    public Guid PurchasePlanDetailId { get; set; }

    /// <summary>
    /// 来源采购计划主键。
    /// </summary>
    public Guid PurchasePlanId { get; set; }

    /// <summary>
    /// 来源采购计划业务编号。
    /// </summary>
    public string PurchasePlanNo { get; set; } = string.Empty;

    /// <summary>
    /// 从来源计划占用的数量，按采购商品行的采购单位计量。
    /// </summary>
    public decimal AllocatedQuantity { get; set; }
}
