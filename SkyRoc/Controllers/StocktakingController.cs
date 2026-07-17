using Application.DTOs.Storage;
using Application.Interfaces;
using Application.QueryParameters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using Shared.Constants;
using SkyRoc.Authorization;

namespace SkyRoc.Controllers;

/// <summary>
/// 库存盘点控制器，提供盘点快照查询、创建和一次性差异调整审核接口。
/// </summary>
[ApiController]
[Route("api/stocktaking")]
[Authorize]
[PermissionResource(PermissionCodes.Business.Storage.Resource)]
public class StocktakingController(IStocktakingService service) : ControllerBase
{
    /// <summary>
    /// 分页查询库存盘点单和批次账实差异。需要库存读取权限。
    /// </summary>
    /// <param name="parameters">盘点分页及筛选参数。</param>
    /// <returns>盘点单分页结果。</returns>
    [HttpGet("list")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<PagedResult<StocktakingOrderDto>>>> GetPaged([FromQuery] StocktakingQueryParameters parameters)
    {
        var result = await service.GetPagedAsync(parameters);
        return Ok(ApiResponse<PagedResult<StocktakingOrderDto>>.Ok(result));
    }

    /// <summary>
    /// 查询库存盘点单完整详情。需要库存读取权限。
    /// </summary>
    /// <param name="id">盘点单主键。</param>
    /// <returns>包含批次账面、实盘和差异数量的盘点详情。</returns>
    [HttpGet("{id:guid}")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<StocktakingOrderDto>>> GetById(Guid id)
    {
        var result = await service.GetByIdAsync(id);
        return Ok(ApiResponse<StocktakingOrderDto>.Ok(result));
    }

    /// <summary>
    /// 创建库存盘点草稿，按当前批次余额固化账面数量和成本并计算差异。需要库存创建权限。
    /// </summary>
    /// <param name="dto">盘点仓库、批次实盘数量和差异说明。</param>
    /// <returns>创建后的库存盘点详情。</returns>
    [HttpPost]
    [ResourcePermission(PermissionActions.Create)]
    public async Task<ActionResult<ApiResponse<StocktakingOrderDto>>> Create([FromBody] CreateStocktakingDto dto)
    {
        var result = await service.CreateAsync(dto);
        return Ok(ApiResponse<StocktakingOrderDto>.Ok(result));
    }

    /// <summary>
    /// 审核库存盘点，锁定批次后执行盘盈盘亏并只追加调整流水。需要库存更新权限。
    /// </summary>
    /// <param name="id">待审核盘点单主键。</param>
    /// <param name="dto">写入调整流水的审核说明。</param>
    /// <returns>审核并完成库存调整后的盘点详情。</returns>
    [HttpPost("{id:guid}/audit")]
    [ResourcePermission(PermissionActions.Update)]
    public async Task<ActionResult<ApiResponse<StocktakingOrderDto>>> Audit(Guid id, [FromBody] StocktakingAuditDto? dto)
    {
        var result = await service.AuditAsync(id, dto?.Remark);
        return Ok(ApiResponse<StocktakingOrderDto>.Ok(result));
    }
}
