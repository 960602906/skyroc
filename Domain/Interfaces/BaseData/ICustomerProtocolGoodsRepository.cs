using Domain.Entities;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace Domain.Interfaces;

/// <summary>
///     客户协议价商品仓储接口。
/// </summary>
public interface ICustomerProtocolGoodsRepository : IRepository<CustomerProtocolGoods>
{
    /// <summary>
    ///     判断协议价商品明细是否重复。
    /// </summary>
    Task<bool> ExistsDetailAsync(Guid customerProtocolId, Guid goodsId, Guid goodsUnitId, Guid? excludeId = null);

    /// <summary>
    ///     按多个 ID 获取协议价商品明细。
    /// </summary>
    Task<List<CustomerProtocolGoods>> GetByIdsAsync(IEnumerable<Guid> ids);
}

