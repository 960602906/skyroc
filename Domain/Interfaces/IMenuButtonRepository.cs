using Domain.Entities;

namespace Domain.Interfaces;

/// <summary>
/// 定义菜单按钮权限的持久化操作。
/// </summary>
public interface IMenuButtonRepository : IRepository<MenuButton>
{
    /// <summary>
    ///     根据菜单Id获取菜单按钮
    /// </summary>
    /// <param name="menuId"></param>
    /// <returns></returns>
    Task<IEnumerable<MenuButton>> GetByMenuIdAsync(Guid menuId);
}
