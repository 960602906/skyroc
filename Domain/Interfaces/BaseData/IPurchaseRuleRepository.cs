using Domain.Entities;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace Domain.Interfaces;

/// <summary>
///     采购规则仓储接口。
/// </summary>
public interface IPurchaseRuleRepository : INamedCodeRepository<PurchaseRule>
{
    /// <summary>
    ///     替换采购规则适用商品关系。
    /// </summary>
    Task ReplaceGoodsRelationsAsync(Guid purchaseRuleId, IEnumerable<Guid>? goodsIds);

    /// <summary>
    ///     替换采购规则适用客户关系。
    /// </summary>
    Task ReplaceCustomerRelationsAsync(Guid purchaseRuleId, IEnumerable<Guid>? customerIds);
}

