namespace Domain.Entities;

/// <summary>
///     角色实体
/// </summary>
public class Role : BaseEntity
{
    /// <summary>
    ///     角色名称
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    ///     角色编码
    /// </summary>
    public required string Code { get; set; }

    /// <summary>
    ///     角色描述
    /// </summary>
    public string? Desc { get; set; }

    /// <summary>
    ///     导航属性：用户角色关联 (多对多)
    /// </summary>
    public virtual ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();

    /// <summary>
    ///     导航属性：角色菜单关联 (多对多)
    /// </summary>
    public virtual ICollection<RoleMenu> RoleMenus { get; private set; } = new List<RoleMenu>();
}