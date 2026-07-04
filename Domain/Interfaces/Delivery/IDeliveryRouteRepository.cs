using Domain.Entities.Delivery;

namespace Domain.Interfaces;

/// <summary>
///     配送路线仓储接口，负责路线及其客户关系的持久化。
/// </summary>
public interface IDeliveryRouteRepository : INamedCodeRepository<DeliveryRoute>
{
    /// <summary>
    ///     以给定客户集合整体替换路线的客户关系，去重并忽略空客户主键。
    /// </summary>
    /// <param name="routeId">配送路线主键。</param>
    /// <param name="customerIds">目标客户主键集合；为空表示清空该路线的所有客户关系。</param>
    Task ReplaceCustomerRelationsAsync(Guid routeId, IEnumerable<Guid>? customerIds);
}
