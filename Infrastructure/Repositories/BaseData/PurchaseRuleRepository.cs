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
///     采购规则仓储。
/// </summary>
public class PurchaseRuleRepository(ApplicationDbContext context)
    : NamedCodeRepository<PurchaseRule>(context), IPurchaseRuleRepository
{
    private readonly ApplicationDbContext _context = context;

    public override async Task<PurchaseRule?> GetByIdAsync(Guid id)
    {
        return await DbSet
            .Include(x => x.Supplier)
            .Include(x => x.Purchaser)
            .Include(x => x.Ware)
            .Include(x => x.GoodsType)
            .Include(x => x.Goods)
            .Include(x => x.Customers)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task ReplaceGoodsRelationsAsync(Guid purchaseRuleId, IEnumerable<Guid>? goodsIds)
    {
        var relations = await _context.Set<PurchaseRuleGoods>()
            .Where(x => x.PurchaseRuleId == purchaseRuleId)
            .ToListAsync();
        _context.Set<PurchaseRuleGoods>().RemoveRange(relations);

        var newRelations = goodsIds?
            .Where(x => x != Guid.Empty)
            .Distinct()
            .Select(goodsId => new PurchaseRuleGoods
            {
                PurchaseRuleId = purchaseRuleId,
                GoodsId = goodsId
            }) ?? [];

        await _context.Set<PurchaseRuleGoods>().AddRangeAsync(newRelations);
    }

    public async Task ReplaceCustomerRelationsAsync(Guid purchaseRuleId, IEnumerable<Guid>? customerIds)
    {
        var relations = await _context.Set<PurchaseRuleCustomer>()
            .Where(x => x.PurchaseRuleId == purchaseRuleId)
            .ToListAsync();
        _context.Set<PurchaseRuleCustomer>().RemoveRange(relations);

        var newRelations = customerIds?
            .Where(x => x != Guid.Empty)
            .Distinct()
            .Select(customerId => new PurchaseRuleCustomer
            {
                PurchaseRuleId = purchaseRuleId,
                CustomerId = customerId
            }) ?? [];

        await _context.Set<PurchaseRuleCustomer>().AddRangeAsync(newRelations);
    }
}

