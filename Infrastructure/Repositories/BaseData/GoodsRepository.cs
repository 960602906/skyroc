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
///     商品档案仓储。
/// </summary>
public class GoodsRepository(ApplicationDbContext context)
    : NamedCodeRepository<GoodsEntity>(context), IGoodsRepository
{
    private readonly ApplicationDbContext _context = context;

    public override async Task<GoodsEntity?> GetByIdAsync(Guid id)
    {
        return await DbSet
            .Include(x => x.GoodsType)
            .Include(x => x.BaseUnit)
            .Include(x => x.DefaultSupplier)
            .Include(x => x.DefaultWare)
            .Include(x => x.Units)
            .Include(x => x.Images)
            .Include(x => x.SupplierRelations)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task ReplaceSupplierRelationsAsync(Guid goodsId, IEnumerable<Guid>? supplierIds, Guid? defaultSupplierId)
    {
        var relations = await _context.Set<GoodsSupplierRelation>()
            .Where(x => x.GoodsId == goodsId)
            .ToListAsync();
        _context.Set<GoodsSupplierRelation>().RemoveRange(relations);

        var supplierIdList = supplierIds?
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList() ?? [];

        if (defaultSupplierId.HasValue && defaultSupplierId.Value != Guid.Empty && !supplierIdList.Contains(defaultSupplierId.Value))
        {
            supplierIdList.Add(defaultSupplierId.Value);
        }

        var newRelations = supplierIdList.Select(supplierId => new GoodsSupplierRelation
        {
            GoodsId = goodsId,
            SupplierId = supplierId,
            IsDefault = defaultSupplierId.HasValue && supplierId == defaultSupplierId.Value
        });
        await _context.Set<GoodsSupplierRelation>().AddRangeAsync(newRelations);
    }
}

