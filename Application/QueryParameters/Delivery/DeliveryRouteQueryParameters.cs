using System.Linq.Expressions;
using Domain.Entities.Delivery;

namespace Application.QueryParameters;

/// <summary>
///     配送路线查询参数。
/// </summary>
public class DeliveryRouteQueryParameters : NamedCodeQueryParameters
{
    /// <summary>
    ///     构建配送路线查询表达式，支持名称、编码模糊匹配和状态过滤。
    /// </summary>
    public Expression<Func<DeliveryRoute, bool>> QueryBuild()
    {
        return x =>
            (string.IsNullOrWhiteSpace(Name) || x.Name.Contains(Name.Trim())) &&
            (string.IsNullOrWhiteSpace(Code) || x.Code.Contains(Code.Trim())) &&
            (!Status.HasValue || x.Status == Status.Value);
    }
}
