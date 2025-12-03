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