using Common.Constants;

namespace Application.DTOs.Role;

/// <summary>
///     创建角色 DTO - 用于接收创建角色的请求
/// </summary>
public class CreateRoleDto
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
    
    public Status? Status { get; set; }
}