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
    public string? Name { get; set; }

    /// <summary>
    ///     角色编码
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    ///     角色描述
    /// </summary>
    public string? Desc { get; set; }
    
    public Status? Status { get; set; }
}