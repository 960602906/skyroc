using Application.DTOs.Goods;
using Application.interfaces;
using Application.QueryParameters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using Shared.Constants;
using SkyRoc.Authorization;

namespace SkyRoc.Controllers;

/// <summary>
///     商品分类管理控制器。
/// </summary>
[Route("api/[controller]")]
[Authorize]
[PermissionResource(PermissionCodes.Business.Goods.Resource)]
public class GoodsTypesController(IGoodsTypeService service)
    : BaseDataController<GoodsTypeDto, CreateGoodsTypeDto, UpdateGoodsTypeDto, GoodsTypeQueryParameters>(service)
{
    /// <summary>
    ///     获取商品分类树。
    /// </summary>
    [HttpGet("tree")]
    [Authorize(Policy = PermissionCodes.Business.Goods.Read)]
    public async Task<ActionResult<ApiResponse<PagedResult<GoodsTypeDto>>>> GetTree()
    {
        var result = await service.GetTreeAsync();
        return Ok(ApiResponse<PagedResult<GoodsTypeDto>>.Ok(new PagedResult<GoodsTypeDto>
        {
            Current = 1,
            Size = result.Count,
            Total = result.Count,
            Records = result
        }));
    }
}
