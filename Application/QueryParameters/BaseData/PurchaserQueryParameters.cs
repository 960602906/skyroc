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
///     采购员查询参数。
/// </summary>
public class PurchaserQueryParameters : NamedCodeQueryParameters
{
    /// <summary>
    ///     所属部门 ID。
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    ///     构建查询表达式。
    /// </summary>
    public Expression<Func<Purchaser, bool>> QueryBuild()
    {
        return x =>
            (string.IsNullOrWhiteSpace(Name) || x.Name.Contains(Name.Trim())) &&
            (string.IsNullOrWhiteSpace(Code) || x.Code.Contains(Code.Trim())) &&
            (!DepartmentId.HasValue || x.DepartmentId == DepartmentId.Value) &&
            (!Status.HasValue || x.Status == Status.Value);
    }
}

