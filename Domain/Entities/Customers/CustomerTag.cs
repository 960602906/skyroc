namespace Domain.Entities.Customers;

/// <summary>
/// 客户标签实体，支持客户分组和树形标签。
/// </summary>
public class CustomerTag : BaseEntity
{
    /// <summary>
    /// 标签名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 标签编码。
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 父级标签 ID。
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// 排序值。
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 父级标签。
    /// </summary>
    public virtual CustomerTag? Parent { get; set; }

    /// <summary>
    /// 子级标签集合。
    /// </summary>
    public virtual ICollection<CustomerTag> Children { get; set; } = new List<CustomerTag>();

    /// <summary>
    /// 客户标签关系集合。
    /// </summary>
    public virtual ICollection<CustomerTagRelation> CustomerRelations { get; set; } = new List<CustomerTagRelation>();
}
