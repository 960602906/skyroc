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
/// 配送任务控制器，提供销售出库生成、订单/司机任务查询、司机分配和路线规划接口。
/// </summary>
[ApiController]
[Route("api/delivery-tasks")]
[Authorize]
[PermissionResource(PermissionCodes.Business.Delivery.Resource)]
public class DeliveryTasksController(IDeliveryTaskService service) : ControllerBase
{
    /// <summary>
    /// 分页查询配送订单任务。需要配送读取权限。
    /// </summary>
    /// <param name="parameters">任务分页与客户、司机、路线、状态等筛选参数。</param>
    /// <returns>配送订单任务分页结果。</returns>
    [HttpGet]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<PagedResult<DeliveryTaskDto>>>> GetOrderTasks(
        [FromQuery] DeliveryTaskQueryParameters parameters)
    {
        var result = await service.GetOrderTasksAsync(parameters);
        return Ok(ApiResponse<PagedResult<DeliveryTaskDto>>.Ok(result));
    }

    /// <summary>
    /// 分页查询已经分配司机的配送任务。需要配送读取权限。
    /// </summary>
    /// <param name="parameters">司机任务分页与组合筛选参数。</param>
    /// <returns>司机配送任务分页结果。</returns>
    [HttpGet("driver")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<PagedResult<DeliveryTaskDto>>>> GetDriverTasks(
        [FromQuery] DeliveryTaskQueryParameters parameters)
    {
        var result = await service.GetDriverTasksAsync(parameters);
        return Ok(ApiResponse<PagedResult<DeliveryTaskDto>>.Ok(result));
    }

    /// <summary>
    /// 查询配送任务详情。需要配送读取权限。
    /// </summary>
    /// <param name="id">配送任务主键。</param>
    /// <returns>配送任务完整详情。</returns>
    [HttpGet("{id:guid}")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<DeliveryTaskDto>>> GetById(Guid id)
    {
        var result = await service.GetByIdAsync(id);
        return Ok(ApiResponse<DeliveryTaskDto>.Ok(result));
    }

    /// <summary>
    /// 从已审核销售出库单幂等生成配送任务。需要配送创建权限。
    /// </summary>
    /// <param name="stockOutOrderId">已审核销售出库单主键。</param>
    /// <returns>新建或已存在的配送任务。</returns>
    [HttpPost("generate/{stockOutOrderId:guid}")]
    [ResourcePermission(PermissionActions.Create)]
    public async Task<ActionResult<ApiResponse<DeliveryTaskDto>>> Generate(Guid stockOutOrderId)
    {
        var result = await service.GenerateFromStockOutAsync(stockOutOrderId);
        return Ok(ApiResponse<DeliveryTaskDto>.Ok(result));
    }

    /// <summary>
    /// 为待分配或已分配配送任务批量指定启用司机。需要配送更新权限。
    /// </summary>
    /// <param name="dto">任务主键集合和目标司机主键。</param>
    /// <returns>更新后的配送任务集合。</returns>
    [HttpPut("driver")]
    [ResourcePermission(PermissionActions.Update)]
    public async Task<ActionResult<ApiResponse<List<DeliveryTaskDto>>>> AssignDriver(
        [FromBody] AssignDeliveryDriverDto dto)
    {
        var result = await service.AssignDriverAsync(dto);
        return Ok(ApiResponse<List<DeliveryTaskDto>>.Ok(result));
    }

    /// <summary>
    /// 按客户已配置的启用配送路线批量规划任务。需要配送更新权限。
    /// </summary>
    /// <param name="dto">待规划任务主键集合。</param>
    /// <returns>规划后的配送任务集合。</returns>
    [HttpPut("intelligent-plan")]
    [ResourcePermission(PermissionActions.Update)]
    public async Task<ActionResult<ApiResponse<List<DeliveryTaskDto>>>> IntelligentPlan(
        [FromBody] IntelligentPlanDeliveryTasksDto dto)
    {
        var result = await service.IntelligentPlanAsync(dto);
        return Ok(ApiResponse<List<DeliveryTaskDto>>.Ok(result));
    }
}
