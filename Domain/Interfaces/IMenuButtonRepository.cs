using Domain.Entities;

namespace Domain.Interfaces;

public interface IMenuButtonRepository : IRepository<MenuButton>
{
    /// <summary>
    ///     根据菜单Id获取菜单按钮
    /// </summary>
    /// <param name="menuId"></param>
    /// <returns></returns>
    Task<IEnumerable<MenuButton>> GetByMenuIdAsync(Guid menuId);
}