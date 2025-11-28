using Application.DTOs.Menu;

namespace Application.DTOs.Role;

/// <summary>
///     角色 DTO - 用于返回角色信息
/// </summary>
public class RoleDto : BaseDto
{
    /// <summary>
    ///     角色名称
    /// </summary>
    public string? RoleName { get; set; }

    /// <summary>
    ///     角色编码
    /// </summary>
    public string? RoleCode { get; set; }

    /// <summary>
    ///     角色描述
    /// </summary>
    public string? RoleDesc { get; set; }

    /// <summary>
    ///     角色拥有的菜单权限集合
    /// </summary>
    public IEnumerable<MenuDto> Menus { get; set; } = [];
}