using Application.DTOs.Purchases;
using Application.Interfaces;
using Application.QueryParameters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using Shared.Constants;
using SkyRoc.Authorization;

namespace SkyRoc.Controllers;

/// <summary>
/// 采购计划管理控制器，提供查询、生成、分配、合并和拆分接口。
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
    public async Task<ActionResult<ApiResponse<PagedResult<PurchasePlanDto>>>> GetPaged(
        [FromQuery] PurchasePlanQueryParameters parameters)
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
    public async Task<ActionResult<ApiResponse<PurchasePlanDto>>> GetById(Guid id)
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
    public async Task<ActionResult<ApiResponse<PurchasePlanDto>>> Create([FromBody] CreatePurchasePlanDto dto)
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
    public async Task<ActionResult<ApiResponse<List<PurchasePlanDto>>>> GenerateFromOrders(
        [FromBody] GeneratePurchasePlanFromOrdersDto dto)
    {
        var result = await service.GenerateFromOrdersAsync(dto);
        return Ok(ApiResponse<List<PurchasePlanDto>>.Ok(result));
    }

    /// <summary>
    /// 批量分配或清除未发布采购计划的供应商。需要采购更新权限。
    /// </summary>
    /// <param name="dto">采购计划主键及目标供应商。</param>
    /// <returns>更新后的采购计划详情集合。</returns>
    [HttpPut("supplier")]
    [ResourcePermission(PermissionActions.Update)]
    public async Task<ActionResult<ApiResponse<List<PurchasePlanDto>>>> AssignSupplier(
        [FromBody] AssignPurchasePlanSupplierDto dto)
    {
        var result = await service.AssignSupplierAsync(dto);
        return Ok(ApiResponse<List<PurchasePlanDto>>.Ok(result));
    }

    /// <summary>
    /// 批量分配或清除未发布采购计划的采购员。需要采购更新权限。
    /// </summary>
    /// <param name="dto">采购计划主键及目标采购员。</param>
    /// <returns>更新后的采购计划详情集合。</returns>
    [HttpPut("purchaser")]
    [ResourcePermission(PermissionActions.Update)]
    public async Task<ActionResult<ApiResponse<List<PurchasePlanDto>>>> AssignPurchaser(
        [FromBody] AssignPurchasePlanPurchaserDto dto)
    {
        var result = await service.AssignPurchaserAsync(dto);
        return Ok(ApiResponse<List<PurchasePlanDto>>.Ok(result));
    }

    /// <summary>
    /// 合并采购模式、供应商和采购员一致的未发布采购计划。需要采购更新权限。
    /// </summary>
    /// <param name="dto">待合并采购计划主键及新计划备注。</param>
    /// <returns>合并产生的新采购计划详情。</returns>
    [HttpPost("merge")]
    [ResourcePermission(PermissionActions.Update)]
    public async Task<ActionResult<ApiResponse<PurchasePlanDto>>> Merge([FromBody] MergePurchasePlansDto dto)
    {
        var result = await service.MergeAsync(dto);
        return Ok(ApiResponse<PurchasePlanDto>.Ok(result));
    }

    /// <summary>
    /// 查询指定采购计划中可按订单拆分的来源订单。需要采购读取权限。
    /// </summary>
    /// <param name="planId">采购计划主键。</param>
    /// <returns>来源订单及其需求数量摘要。</returns>
    [HttpGet("{planId:guid}/split-orders")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<List<SplittablePurchasePlanOrderDto>>>> GetSplittableOrders(Guid planId)
    {
        var result = await service.GetSplittableOrdersAsync(planId);
        return Ok(ApiResponse<List<SplittablePurchasePlanOrderDto>>.Ok(result));
    }

    /// <summary>
    /// 按来源销售订单拆分未发布采购计划。需要采购更新权限。
    /// </summary>
    /// <param name="dto">原计划、待拆订单及新计划备注。</param>
    /// <returns>拆分产生的新采购计划详情。</returns>
    [HttpPost("split/orders")]
    [ResourcePermission(PermissionActions.Update)]
    public async Task<ActionResult<ApiResponse<PurchasePlanDto>>> SplitByOrders(
        [FromBody] SplitPurchasePlanByOrdersDto dto)
    {
        var result = await service.SplitByOrdersAsync(dto);
        return Ok(ApiResponse<PurchasePlanDto>.Ok(result));
    }

    /// <summary>
    /// 按商品采购数量拆分未发布采购计划。需要采购更新权限。
    /// </summary>
    /// <param name="dto">原计划及各商品明细拆出数量。</param>
    /// <returns>拆分产生的新采购计划详情。</returns>
    [HttpPost("split/quantity")]
    [ResourcePermission(PermissionActions.Update)]
    public async Task<ActionResult<ApiResponse<PurchasePlanDto>>> SplitByQuantity(
        [FromBody] SplitPurchasePlanByQuantityDto dto)
    {
        var result = await service.SplitByQuantityAsync(dto);
        return Ok(ApiResponse<PurchasePlanDto>.Ok(result));
    }
}
