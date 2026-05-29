using Domain.Entities.Customers;

namespace Domain.Entities.Purchases;

/// <summary>
/// 采购规则与客户的绑定关系实体。
/// </summary>
public class PurchaseRuleCustomer
{
    /// <summary>
    /// 采购规则 ID。
    /// </summary>
    public Guid PurchaseRuleId { get; set; }

    /// <summary>
    /// 客户 ID。
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// 采购规则。
    /// </summary>
    public virtual PurchaseRule PurchaseRule { get; set; } = null!;

    /// <summary>
    /// 客户。
    /// </summary>
    public virtual Customer Customer { get; set; } = null!;
}
