using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class UserRepository(ApplicationDbContext context) : Repository<User>(context), IUserRepository
{
    private readonly ApplicationDbContext _context = context;
    private readonly DbSet<UserRole> _dbSetUserRole = context.Set<UserRole>();

    public Task<User?> FindByUsernameAsync(string username)
    {
        return DbSet.Where(u => u.Username == username).FirstOrDefaultAsync();
    }

    public async Task DeleteByUserIdAndRoleIdsAsync(Guid userId, IEnumerable<Guid> roleIds)
    {
        var userRoles = await _dbSetUserRole
            .Where(ur => ur.UserId == userId && roleIds.Contains(ur.RoleId))
            .ToListAsync();
        _dbSetUserRole.RemoveRange(userRoles);
        await _context.SaveChangesAsync();
    }

    public async Task AddByUserIdAndRoleIdsAsync(Guid userId, IEnumerable<Guid> roleIds)
    {
        var userRoles = roleIds.Select(roleId => new UserRole
        {
            UserId = userId,
            RoleId = roleId
        });
        await _dbSetUserRole.AddRangeAsync(userRoles);
        await _context.SaveChangesAsync();
    }
}