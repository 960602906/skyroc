using Application.DTOs.Menu;
using Application.QueryParameters;
using Shared.Common;
using Shared.Constants;

namespace Application.interfaces;

/// <summary>
///     菜单应用服务接口
/// </summary>
public interface IMenuService
{
    /// <summary>
    ///     分页查询菜单
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns></returns>
    [Cacheable(KeyPrefix = "menu:page", Seconds = 300, Buckets = ["menu"])]
    Task<PagedResult<MenuDto>> GetPagedMenusAsync(MenuQueryParameters parameters);

    /// <summary>
    ///     创建菜单
    /// </summary>
    [CacheEvict("menu", "role")]
    Task<MenuDto> CreateMenuAsync(CreateMenuDto request);

    /// <summary>
    ///     根据 ID 获取菜单
    /// </summary>
    [Cacheable(KeyPrefix = "menu:id", Seconds = 600, Buckets = ["menu"])]
    Task<MenuDto> GetMenuByIdAsync(Guid id);

    /// <summary>
    ///     获取所有菜单
    /// </summary>
    [Cacheable(KeyPrefix = "menu:all", Seconds = 600, Buckets = ["menu"])]
    Task<List<MenuDto>> GetAllMenusAsync();

    /// <summary>
    ///     获取所有菜单树形结构
    /// </summary>
    /// <returns></returns>
    [Cacheable(KeyPrefix = "menu:tree:all", Seconds = 600, Buckets = ["menu"])]
    Task<List<MenuTreeDto>> GetAllMenusTreeAsync();

    /// <summary>
    ///     获取菜单树形结构
    /// </summary>
    [Cacheable(KeyPrefix = "menu:tree", Seconds = 600, Buckets = ["menu"])]
    Task<List<MenuTreeDto>> GetMenuTreeAsync();

    /// <summary>
    ///     根据角色 ID 获取菜单列表
    /// </summary>
    [Cacheable(KeyPrefix = "menu:byRole", Seconds = 600, Buckets = ["menu", "role"])]
    Task<List<MenuDto>> GetMenusByRoleIdAsync(Guid roleId);

    /// <summary>
    ///     根据角色 ID 获取菜单树形结构
    /// </summary>
    [Cacheable(KeyPrefix = "menu:tree:byRole", Seconds = 600, Buckets = ["menu", "role"])]
    Task<List<MenuDto>> GetMenuTreeByRoleIdAsync(Guid roleId);

    /// <summary>
    ///     更新菜单
    /// </summary>
    [CacheEvict("menu", "role")]
    Task UpdateMenuAsync(Guid id, UpdateMenuDto request);

    /// <summary>
    ///     删除菜单
    /// </summary>
    [CacheEvict("menu", "role")]
    Task DeleteMenuAsync(Guid id);

    /// <summary>
    ///     批量删除菜单
    /// </summary>
    /// <param name="menuIds"></param>
    /// <returns></returns>
    [CacheEvict("menu", "role")]
    Task DeleteMenusAsync(List<Guid> menuIds);
}
