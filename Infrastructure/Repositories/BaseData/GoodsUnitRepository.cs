using System.Linq.Expressions;
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

    /// <summary>
    ///     按主键查询商品单位，并加载所属商品，供详情回填商品名称/编码。
    /// </summary>
    public override async Task<GoodsUnit?> GetByIdAsync(Guid id)
    {
        return await DbSet
            .Include(x => x.Goods)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <summary>
    ///     分页查询商品单位，并加载所属商品，供列表展示商品名称/编码。
    /// </summary>
    public override async Task<(IEnumerable<GoodsUnit> Data, int Total)> GetPagedAsync(
        Expression<Func<GoodsUnit, bool>>? predicate,
        int pageNumber,
        int pageSize,
        Expression<Func<GoodsUnit, object>>? orderBy = null,
        bool isDescending = false)
    {
        return await PagedFromQueryAsync(
            DbSet.AsNoTracking().Include(x => x.Goods),
            predicate,
            pageNumber,
            pageSize,
            orderBy,
            isDescending);
    }

    /// <inheritdoc />
    public async Task<List<GoodsUnit>> GetByGoodsIdAsync(Guid goodsId)
    {
        return await DbSet
            .AsNoTracking()
            .Include(x => x.Goods)
            .Where(x => x.GoodsId == goodsId)
            .OrderBy(x => x.Sort)
            .ThenBy(x => x.Id)
            .ToListAsync();
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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
