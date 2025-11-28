namespace Domain.Entities;

/// <summary>
///     角色菜单关联表 (多对多关系)
/// </summary>
public class RoleMenu
{
    /// <summary>
    ///     角色ID
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    ///     菜单ID
    /// </summary>
    public Guid MenuId { get; set; }

    /// <summary>
    ///     导航属性：角色
    /// </summary>
    public virtual Role? Role { get; set; }

    /// <summary>
    ///     导航属性：菜单
    /// </summary>
    public virtual Menu? Menu { get; set; }
}