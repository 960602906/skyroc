using Domain.Entities.Purchases;
namespace Application.DTOs.Purchases;

/// <summary>
/// 手工新增采购计划 DTO。
/// </summary>
public class CreatePurchasePlanDto
{
    /// <summary>
    /// 计划采购交期（UTC）。
    /// </summary>
    public DateTime PlanDate { get; set; }

    /// <summary>
    /// 采购模式：供应商直供或市场自采，默认供应商直供。
    /// </summary>
    public PurchasePattern PurchasePattern { get; set; } = PurchasePattern.SupplierDirect;

    /// <summary>
    /// 关联供应商主键，供应商直供模式下可指定。
    /// </summary>
    public Guid? SupplierId { get; set; }

    /// <summary>
    /// 负责采购的采购员主键。
    /// </summary>
    public Guid? PurchaserId { get; set; }

    /// <summary>
    /// 业务备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 采购计划商品明细集合，至少一条。
    /// </summary>
    public List<CreatePurchasePlanDetailDto> Details { get; set; } = [];
}
