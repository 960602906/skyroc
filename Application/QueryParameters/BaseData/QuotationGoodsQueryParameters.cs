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
///     报价商品查询参数。
/// </summary>
public class QuotationGoodsQueryParameters : PagedQueryParameters
{
    /// <summary>
    ///     报价单 ID。
    /// </summary>
    public Guid? QuotationId { get; set; }

    /// <summary>
    ///     商品 ID。
    /// </summary>
    public Guid? GoodsId { get; set; }

    /// <summary>
    ///     是否在报价单内上架。
    /// </summary>
    public bool? IsOnSale { get; set; }

    /// <summary>
    ///     启用禁用状态。
    /// </summary>
    public Status? Status { get; set; }

    /// <summary>
    ///     构建查询表达式。
    /// </summary>
    public Expression<Func<QuotationGoods, bool>> QueryBuild()
    {
        return x =>
            (!QuotationId.HasValue || x.QuotationId == QuotationId.Value) &&
            (!GoodsId.HasValue || x.GoodsId == GoodsId.Value) &&
            (!IsOnSale.HasValue || x.IsOnSale == IsOnSale.Value) &&
            (!Status.HasValue || x.Status == Status.Value);
    }
}

