namespace Application.DTOs.Auth;

/// <summary>
///     路由信息
/// </summary>
public class RoutesDto
{
    /// <summary>
    ///     主键ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     菜单名称
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///     路由路径
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    ///     只有第一级或最后一级路由才有该属性，作为布局组件或者页面组件
    /// </summary>
    public string? Component { get; set; }

    /// <summary>
    ///     元信息
    /// </summary>
    public RoutesHandleDto Handle { get; set; } = new();

    /// <summary>
    ///     父菜单ID (支持无限级菜单)
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    ///     子菜单集合
    /// </summary>
    public IEnumerable<RoutesDto>? Children { get; set; }
}