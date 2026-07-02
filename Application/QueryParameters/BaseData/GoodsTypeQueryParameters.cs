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
///     商品分类查询参数。
/// </summary>
public class GoodsTypeQueryParameters : NamedCodeQueryParameters
{
    /// <summary>
    ///     父级分类 ID。
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    ///     税收分类编码。
    /// </summary>
    public string? TaxCategoryCode { get; set; }

    /// <summary>
    ///     构建查询表达式。
    /// </summary>
    public Expression<Func<GoodsType, bool>> QueryBuild()
    {
        return x =>
            (string.IsNullOrWhiteSpace(Name) || x.Name.Contains(Name.Trim())) &&
            (string.IsNullOrWhiteSpace(Code) || x.Code.Contains(Code.Trim())) &&
            (string.IsNullOrWhiteSpace(TaxCategoryCode) || (x.TaxCategoryCode != null && x.TaxCategoryCode.Contains(TaxCategoryCode.Trim()))) &&
            (!ParentId.HasValue || x.ParentId == ParentId.Value) &&
            (!Status.HasValue || x.Status == Status.Value);
    }
}

