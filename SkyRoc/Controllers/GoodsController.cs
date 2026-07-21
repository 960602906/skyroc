using Application.DTOs.Goods;
using Application.Interfaces;
using Application.QueryParameters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using Shared.Constants;
using SkyRoc.Authorization;

namespace SkyRoc.Controllers;

/// <summary>
///     商品档案管理控制器。
/// </summary>
[Route("api/goods")]
[Authorize]
[PermissionResource(PermissionCodes.Business.Goods.Resource)]
public class GoodsController(IGoodsService service)
    : NamedCodeDataController<GoodsDto, CreateGoodsDto, UpdateGoodsDto, GoodsQueryParameters>(service)
{
    /// <summary>
    ///     修改商品上下架状态。
    /// </summary>
    [HttpPatch("{id:guid}/sale-status")]
    [Authorize(Policy = PermissionCodes.Business.Goods.Update)]
    public async Task<ActionResult<ApiResponse<GoodsDto>>> ToggleSaleStatus(Guid id, [FromQuery] bool isOnSale)
    {
        var result = await service.ToggleSaleStatusAsync(id, isOnSale);
        return Ok(ApiResponse<GoodsDto>.Ok(result));
    }
}
