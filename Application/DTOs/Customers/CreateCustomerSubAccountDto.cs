using Shared.Constants;

namespace Application.DTOs.Customers;

/// <summary>
///     创建客户子账号 DTO。
/// </summary>
public class CreateCustomerSubAccountDto
{
    /// <summary>
    ///     所属公司 ID。
    /// </summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    ///     授权客户 ID。
    /// </summary>
    public Guid? CustomerId { get; set; }

    /// <summary>
    ///     登录账号。
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    ///     昵称。
    /// </summary>
    public string? NickName { get; set; }

    /// <summary>
    ///     手机号。
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    ///     邮箱。
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    ///     密码哈希值。
    /// </summary>
    public string? PasswordHash { get; set; }

    /// <summary>
    ///     备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    ///     启用禁用状态。
    /// </summary>
    public Status? Status { get; set; }
}

