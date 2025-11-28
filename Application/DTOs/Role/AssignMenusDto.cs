namespace Application.DTOs.Role;

/// <summary>
///     角色分配菜单DTO
/// </summary>
public class AssignMenusDto
{
    public Guid RoleId { get; set; }
    public IEnumerable<Guid> MenuIds { get; set; } = [];
}