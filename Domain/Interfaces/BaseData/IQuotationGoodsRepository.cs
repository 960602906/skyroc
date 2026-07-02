using Domain.Entities;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace Domain.Interfaces;

/// <summary>
///     报价商品仓储接口。
/// </summary>
public interface IQuotationGoodsRepository : IRepository<QuotationGoods>
{
    /// <summary>
    ///     判断报价商品明细是否重复。
    /// </summary>
    Task<bool> ExistsDetailAsync(Guid quotationId, Guid goodsId, Guid goodsUnitId, Guid? excludeId = null);

    /// <summary>
    ///     按多个 ID 获取报价商品明细。
    /// </summary>
    Task<List<QuotationGoods>> GetByIdsAsync(IEnumerable<Guid> ids);
}

