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
///     供应商查询参数。
/// </summary>
public class SupplierQueryParameters : NamedCodeQueryParameters
{
    /// <summary>
    ///     构建查询表达式。
    /// </summary>
    public Expression<Func<Supplier, bool>> QueryBuild()
    {
        return x =>
            (string.IsNullOrWhiteSpace(Name) || x.Name.Contains(Name.Trim())) &&
            (string.IsNullOrWhiteSpace(Code) || x.Code.Contains(Code.Trim())) &&
            (!Status.HasValue || x.Status == Status.Value);
    }
}

