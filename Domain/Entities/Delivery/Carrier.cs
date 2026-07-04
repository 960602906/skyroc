namespace Domain.Entities.Delivery;

/// <summary>
/// 承运商实体，维护负责配送履约的第三方物流公司基础资料。
/// </summary>
public class Carrier : BaseEntity
{
    /// <summary>
    /// 承运商名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 承运商编码，全局唯一。
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 联系人姓名。
    /// </summary>
    public string? ContactName { get; set; }

    /// <summary>
    /// 联系电话。
    /// </summary>
    public string? ContactPhone { get; set; }

    /// <summary>
    /// 承运商地址。
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// 备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 归属该承运商的司机集合。
    /// </summary>
    public virtual ICollection<Driver> Drivers { get; set; } = new List<Driver>();
}
