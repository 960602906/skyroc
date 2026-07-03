using Domain.Entities;

namespace Domain.Interfaces;

/// <summary>
/// 定义菜单树及角色菜单关系的持久化操作。
/// </summary>
public interface IMenuRepository : IRepository<Menu>
{
    /// <summary>
    ///     根据角色ID获取菜单ID列表
    /// </summary>
    /// <param name="roleId"></param>
    /// <returns></returns>
    Task<IEnumerable<Guid>> GetMenuIdsByRoleIdAsync(Guid roleId);

    /// <summary>
    ///     根据角色ID获取菜单列表
    /// </summary>
    /// <param name="roleId"></param>
    /// <returns></returns>
    Task<IEnumerable<Menu>> GetMenusByRoleIdAsync(Guid roleId);

    /// <summary>
    ///     根据多个角色 ID 获取去重后的菜单列表及按钮权限。
    /// </summary>
    Task<IEnumerable<Menu>> GetMenusByRoleIdsAsync(IEnumerable<Guid> roleIds);

    /// <summary>
    ///     根据多个 ID 获取菜单列表
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    Task<IEnumerable<Menu?>> GetByIdsAsync(IEnumerable<Guid> ids);
}
