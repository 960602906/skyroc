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
///     客户协议价商品仓储。
/// </summary>
public class CustomerProtocolGoodsRepository(ApplicationDbContext context)
    : Repository<CustomerProtocolGoods>(context), ICustomerProtocolGoodsRepository
{
    /// <inheritdoc />
    public override async Task<CustomerProtocolGoods?> GetByIdAsync(Guid id)
    {
        return await DbSet
            .Include(x => x.Goods)
            .Include(x => x.GoodsUnit)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsDetailAsync(Guid customerProtocolId, Guid goodsId, Guid goodsUnitId, Guid? excludeId = null)
    {
        var query = DbSet.Where(x =>
            x.CustomerProtocolId == customerProtocolId &&
            x.GoodsId == goodsId &&
            x.GoodsUnitId == goodsUnitId);
        if (excludeId.HasValue)
        {
            query = query.Where(x => x.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    /// <inheritdoc />
    public async Task<List<CustomerProtocolGoods>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        var idList = ids.Distinct().ToList();
        return idList.Count == 0
            ? []
            : await DbSet.Where(x => idList.Contains(x.Id)).ToListAsync();
    }
}
