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
///     带名称、编码和状态的基础资料分页查询参数。
/// </summary>
public abstract class NamedCodeQueryParameters : PagedQueryParameters
{
    /// <summary>
    ///     名称，支持模糊查询。
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///     编码，支持模糊查询。
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    ///     启用禁用状态。
    /// </summary>
    public Status? Status { get; set; }
}

