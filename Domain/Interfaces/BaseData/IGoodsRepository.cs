using Domain.Entities;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace Domain.Interfaces;

/// <summary>
///     商品档案仓储接口。
/// </summary>
public interface IGoodsRepository : INamedCodeRepository<GoodsEntity>
{
    /// <summary>
    ///     替换商品可供货供应商关系。
    /// </summary>
    Task ReplaceSupplierRelationsAsync(Guid goodsId, IEnumerable<Guid>? supplierIds, Guid? defaultSupplierId);
}

