using Common.Constants;

namespace Application.DTOs.Role;

public class UpdateRoleDto
{
    /// <summary>
    ///     主键ID
    /// </summary>
    public Guid Id { get; set; }

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