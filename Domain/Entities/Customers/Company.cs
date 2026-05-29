namespace Domain.Entities.Customers;

/// <summary>
/// 公司实体，承载客户所属单位或集团资料。
/// </summary>
public class Company : BaseEntity
{
    /// <summary>
    /// 公司名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 公司编码。
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 联系人。
    /// </summary>
    public string? ContactName { get; set; }

    /// <summary>
    /// 联系电话。
    /// </summary>
    public string? ContactPhone { get; set; }

    /// <summary>
    /// 地址。
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// 备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 公司下属客户集合。
    /// </summary>
    public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();

    /// <summary>
    /// 公司子账号集合。
    /// </summary>
    public virtual ICollection<CustomerSubAccount> SubAccounts { get; set; } = new List<CustomerSubAccount>();
}
