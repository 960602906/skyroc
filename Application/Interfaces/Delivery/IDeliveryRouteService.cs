using Application.DTOs.Delivery;
using Application.QueryParameters;

namespace Application.Interfaces;

/// <summary>
///     配送路线基础资料维护用例，支持路线 CRUD 及客户分配。
/// </summary>
public interface IDeliveryRouteService
    : INamedCodeBaseDataService<DeliveryRouteDto, CreateDeliveryRouteDto, UpdateDeliveryRouteDto, DeliveryRouteQueryParameters>
{
    /// <summary>
    ///     用给定客户集合整体替换指定路线的客户关系。
    /// </summary>
    /// <param name="routeId">配送路线主键。</param>
    /// <param name="customerIds">目标客户主键集合；为空表示清空该路线的所有客户关系。</param>
    /// <returns>包含最新客户关系的路线详情。</returns>
    Task<DeliveryRouteDto> DispatchCustomersAsync(Guid routeId, List<Guid>? customerIds);
}
