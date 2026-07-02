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
///     商品单位查询参数。
/// </summary>
public class GoodsUnitQueryParameters : PagedQueryParameters
{
    /// <summary>
    ///     商品 ID。
    /// </summary>
    public Guid? GoodsId { get; set; }

    /// <summary>
    ///     单位名称。
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///     启用禁用状态。
    /// </summary>
    public Status? Status { get; set; }

    /// <summary>
    ///     构建查询表达式。
    /// </summary>
    public Expression<Func<GoodsUnit, bool>> QueryBuild()
    {
        return x =>
            (!GoodsId.HasValue || x.GoodsId == GoodsId.Value) &&
            (string.IsNullOrWhiteSpace(Name) || x.Name.Contains(Name.Trim())) &&
            (!Status.HasValue || x.Status == Status.Value);
    }
}

