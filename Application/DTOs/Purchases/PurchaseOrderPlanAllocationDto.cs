namespace Application.DTOs.Purchases;

/// <summary>
/// 采购单商品行对采购计划明细的数量占用请求。
/// </summary>
public class PurchaseOrderPlanAllocationDto
{
    /// <summary>
    /// 来源采购计划商品明细主键；商品和采购单位必须与采购单商品行一致。
    /// </summary>
    public Guid PurchasePlanDetailId { get; set; }

    /// <summary>
    /// 从来源计划占用的采购数量，按采购单商品行的采购单位计量且必须大于零。
    /// </summary>
    public decimal AllocatedQuantity { get; set; }
}
