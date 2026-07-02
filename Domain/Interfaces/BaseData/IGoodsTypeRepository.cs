using Domain.Entities;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace Domain.Interfaces;

/// <summary>
///     商品分类仓储接口。
/// </summary>
public interface IGoodsTypeRepository : ITreeBaseDataRepository<GoodsType>
{
}

