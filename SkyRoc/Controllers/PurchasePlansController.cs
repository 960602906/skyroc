using Application.DTOs.Purchases;
using Application.interfaces;
using Application.QueryParameters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using Shared.Constants;
using SkyRoc.Authorization;

namespace SkyRoc.Controllers;

/// <summary>
/// 采购计划管理控制器，提供查询、手工新增和从已审核订单生成计划的接口。
/// </summary>
[ApiController]
[Route("api/purchase-plans")]
[Authorize]
[PermissionResource(PermissionCodes.Business.Purchases.Resource)]
public class PurchasePlansController(IPurchasePlanService service) : ControllerBase
{
    /// <summary>
    /// 分页查询采购计划。需要采购读取权限。
    /// </summary>
    /// <param name="parameters">分页与筛选参数。</param>
    /// <returns>分页后的采购计划集合。</returns>
    [HttpGet("list")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<IActionResult> GetPaged([FromQuery] PurchasePlanQueryParameters parameters)
    {
        var result = await service.GetPagedAsync(parameters);
        return Ok(ApiResponse<PagedResult<PurchasePlanDto>>.Ok(result));
    }

    /// <summary>
    /// 查询采购计划详情，含商品明细与订单来源关系。需要采购读取权限。
    /// </summary>
    /// <param name="id">采购计划主键。</param>
    /// <returns>采购计划详情。</returns>
    [HttpGet("{id:guid}")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await service.GetByIdAsync(id);
        return Ok(ApiResponse<PurchasePlanDto>.Ok(result));
    }

    /// <summary>
    /// 手工新增采购计划及其商品明细。需要采购创建权限。
    /// </summary>
    /// <param name="dto">采购计划创建请求。</param>
    /// <returns>创建后的采购计划详情。</returns>
    [HttpPost]
    [ResourcePermission(PermissionActions.Create)]
    public async Task<IActionResult> Create([FromBody] CreatePurchasePlanDto dto)
    {
        var result = await service.CreateAsync(dto);
        return Ok(ApiResponse<PurchasePlanDto>.Ok(result));
    }

    /// <summary>
    /// 从已审核通过的销售订单批量生成采购计划。需要采购创建权限。
    /// </summary>
    /// <param name="dto">来源销售订单集合及备注。</param>
    /// <returns>本次生成的采购计划详情集合。</returns>
    [HttpPost("generate")]
    [ResourcePermission(PermissionActions.Create)]
    public async Task<IActionResult> GenerateFromOrders([FromBody] GeneratePurchasePlanFromOrdersDto dto)
    {
        var result = await service.GenerateFromOrdersAsync(dto);
        return Ok(ApiResponse<List<PurchasePlanDto>>.Ok(result));
    }
}
