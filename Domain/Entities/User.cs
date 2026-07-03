using Shared.Constants;

namespace Domain.Entities;

/// <summary>
///     用户实体
/// </summary>
public class User : BaseEntity
{
    /// <summary>
    ///     用户名
    /// </summary>
    public required string Username { get; set; }

    /// <summary>
    ///     性别
    /// </summary>
    public required GenderType Gender { get; set; }

    /// <summary>
    /// 部门ID ⭐ 新增
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    ///     昵称
    /// </summary>
    public required string NickName { get; set; }

    /// <summary>
    ///     电话
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    ///     邮箱
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    ///     密码哈希值
    /// </summary>
    public required string PasswordHash { get; set; }

    // 导航属性
    /// <summary>
    /// 用户所属部门导航属性。
    /// </summary>
    public virtual Department? Department { get; set; } // ⭐ 新增

    /// <summary>
    ///     导航属性：用户角色关联 (多对多)
    /// </summary>
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
