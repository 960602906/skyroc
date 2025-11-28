using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class MenuRepository(ApplicationDbContext context) : Repository<Menu>(context), IMenuRepository
{
    private readonly DbSet<RoleMenu> _dbSetRoleMenu = context.Set<RoleMenu>();

    public async Task<IEnumerable<Guid>> GetMenuIdsByRoleIdAsync(Guid roleId)
    {
        return await _dbSetRoleMenu
            .Where(x => x.RoleId == roleId && x.Menu != null)
            .Select(r => r.MenuId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Menu>> GetMenusByRoleIdAsync(Guid roleId)
    {
        return await _dbSetRoleMenu
            .Where(r => r.RoleId == roleId && r.Menu != null)
            .Select(r => r.Menu!)
            .ToListAsync();
    }

    public async Task<IEnumerable<Menu>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        return await DbSet
            .Where(x => ids.Contains(x.Id))
            .ToListAsync();
    }

    
}