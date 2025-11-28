using Domain.Entities;

namespace Domain.Interfaces;

public interface IRoleRepository : IRepository<Role>
{
    /// <summary>
    ///     根据多个 ID 获取角色列表
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    Task<IEnumerable<Role>> GetByIdsAsync(IEnumerable<Guid> ids);

    /// <summary>
    ///     根据用户ID获取角色ID列表
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<IEnumerable<Guid>> GetRoleIdsByUserIdAsync(Guid userId);

    /// <summary>
    ///     根据用户ID获取角色列表
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<IEnumerable<Role>> GetRolesByUserIdAsync(Guid userId);

    /// <summary>
    ///     删除角色的指定菜单
    /// </summary>
    /// <param name="roleId"></param>
    /// <param name="menuIds"></param>
    /// <returns></returns>
    Task DeleteByRoleIdAndMenuIdsAsync(Guid roleId, IEnumerable<Guid> menuIds);

    /// <summary>
    ///     添加角色的指定菜单
    /// </summary>
    /// <param name="roleId"></param>
    /// <param name="menuIds"></param>
    /// <returns></returns>
    Task AddByRoleIdAndMenuIdsAsync(Guid roleId, IEnumerable<Guid> menuIds);
}