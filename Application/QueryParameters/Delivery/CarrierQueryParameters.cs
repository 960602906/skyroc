using System.Linq.Expressions;
using Domain.Entities.Delivery;

namespace Application.QueryParameters;

/// <summary>
///     承运商查询参数。
/// </summary>
public class CarrierQueryParameters : NamedCodeQueryParameters
{
    /// <summary>
    ///     构建承运商查询表达式，支持名称、编码模糊匹配和状态过滤。
    /// </summary>
    public Expression<Func<Carrier, bool>> QueryBuild()
    {
        return x =>
            (string.IsNullOrWhiteSpace(Name) || x.Name.Contains(Name.Trim())) &&
            (string.IsNullOrWhiteSpace(Code) || x.Code.Contains(Code.Trim())) &&
            (!Status.HasValue || x.Status == Status.Value);
    }
}
