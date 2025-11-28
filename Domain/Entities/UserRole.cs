namespace Domain.Entities;

/// <summary>
///     用户角色关联表 (多对多关系)
/// </summary>
public class UserRole
{
    /// <summary>
    ///     用户ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    ///     角色ID
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    ///     导航属性：用户
    /// </summary>
    public virtual User? User { get; set; }

    /// <summary>
    ///     导航属性：角色
    /// </summary>
    public virtual Role? Role { get; set; }
}