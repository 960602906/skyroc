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
/// 库存查询控制器，提供仓库商品总览、批次余额和只追加台账的只读接口。
/// </summary>
[ApiController]
[Route("api/stock")]
[Authorize]
[PermissionResource(PermissionCodes.Business.Storage.Resource)]
public class StockQueryController(IStockQueryService service) : ControllerBase
{
    /// <summary>
    /// 按仓库和商品分页汇总当前数量、可用数量、占用量与货值。需要库存读取权限。
    /// </summary>
    /// <param name="parameters">仓库、分类、商品、关键字和零库存筛选参数。</param>
    /// <returns>仓库商品粒度的库存总览分页结果。</returns>
    [HttpGet("overview")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<PagedResult<StockOverviewDto>>>> GetOverview([FromQuery] StockOverviewQueryParameters parameters)
    {
        var result = await service.GetOverviewAsync(parameters);
        return Ok(ApiResponse<PagedResult<StockOverviewDto>>.Ok(result));
    }

    /// <summary>
    /// 分页查询库存批次余额、成本、生产日期和到期日期。需要库存读取权限。
    /// </summary>
    /// <param name="parameters">仓库、分类、商品、批次号、效期和零库存筛选参数。</param>
    /// <returns>库存批次分页结果。</returns>
    [HttpGet("batches")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<PagedResult<StockBatchDto>>>> GetBatches([FromQuery] StockBatchQueryParameters parameters)
    {
        var result = await service.GetBatchesAsync(parameters);
        return Ok(ApiResponse<PagedResult<StockBatchDto>>.Ok(result));
    }

    /// <summary>
    /// 分页查询审核与反审核产生的库存增减台账，按发生时间倒序返回。需要库存读取权限。
    /// </summary>
    /// <param name="parameters">仓库、商品、批次、方向、来源和发生时间筛选参数。</param>
    /// <returns>包含来源单据、带方向数量和批次余额的台账分页结果。</returns>
    [HttpGet("ledgers")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<PagedResult<StockLedgerDto>>>> GetLedgers([FromQuery] StockLedgerQueryParameters parameters)
    {
        var result = await service.GetLedgersAsync(parameters);
        return Ok(ApiResponse<PagedResult<StockLedgerDto>>.Ok(result));
    }
}
