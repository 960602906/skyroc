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
///     商品查询参数。
/// </summary>
public class GoodsQueryParameters : NamedCodeQueryParameters
{
    /// <summary>
    ///     商品分类 ID。
    /// </summary>
    public Guid? GoodsTypeId { get; set; }

    /// <summary>
    ///     默认供应商 ID。
    /// </summary>
    public Guid? DefaultSupplierId { get; set; }

    /// <summary>
    ///     默认仓库 ID。
    /// </summary>
    public Guid? DefaultWareId { get; set; }

    /// <summary>
    ///     是否上架。
    /// </summary>
    public bool? IsOnSale { get; set; }

    /// <summary>
    ///     构建查询表达式。
    /// </summary>
    public Expression<Func<GoodsEntity, bool>> QueryBuild()
    {
        return x =>
            (string.IsNullOrWhiteSpace(Name) || x.Name.Contains(Name.Trim())) &&
            (string.IsNullOrWhiteSpace(Code) || x.Code.Contains(Code.Trim())) &&
            (!GoodsTypeId.HasValue || x.GoodsTypeId == GoodsTypeId.Value) &&
            (!DefaultSupplierId.HasValue || x.DefaultSupplierId == DefaultSupplierId.Value) &&
            (!DefaultWareId.HasValue || x.DefaultWareId == DefaultWareId.Value) &&
            (!IsOnSale.HasValue || x.IsOnSale == IsOnSale.Value) &&
            (!Status.HasValue || x.Status == Status.Value);
    }
}

