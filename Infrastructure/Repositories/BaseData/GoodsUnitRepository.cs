using Domain.Entities;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace Infrastructure.Repositories;

/// <summary>
///     商品单位仓储。
/// </summary>
public class GoodsUnitRepository(ApplicationDbContext context)
    : Repository<GoodsUnit>(context), IGoodsUnitRepository
{
    private readonly ApplicationDbContext _context = context;

    public async Task<List<GoodsUnit>> GetByGoodsIdAsync(Guid goodsId)
    {
        return await DbSet
            .AsNoTracking()
            .Where(x => x.GoodsId == goodsId)
            .OrderBy(x => x.Sort)
            .ThenBy(x => x.Id)
            .ToListAsync();
    }

    public async Task<bool> ExistsByGoodsAndNameAsync(Guid goodsId, string name, Guid? excludeId = null)
    {
        var normalizedName = name.Trim();
        var query = DbSet.Where(x => x.GoodsId == goodsId && x.Name == normalizedName);
        if (excludeId.HasValue)
        {
            query = query.Where(x => x.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task SetBaseUnitAsync(Guid goodsId, Guid unitId)
    {
        var units = await DbSet.Where(x => x.GoodsId == goodsId).ToListAsync();
        foreach (var unit in units)
        {
            unit.IsBaseUnit = unit.Id == unitId;
        }

        var goods = await _context.Set<GoodsEntity>().FirstOrDefaultAsync(x => x.Id == goodsId);
        if (goods is not null)
        {
            goods.BaseUnitId = unitId;
        }
    }
}

