using System.Linq.Expressions;
using Common.Constants;
using Domain.Entities;

namespace Application.QueryParameters;

/// <summary>
///     角色查询参数
/// </summary>
public class RoleQueryParameters : PagedQueryParameters
{
    /// <summary>
    ///     角色名称
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///     角色编码
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    ///     启用状态
    /// </summary>
    public Status? Status { get; set; }

    /// <summary>
    ///     查询表达式
    /// </summary>
    /// <returns></returns>
    public Expression<Func<Role, bool>> QueryBuild()
    {
        return r =>
            (string.IsNullOrWhiteSpace(Name) || r.Name.Contains(Name.Trim())) &&
            (string.IsNullOrWhiteSpace(Code) || r.Code.Contains(Code.Trim())) &&
            (!Status.HasValue || r.Status == Status.Value);
    }
}