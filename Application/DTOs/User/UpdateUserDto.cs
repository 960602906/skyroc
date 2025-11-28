using Common.Constants;

namespace Application.DTOs.User;

public class UpdateUserDto
{
    /// <summary>
    ///     主键ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     用户名
    /// </summary>
    public string? UserName { get; set; }

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
    ///     分配的角色 ID 集合
    /// </summary>
    public IEnumerable<Guid> RoleIds { get; set; } = [];
}