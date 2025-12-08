using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class UserRepository(ApplicationDbContext context) : Repository<User>(context), IUserRepository
{
    private readonly ApplicationDbContext _context = context;
    private readonly DbSet<UserRole> _dbSetUserRole = context.Set<UserRole>();

    /// <summary>
    ///     批量获取实体
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    public async Task<IEnumerable<User>> GetByIdAsync(IEnumerable<Guid> ids)
    {
        return await DbSet.Where(r => ids.Contains(r.Id)).ToListAsync();
    }

    /// <summary>
    ///     根据用户名查找用户
    /// </summary>
    /// <param name="username"></param>
    /// <returns></returns>
    public Task<User?> FindByUsernameAsync(string username)
    {
        return DbSet.Where(u => u.Username == username).FirstOrDefaultAsync();
    }

    /// <summary>
    ///     删除用户的指定角色
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="roleIds"></param>
    public async Task DeleteByUserIdAndRoleIdsAsync(Guid userId, IEnumerable<Guid> roleIds)
    {
        var userRoles = await _dbSetUserRole
            .Where(ur => ur.UserId == userId && roleIds.Contains(ur.RoleId))
            .ToListAsync();
        _dbSetUserRole.RemoveRange(userRoles);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    ///     添加用户的指定角色
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="roleIds"></param>
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

    /// <summary>
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<IEnumerable<User>> GetByDepartmentIdsAsync(List<Guid> ids)
    {
        return await DbSet.Where(r => ids.Contains(r.DepartmentId ?? Guid.Empty)).ToListAsync();
    }
}