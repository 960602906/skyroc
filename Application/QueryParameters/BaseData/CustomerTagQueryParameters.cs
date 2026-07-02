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
///     客户标签查询参数。
/// </summary>
public class CustomerTagQueryParameters : NamedCodeQueryParameters
{
    /// <summary>
    ///     父级标签 ID。
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    ///     构建查询表达式。
    /// </summary>
    public Expression<Func<CustomerTag, bool>> QueryBuild()
    {
        return x =>
            (string.IsNullOrWhiteSpace(Name) || x.Name.Contains(Name.Trim())) &&
            (string.IsNullOrWhiteSpace(Code) || x.Code.Contains(Code.Trim())) &&
            (!ParentId.HasValue || x.ParentId == ParentId.Value) &&
            (!Status.HasValue || x.Status == Status.Value);
    }
}

