using Application.DTOs.Menu;
using Application.QueryParameters;
using Common.Constants;

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
    Task<PagedResult<MenuDto>> GetPagedMenusAsync(MenuQueryParameters parameters);

    /// <summary>
    ///     创建菜单
    /// </summary>
    Task<MenuDto> CreateMenuAsync(CreateMenuDto request);

    /// <summary>
    ///     根据 ID 获取菜单
    /// </summary>
    Task<MenuDto> GetMenuByIdAsync(Guid id);

    /// <summary>
    ///     获取所有菜单
    /// </summary>
    Task<List<MenuDto>> GetAllMenusAsync();
    
    /// <summary>
    ///   获取所有菜单树形结构
    /// </summary>
    /// <returns></returns>
    Task<List<MenuTreeDto>> GetAllMenusTreeAsync();
    
    /// <summary>
    ///     获取菜单树形结构
    /// </summary>
    Task<IEnumerable<MenuDto>> GetMenuTreeAsync();

    /// <summary>
    ///     根据角色 ID 获取菜单列表
    /// </summary>
    Task<IEnumerable<MenuDto>> GetMenusByRoleIdAsync(Guid roleId);

    /// <summary>
    ///     根据角色 ID 获取菜单树形结构
    /// </summary>
    Task<IEnumerable<MenuDto>> GetMenuTreeByRoleIdAsync(Guid roleId);

    /// <summary>
    ///     更新菜单
    /// </summary>
    Task UpdateMenuAsync(Guid id, UpdateMenuDto request);

    /// <summary>
    ///     删除菜单
    /// </summary>
    Task DeleteMenuAsync(Guid id);
}