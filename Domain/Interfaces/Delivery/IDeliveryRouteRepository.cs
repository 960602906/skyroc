using Domain.Entities.Delivery;

namespace Domain.Interfaces;

/// <summary>
///     配送路线仓储接口，负责路线及其客户关系的持久化。
/// </summary>
public interface IDeliveryRouteRepository : INamedCodeRepository<DeliveryRoute>
{
    /// <summary>
    /// 查询指定客户的启用路线关系，用于按路线和客户顺序规划配送任务。
    /// </summary>
    /// <param name="customerIds">待规划客户主键集合。</param>
    /// <returns>包含路线导航的客户路线关系。</returns>
    Task<IReadOnlyList<CustomerRoute>> GetEnabledCustomerRelationsAsync(IReadOnlyCollection<Guid> customerIds);

    /// <summary>
    ///     以给定客户集合整体替换路线的客户关系，去重并忽略空客户主键。
    /// </summary>
    /// <param name="routeId">配送路线主键。</param>
    /// <param name="customerIds">目标客户主键集合；为空表示清空该路线的所有客户关系。</param>
    Task ReplaceCustomerRelationsAsync(Guid routeId, IEnumerable<Guid>? customerIds);
}
