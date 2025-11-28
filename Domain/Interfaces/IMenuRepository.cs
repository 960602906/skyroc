using Domain.Entities;

namespace Domain.Interfaces;

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
    ///     根据多个 ID 获取菜单列表
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    Task<IEnumerable<Menu>> GetByIdsAsync(IEnumerable<Guid> ids);
    

}