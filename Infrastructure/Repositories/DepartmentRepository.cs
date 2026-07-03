using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <inheritdoc />
public class DepartmentRepository(ApplicationDbContext context)
    : Repository<Department>(context), IDepartmentRepository
{
    private readonly DbSet<User> _dbSetUser = context.Set<User>();

    /// <summary>
    /// 获取所有部门（用于构建树）
    /// </summary>
    public async Task<List<Department>> GetAllDepartmentsAsync()
    {
        return await DbSet
            .AsNoTracking()
            .Include(x => x.Leader)
            .OrderBy(x => x.Sort)
            .ThenBy(x => x.Id)
            .ToListAsync();
    }

    /// <summary>
    /// 获取所有子孙部门（递归）
    /// </summary>
    public async Task<List<Department>> GetDescendantsAsync(Guid departmentId)
    {
        var allDepartments = await GetAllDepartmentsAsync();
        var result = new List<Department>();
        GetDescendantsRecursive(departmentId, result, allDepartments);
        return result;
    }
    /// <summary>
    /// 递归获取子孙部门
    /// </summary>
    private void GetDescendantsRecursive(Guid parentId, List<Department> allDepartments, List<Department> result)
    {
        var children = allDepartments.Where(x => x.ParentId == parentId).ToList();
        foreach (var child in children)
        {
            result.Add(child);
            GetDescendantsRecursive(child.Id, allDepartments, result);
        }
    }

    /// <summary>
    /// 获取直接子部门
    /// </summary>
    public async Task<List<Department>> GetChildrenAsync(Guid parentId)
    {
        return await DbSet
            .AsNoTracking()
            .Include(x => x.Leader)
            .Where(x => x.ParentId == parentId)
            .OrderBy(x => x.Sort)
            .ThenBy(x => x.Id)
            .ToListAsync();
    }

    /// <summary>
    /// 根据部门代码获取部门
    /// </summary>
    public async Task<Department?> GetByCodeAsync(string code)
    {
        return await DbSet
            .Include(x => x.Leader)
            .FirstOrDefaultAsync(x => x.Code == code);
    }

    /// <summary>
    /// 检查部门代码是否存在
    /// </summary>
    public async Task<bool> ExistsByCodeAsync(string code, Guid? excludeId = null)
    {
        var query = DbSet
            .Where(x => x.Code == code);
        if (excludeId.HasValue)
        {
            query = query.Where(x => x.Id != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    /// <summary>
    /// 检查部门名称是否存在（同一父级下）
    /// </summary>
    public async Task<bool> ExistsByNameAsync(string name, Guid? parentId, Guid? excludeId = null)
    {
        var query = DbSet
            .Where(x => x.Name == name && x.ParentId == parentId);
        if (excludeId.HasValue)
        {
            query = query.Where(x => x.Id != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    /// <summary>
    /// 获取部门及其所有父级部门
    /// </summary>
    public async Task<List<Department>> GetAncestorsAsync(Guid departmentId)
    {
        var result = new List<Department>();
        var allDepartments = await GetAllDepartmentsAsync();
        var current = allDepartments.FirstOrDefault(x => x.Id == departmentId);
        while (current is { ParentId: not null })
        {
            var parent = allDepartments.FirstOrDefault(x => x.Id == current.ParentId.Value);
            if (parent != null)
            {
                result.Insert(0, parent);
                current = parent;
            }
            else
            {
                break;
            }
        }
        return result;
    }
    /// <summary>
    /// 检查部门是否有子部门
    /// </summary>
    public async Task<bool> HasChildrenAsync(Guid departmentId)
    {
        return await DbSet
            .Where(x => x.ParentId == departmentId)
            .AnyAsync();
    }

    /// <summary>
    /// 获取部门下的所有用户数量
    /// </summary>
    public async Task<int> GetUserCountAsync(Guid departmentId, bool includeDescendants = false)
    {
        if (!includeDescendants)
        {
            return await _dbSetUser
                .CountAsync(x => x.DepartmentId == departmentId);
        }
        // 包含所有子孙部门的用户
        var descendants = await GetDescendantsAsync(departmentId);
        var departmentIds = descendants.Select(x => x.Id).ToList();
        departmentIds.Add(departmentId);
        return await _dbSetUser
            .CountAsync(x => x.DepartmentId != null && departmentIds.Contains(x.DepartmentId.Value));
    }
    /// <summary>
    /// 批量获取部门
    /// </summary>
    public async Task<List<Department>> GetByIdsAsync(params Guid[] ids)
    {
        return await DbSet
            .AsNoTracking()
            .Include(x => x.Leader)
            .Where(x => ids.Contains(x.Id))
            .ToListAsync();
    }
}
