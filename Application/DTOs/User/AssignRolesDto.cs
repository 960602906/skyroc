namespace Application.DTOs.User;

/// <summary>
///     用户分配角色DTO
/// </summary>
public class AssignRolesDto
{
    public Guid UserId { get; set; }
    public IEnumerable<Guid> RoleIds { get; set; } = [];
}