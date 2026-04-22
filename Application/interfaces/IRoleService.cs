using Application.DTOs.Role;
using Application.QueryParameters;
using Shared.Common;
using Shared.Constants;

namespace Application.interfaces;

/// <summary>
///     角色应用服务接口
/// </summary>
public interface IRoleService
{
    /// <summary>
    ///     分页查询菜单
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns></returns>
    Task<PagedResult<RoleDto>> GetPagedMenusAsync(RoleQueryParameters parameters);

    /// <summary>
    ///     创建角色
    /// </summary>
    Task<RoleDto> CreateRoleAsync(CreateRoleDto request);

    /// <summary>
    ///     根据 ID 获取角色
    /// </summary>
    Task<RoleDto> GetRoleByIdAsync(Guid id);

    /// <summary>
    ///     获取所有角色
    /// </summary>
    Task<IEnumerable<RoleDto>> GetAllRolesAsync();

    /// <summary>
    ///     更新角色
    /// </summary>
    Task UpdateRoleAsync(Guid id, UpdateRoleDto request);

    /// <summary>
    ///     删除角色
    /// </summary>
    Task DeleteRoleAsync(Guid id);

    /// <summary>
    ///     批量删除角色
    /// </summary>
    /// <returns></returns>
    Task DeleteAllRolesAsync(List<Guid> roleIds);

    /// <summary>
    ///     给角色分配菜单权限
    /// </summary>
    Task AssignMenusToRoleAsync(Guid roleId, IEnumerable<Guid> menuIds);

    /// <summary>
    ///     移除角色的菜单权限
    /// </summary>
    Task RemoveMenusFromRoleAsync(Guid roleId, IEnumerable<Guid> menuIds);
}
