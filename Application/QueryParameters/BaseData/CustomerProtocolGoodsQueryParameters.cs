using System.Linq.Expressions;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using Shared.Constants;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace Application.QueryParameters;

/// <summary>
///     客户协议价商品查询参数。
/// </summary>
public class CustomerProtocolGoodsQueryParameters : PagedQueryParameters
{
    /// <summary>
    ///     客户协议价 ID。
    /// </summary>
    public Guid? CustomerProtocolId { get; set; }

    /// <summary>
    ///     商品 ID。
    /// </summary>
    public Guid? GoodsId { get; set; }

    /// <summary>
    ///     启用禁用状态。
    /// </summary>
    public Status? Status { get; set; }

    /// <summary>
    ///     构建查询表达式。
    /// </summary>
    public Expression<Func<CustomerProtocolGoods, bool>> QueryBuild()
    {
        return x =>
            (!CustomerProtocolId.HasValue || x.CustomerProtocolId == CustomerProtocolId.Value) &&
            (!GoodsId.HasValue || x.GoodsId == GoodsId.Value) &&
            (!Status.HasValue || x.Status == Status.Value);
    }
}

