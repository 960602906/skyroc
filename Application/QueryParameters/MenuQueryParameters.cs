using System.Linq.Expressions;
using Domain.Entities;
using Shared.Constants;

namespace Application.QueryParameters;

/// <summary>
///     菜单查询参数
/// </summary>
public class MenuQueryParameters : PagedQueryParameters
{
    /// <summary>
    ///     名称
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///     类型
    /// </summary>
    public MenuType? MenuType { get; set; }

    /// <summary>
    ///     是否显示
    /// </summary>
    public bool? IsHidden { get; set; }

    /// <summary>
    ///     启用 禁用
    /// </summary>
    public Status? Status { get; set; }

    /// <summary>
    ///     构建查询条件表达式
    /// </summary>
    public Expression<Func<Menu, bool>> QueryBuild()
    {
        return m =>
            (string.IsNullOrWhiteSpace(Name) || m.Name.Contains(Name.Trim())) &&
            // (!MenuType.HasValue || m.MenuType == MenuType.Value) &&
            // (!IsHidden.HasValue || m.IsHidden == IsHidden.Value) &&
            (!Status.HasValue || m.Status == Status.Value);
    }
}