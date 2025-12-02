using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class MenuRepository(ApplicationDbContext context) : Repository<Menu>(context), IMenuRepository
{
    private readonly DbSet<RoleMenu> _dbSetRoleMenu = context.Set<RoleMenu>();
    
    /// <summary>
    ///  批量删除实体
    /// </summary>
    /// <param name="guids"></param>
    /// <returns></returns>
    public override Task DeleteRangeAsync(IEnumerable<Guid> guids)
    {
        var guidList = guids.ToList();
        return guidList.Count == 0 ? throw new Exception("There are no menus in the database.") : DeleteRangeAsync(DbSet.Where(m => guidList.Contains(m.Id)));
    }

    /// <summary>
    ///     批量删除菜单
    /// </summary>
    /// <param name="entities"></param>
    /// <returns></returns>
    public override async Task DeleteRangeAsync(IEnumerable<Menu> entities)
    {
        var enumerable = entities.ToList();
        if (enumerable.Count == 0) throw new Exception("There are no menus in the database.");
        var menuIdSet = new HashSet<Guid>(enumerable.Select(m => m.Id));
        // 获取所有要删除的菜单及其完整树结构
        var menuToDelete = await DbSet
            .Where(x => menuIdSet.Contains(x.Id))
            .ToListAsync();
        if (menuToDelete.Count == 0) throw new Exception("There are no menus in the database.");
        // 2️⃣ 获取待删除菜单的所有后代
        var allDescendants = await GetAllDescendantsAsync(menuIdSet);
        menuIdSet.UnionWith(allDescendants);
        // 3️⃣ 按树形结构排序：先删子菜单，再删父菜单
        var sortedMenus = await SortMenusByHierarchyAsync(menuIdSet, true);
        await base.DeleteRangeAsync(sortedMenus);
    }

    /// <summary>
    ///     删除菜单
    /// </summary>
    /// <param name="id"></param>
    /// <exception cref="Exception"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public override async Task DeleteAsync(Guid id)
    {
        var menu = await DbSet
            .Include(m => m.Children)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (menu is null) throw new Exception("菜单不存在");
        if (menu.Children.Count != 0)
            throw new InvalidOperationException(
                "该菜单下存在子菜单，无法删除！请先删除子菜单。");
        await base.DeleteAsync(id);
    }

    /// <summary>
    ///     删除菜单
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public override async Task DeleteAsync(Menu entity)
    {
        var menu = await DbSet
            .Include(x => x.Children)
            .FirstOrDefaultAsync(x => x.Id == entity.Id);
        if (menu is null) throw new Exception("菜单不存在");
        if (menu.Children.Count != 0)
            throw new InvalidOperationException(
                "该菜单下存在子菜单，无法删除！请先删除子菜单。");
        await base.DeleteAsync(entity);
    }

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

    /// <summary>
    ///     递归获取所有后代菜单ID
    /// </summary>
    private async Task<HashSet<Guid>> GetAllDescendantsAsync(HashSet<Guid> parentIds)
    {
        var descendants = new HashSet<Guid>();
        var queue = new Queue<Guid>(parentIds);

        while (queue.Count > 0)
        {
            var parentId = queue.Dequeue();

            var children = await DbSet
                .Where(m => m.ParentId == parentId)
                .Select(m => m.Id)
                .ToListAsync();

            foreach (var childId in children.Where(childId => descendants.Add(childId))) queue.Enqueue(childId);
        }

        return descendants;
    }

    /// <summary>
    ///     按层级排序菜单（确保删除顺序正确）
    /// </summary>
    private async Task<List<Menu>> SortMenusByHierarchyAsync(HashSet<Guid> menuIds, bool descending = true)
    {
        var menus = await DbSet
            .Where(m => menuIds.Contains(m.Id))
            .ToListAsync();

        // 计算每个菜单的深度
        var menuDict = menus.ToDictionary(m => m.Id);
        var depths = new Dictionary<Guid, int>();

        foreach (var menu in menus) depths[menu.Id] = CalculateDepth(menu.Id, menuDict);

        // 按深度排序：深度大的先删（叶子先删）
        return descending
            ? menus.OrderByDescending(m => depths[m.Id]).ToList()
            : menus.OrderBy(m => depths[m.Id]).ToList();
    }

    /// <summary>
    ///     计算菜单深度（到顶级菜单的距离）
    /// </summary>
    private int CalculateDepth(Guid menuId, Dictionary<Guid, Menu> menuDict)
    {
        if (!menuDict.TryGetValue(menuId, out var menu) || menu.ParentId == null)
            return 0;

        return 1 + CalculateDepth(menu.ParentId.Value, menuDict);
    }
}