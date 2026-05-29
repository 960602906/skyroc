namespace Domain.Entities.Customers;

/// <summary>
/// 客户与客户标签的多对多关系实体。
/// </summary>
public class CustomerTagRelation
{
    /// <summary>
    /// 客户 ID。
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// 客户标签 ID。
    /// </summary>
    public Guid CustomerTagId { get; set; }

    /// <summary>
    /// 客户。
    /// </summary>
    public virtual Customer Customer { get; set; } = null!;

    /// <summary>
    /// 客户标签。
    /// </summary>
    public virtual CustomerTag CustomerTag { get; set; } = null!;
}
