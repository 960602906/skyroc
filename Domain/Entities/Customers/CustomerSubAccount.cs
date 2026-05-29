namespace Domain.Entities.Customers;

/// <summary>
/// 客户子账号实体，用于客户公司下的下单或查看账号。
/// </summary>
public class CustomerSubAccount : BaseEntity
{
    /// <summary>
    /// 所属公司 ID。
    /// </summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// 授权客户 ID。
    /// </summary>
    public Guid? CustomerId { get; set; }

    /// <summary>
    /// 登录账号。
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 昵称。
    /// </summary>
    public string NickName { get; set; } = string.Empty;

    /// <summary>
    /// 手机号。
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// 邮箱。
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 密码哈希值。
    /// </summary>
    public string? PasswordHash { get; set; }

    /// <summary>
    /// 备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 所属公司。
    /// </summary>
    public virtual Company Company { get; set; } = null!;

    /// <summary>
    /// 授权客户。
    /// </summary>
    public virtual Customer? Customer { get; set; }
}
