using Domain.Entities;

using Domain.ReadModels.BaseData;

namespace Domain.Interfaces;

/// <summary>
/// 定义角色及用户角色关系的持久化操作。
/// </summary>
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

    /// <summary>
    ///     按名称和编码顺序读取有限数量的角色轻量选择项。
    /// </summary>
    /// <param name="take">数据库读取上限；调用方可多取一条检测越界。</param>
    Task<List<SelectionOption>> GetBoundedSelectionOptionsAsync(int take);
}
