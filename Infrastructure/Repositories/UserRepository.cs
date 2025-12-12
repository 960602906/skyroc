using System.Linq.Expressions;
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
        return await DbSet.AsNoTracking().Where(r => ids.Contains(r.Id)).ToListAsync();
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
    
    /// <summary>
    /// 根据ID获取实体
    /// </summary>
    /// <param name="id"></param>
    /// <param name="selector"></param>
    /// <typeparam name="TEntity"></typeparam>
    /// <returns></returns>
    public async Task<TEntity?> GetByIdAsync<TEntity>(Guid id, Expression<Func<User, TEntity>> selector)
    {
        return  await DbSet.AsNoTracking().Where(r => r.Id == id).Select(selector).FirstOrDefaultAsync();
    }
    
    /// <summary>
    /// 查询所有数据并投影
    /// </summary>
    /// <param name="selector"></param>
    /// <typeparam name="TEntity"></typeparam>
    /// <returns></returns>
    public async Task<IEnumerable<TEntity>> GetAllAsync<TEntity>(Expression<Func<User, TEntity>> selector)
    {
        return await DbSet.AsNoTracking().Select(selector).ToListAsync();
    }
    
    /// <summary>
    /// 分页查询并且投影
    /// </summary>
    /// <param name="predicate"></param>
    /// <param name="pageNumber"></param>
    /// <param name="pageSize"></param>
    /// <param name="orderBy"></param>
    /// <param name="isDescending"></param>
    /// <param name="selector"></param>
    /// <typeparam name="TEntity"></typeparam>
    /// <returns></returns>
    public async Task<(IEnumerable<TEntity> Data, int Total)> GetPagedAsync<TEntity>(Expression<Func<User, bool>>? predicate, int pageNumber, int pageSize, Expression<Func<User, object>>? orderBy = null,
        bool isDescending = false, Expression<Func<User, TEntity>>? selector = null)
    {
        var query = DbSet.AsNoTracking();
        // 不需要手动过滤 IsDeleted，全局过滤器已处理 ✅
        if (predicate != null)
            query = query.Where(predicate);
        var total = await query.CountAsync();
        if (orderBy != null) query = isDescending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);
        var data = await query
            .AsNoTracking()
            .Select(selector ?? (_ => default!))
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (data, total);
    }
}