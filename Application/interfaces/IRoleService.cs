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
    [Cacheable(KeyPrefix = "role:page", Seconds = 300, Buckets = ["role"])]
    Task<PagedResult<RoleDto>> GetPagedMenusAsync(RoleQueryParameters parameters);

    /// <summary>
    ///     创建角色
    /// </summary>
    [CacheEvict("role")]
    Task<RoleDto> CreateRoleAsync(CreateRoleDto request);

    /// <summary>
    ///     根据 ID 获取角色
    /// </summary>
    [Cacheable(KeyPrefix = "role:id", Seconds = 600, Buckets = ["role"])]
    Task<RoleDto> GetRoleByIdAsync(Guid id);

    /// <summary>
    ///     获取所有角色
    /// </summary>
    [Cacheable(KeyPrefix = "role:all", Seconds = 600, Buckets = ["role"])]
    Task<IEnumerable<RoleDto>> GetAllRolesAsync();

    /// <summary>
    ///     更新角色
    /// </summary>
    [CacheEvict("role")]
    Task UpdateRoleAsync(Guid id, UpdateRoleDto request);

    /// <summary>
    ///     删除角色
    /// </summary>
    [CacheEvict("role")]
    Task DeleteRoleAsync(Guid id);

    /// <summary>
    ///     批量删除角色
    /// </summary>
    /// <returns></returns>
    [CacheEvict("role")]
    Task DeleteAllRolesAsync(List<Guid> roleIds);

    /// <summary>
    ///     给角色分配菜单权限
    /// </summary>
    [CacheEvict("role", "menu")]
    Task AssignMenusToRoleAsync(Guid roleId, IEnumerable<Guid> menuIds);

    /// <summary>
    ///     移除角色的菜单权限
    /// </summary>
    [CacheEvict("role", "menu")]
    Task RemoveMenusFromRoleAsync(Guid roleId, IEnumerable<Guid> menuIds);
}
