using Application.DTOs.Goods;
using Application.interfaces;
using Application.QueryParameters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;

namespace SkyRoc.Controllers;

/// <summary>
///     商品单位管理控制器。
/// </summary>
[Route("api/[controller]")]
[Authorize]
public class GoodsUnitsController(IGoodsUnitService service)
    : BaseDataController<GoodsUnitDto, CreateGoodsUnitDto, UpdateGoodsUnitDto, GoodsUnitQueryParameters>(service)
{
    /// <summary>
    ///     查询指定商品的单位列表。
    /// </summary>
    [HttpGet("by-goods/{goodsId:guid}")]
    public async Task<IActionResult> GetByGoodsId(Guid goodsId)
    {
        var result = await service.GetByGoodsIdAsync(goodsId);
        return Ok(ApiResponse<List<GoodsUnitDto>>.Ok(result));
    }
}
