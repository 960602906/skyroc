namespace Domain.Entities.Delivery;

/// <summary>
/// 配送路线实体，维护配送分区路线并关联该路线覆盖的客户。
/// </summary>
public class DeliveryRoute : BaseEntity
{
    /// <summary>
    /// 路线名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 路线编码，全局唯一。
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 路线描述，说明覆盖区域或配送顺序。
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 同级路线的排序值，数值越小越靠前。
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 路线覆盖的客户关系集合。
    /// </summary>
    public virtual ICollection<CustomerRoute> CustomerRoutes { get; set; } = new List<CustomerRoute>();
}
