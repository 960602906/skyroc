using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class MenuButtonRepository(ApplicationDbContext context) : Repository<MenuButton>(context), IMenuButtonRepository
{
    /// <summary>
    ///     根据菜单Id获取菜单按钮
    /// </summary>
    /// <param name="menuId"></param>
    /// <returns></returns>
    public async Task<IEnumerable<MenuButton>> GetByMenuIdAsync(Guid menuId)
    {
        return await DbSet.Where(x => x.MenuId == menuId).ToListAsync();
    }
}