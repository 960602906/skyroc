using Application.DTOs.Delivery;
using Application.interfaces;
using Application.QueryParameters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using Shared.Constants;
using SkyRoc.Authorization;

namespace SkyRoc.Controllers;

/// <summary>
///     配送路线管理控制器，提供路线 CRUD 及客户分配能力。
/// </summary>
[Route("api/[controller]")]
[Authorize]
[PermissionResource(PermissionCodes.Business.Delivery.Resource)]
public class RoutesController(IDeliveryRouteService service)
    : BaseDataController<DeliveryRouteDto, CreateDeliveryRouteDto, UpdateDeliveryRouteDto, DeliveryRouteQueryParameters>(
        service)
{
    /// <summary>
    ///     分配客户到指定配送路线，用请求集合整体替换该路线的客户关系。
    /// </summary>
    /// <param name="routeId">配送路线主键。</param>
    /// <param name="dto">客户分配请求，包含目标客户 ID 集合。</param>
    /// <returns>包含最新客户关系的配送路线详情。</returns>
    [HttpPut("{routeId:guid}/customers")]
    [Authorize(Policy = PermissionCodes.Business.Delivery.Update)]
    public async Task<IActionResult> DispatchCustomers(Guid routeId, [FromBody] DispatchRouteCustomersDto dto)
    {
        var result = await service.DispatchCustomersAsync(routeId, dto.CustomerIds);
        return Ok(ApiResponse<DeliveryRouteDto>.Ok(result));
    }
}
