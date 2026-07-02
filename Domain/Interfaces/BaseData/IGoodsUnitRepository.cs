using Domain.Entities;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace Domain.Interfaces;

/// <summary>
///     商品单位仓储接口。
/// </summary>
public interface IGoodsUnitRepository : IRepository<GoodsUnit>
{
    /// <summary>
    ///     查询商品单位。
    /// </summary>
    Task<List<GoodsUnit>> GetByGoodsIdAsync(Guid goodsId);

    /// <summary>
    ///     同一商品下单位名称是否存在。
    /// </summary>
    Task<bool> ExistsByGoodsAndNameAsync(Guid goodsId, string name, Guid? excludeId = null);

    /// <summary>
    ///     将指定单位设置为商品基础单位。
    /// </summary>
    Task SetBaseUnitAsync(Guid goodsId, Guid unitId);
}

