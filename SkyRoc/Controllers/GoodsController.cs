using Application.DTOs.Goods;
using Application.interfaces;
using Application.QueryParameters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;

namespace SkyRoc.Controllers;

/// <summary>
///     商品档案管理控制器。
/// </summary>
[Route("api/[controller]")]
[Authorize]
public class GoodsController(IGoodsService service)
    : BaseDataController<GoodsDto, CreateGoodsDto, UpdateGoodsDto, GoodsQueryParameters>(service)
{
    /// <summary>
    ///     修改商品上下架状态。
    /// </summary>
    [HttpPatch("{id:guid}/sale-status")]
    public async Task<IActionResult> ToggleSaleStatus(Guid id, [FromQuery] bool isOnSale)
    {
        var result = await service.ToggleSaleStatusAsync(id, isOnSale);
        return Ok(ApiResponse<GoodsDto>.Ok(result));
    }
}
