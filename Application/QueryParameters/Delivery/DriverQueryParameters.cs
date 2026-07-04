using System.Linq.Expressions;
using Domain.Entities.Delivery;

namespace Application.QueryParameters;

/// <summary>
///     司机查询参数。
/// </summary>
public class DriverQueryParameters : NamedCodeQueryParameters
{
    /// <summary>
    ///     所属承运商 ID，用于按承运商过滤司机。
    /// </summary>
    public Guid? CarrierId { get; set; }

    /// <summary>
    ///     构建司机查询表达式，支持名称、编码模糊匹配、承运商和状态过滤。
    /// </summary>
    public Expression<Func<Driver, bool>> QueryBuild()
    {
        return x =>
            (string.IsNullOrWhiteSpace(Name) || x.Name.Contains(Name.Trim())) &&
            (string.IsNullOrWhiteSpace(Code) || x.Code.Contains(Code.Trim())) &&
            (!CarrierId.HasValue || x.CarrierId == CarrierId.Value) &&
            (!Status.HasValue || x.Status == Status.Value);
    }
}
