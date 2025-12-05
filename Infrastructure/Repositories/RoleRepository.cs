using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class RoleRepository(ApplicationDbContext context) : Repository<Role>(context), IRoleRepository
{
    private readonly ApplicationDbContext _context = context;
    private readonly DbSet<RoleMenu> _dbSetRoleMenu = context.Set<RoleMenu>();
    private readonly DbSet<UserRole> _dbSetUserRole = context.Set<UserRole>();

    public async Task<IEnumerable<Role>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        return await DbSet.Where(r => ids.Contains(r.Id)).ToListAsync();
    }

    public async Task<IEnumerable<Guid>> GetRoleIdsByUserIdAsync(Guid userId)
    {
        return await _dbSetUserRole
            .Where(ur => ur.UserId == userId && ur.Role != null)
            .Select(ur => ur.RoleId)
            .ToListAsync();
    }
    /// <summary>
    /// 根据用户id获取角色列表
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public async Task<IEnumerable<Role>> GetRolesByUserIdAsync(Guid userId)
    {
        return await _dbSetUserRole
            .Where(ur => ur.UserId == userId && ur.Role != null)
            .Select(ur => ur.Role!)
            .ToListAsync();
    }


    public async Task DeleteByRoleIdAndMenuIdsAsync(Guid roleId, IEnumerable<Guid> menuIds)
    {
        var roleMenus = await _dbSetRoleMenu
            .Where(r => r.RoleId == roleId && menuIds.Contains(r.MenuId))
            .ToListAsync();
        _dbSetRoleMenu.RemoveRange(roleMenus);
        await _context.SaveChangesAsync();
    }

    public async Task AddByRoleIdAndMenuIdsAsync(Guid roleId, IEnumerable<Guid> menuIds)
    {
        var roleMenus = menuIds.Select(menuId => new RoleMenu
        {
            RoleId = roleId,
            MenuId = menuId
        });
        await _dbSetRoleMenu.AddRangeAsync(roleMenus);
        await _context.SaveChangesAsync();
    }
}