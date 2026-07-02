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
///     报价单查询参数。
/// </summary>
public class QuotationQueryParameters : NamedCodeQueryParameters
{
    /// <summary>
    ///     是否已审核。
    /// </summary>
    public bool? IsAudited { get; set; }

    /// <summary>
    ///     构建查询表达式。
    /// </summary>
    public Expression<Func<Quotation, bool>> QueryBuild()
    {
        return x =>
            (string.IsNullOrWhiteSpace(Name) || x.Name.Contains(Name.Trim())) &&
            (string.IsNullOrWhiteSpace(Code) || x.Code.Contains(Code.Trim())) &&
            (!IsAudited.HasValue || x.IsAudited == IsAudited.Value) &&
            (!Status.HasValue || x.Status == Status.Value);
    }
}

