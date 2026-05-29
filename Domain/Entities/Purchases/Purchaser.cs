namespace Domain.Entities.Purchases;

/// <summary>
/// 采购员实体，可关联系统用户和部门。
/// </summary>
public class Purchaser : BaseEntity
{
    /// <summary>
    /// 采购员名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 采购员编码。
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 联系电话。
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// 关联系统用户 ID。
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// 所属部门 ID。
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// 备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 关联系统用户。
    /// </summary>
    public virtual User? User { get; set; }

    /// <summary>
    /// 所属部门。
    /// </summary>
    public virtual Department? Department { get; set; }

    /// <summary>
    /// 采购员关联的采购规则集合。
    /// </summary>
    public virtual ICollection<PurchaseRule> PurchaseRules { get; set; } = new List<PurchaseRule>();
}
