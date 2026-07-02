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
///     采购规则查询参数。
/// </summary>
public class PurchaseRuleQueryParameters : NamedCodeQueryParameters
{
    /// <summary>
    ///     供应商 ID。
    /// </summary>
    public Guid? SupplierId { get; set; }

    /// <summary>
    ///     采购员 ID。
    /// </summary>
    public Guid? PurchaserId { get; set; }

    /// <summary>
    ///     仓库 ID。
    /// </summary>
    public Guid? WareId { get; set; }

    /// <summary>
    ///     商品分类 ID。
    /// </summary>
    public Guid? GoodsTypeId { get; set; }

    /// <summary>
    ///     采购模式。
    /// </summary>
    public int? PurchasePattern { get; set; }

    /// <summary>
    ///     构建查询表达式。
    /// </summary>
    public Expression<Func<PurchaseRule, bool>> QueryBuild()
    {
        return x =>
            (string.IsNullOrWhiteSpace(Name) || x.Name.Contains(Name.Trim())) &&
            (string.IsNullOrWhiteSpace(Code) || x.Code.Contains(Code.Trim())) &&
            (!SupplierId.HasValue || x.SupplierId == SupplierId.Value) &&
            (!PurchaserId.HasValue || x.PurchaserId == PurchaserId.Value) &&
            (!WareId.HasValue || x.WareId == WareId.Value) &&
            (!GoodsTypeId.HasValue || x.GoodsTypeId == GoodsTypeId.Value) &&
            (!PurchasePattern.HasValue || x.PurchasePattern == PurchasePattern.Value) &&
            (!Status.HasValue || x.Status == Status.Value);
    }
}

