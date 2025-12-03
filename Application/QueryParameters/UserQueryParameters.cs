using System.Linq.Expressions;
using Common.Constants;
using Domain.Entities;

namespace Application.QueryParameters;

public class UserQueryParameters : PagedQueryParameters
{
    /// <summary>
    ///     用户名
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    ///     性别
    /// </summary>
    public GenderType? Gender { get; set; }

    /// <summary>
    ///     昵称
    /// </summary>
    public string? NickName { get; set; }

    /// <summary>
    ///     电话
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    ///     邮箱
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    ///     启用状态
    /// </summary>
    public Status? Status { get; set; }

    /// <summary>
    ///     查询表达式
    /// </summary>
    /// <returns></returns>
    public Expression<Func<User, bool>> QueryBuild()
    {
        return u =>
            (string.IsNullOrWhiteSpace(Username) || u.Username.Contains(Username.Trim())) &&
            (string.IsNullOrWhiteSpace(Email) || u.Email.Contains(Email.Trim())) &&
            (string.IsNullOrWhiteSpace(NickName) || u.NickName.Contains(NickName.Trim())) &&
            (string.IsNullOrWhiteSpace(Phone) || (u.Phone != null && u.Phone.Equals(Phone.Trim()))) &&
            (!Gender.HasValue || u.Gender.Equals(Gender)) &&
            (!Status.HasValue || u.Status == Status.Value);
    }
}