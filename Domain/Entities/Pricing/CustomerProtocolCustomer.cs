using Domain.Entities.Customers;

namespace Domain.Entities.Pricing;

/// <summary>
/// 客户协议价与客户的绑定关系实体。
/// </summary>
public class CustomerProtocolCustomer
{
    /// <summary>
    /// 客户协议价 ID。
    /// </summary>
    public Guid CustomerProtocolId { get; set; }

    /// <summary>
    /// 客户 ID。
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// 客户协议价。
    /// </summary>
    public virtual CustomerProtocol CustomerProtocol { get; set; } = null!;

    /// <summary>
    /// 客户。
    /// </summary>
    public virtual Customer Customer { get; set; } = null!;
}
