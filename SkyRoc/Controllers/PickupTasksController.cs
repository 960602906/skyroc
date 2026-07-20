using Application.DTOs.AfterSales;
using Application.Interfaces;
using Application.QueryParameters.AfterSales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using Shared.Constants;
using SkyRoc.Authorization;

namespace SkyRoc.Controllers;

/// <summary>
/// 售后取货任务控制器，提供任务查询、司机分配和取货执行状态流转。
/// </summary>
[ApiController]
[Route("api/after-sales/pickup-tasks")]
[Authorize]
[PermissionResource(PermissionCodes.Business.AfterSales.Resource)]
public class PickupTasksController(IPickupTaskService service) : ControllerBase
{
    /// <summary>分页查询取货任务及其销售退货入库衔接状态。</summary>
    /// <param name="parameters">售后、客户、司机、状态和计划时间筛选条件。</param>
    /// <returns>符合条件的取货任务分页结果。</returns>
    [HttpGet]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<PagedResult<PickupTaskDto>>>> GetPaged(
        [FromQuery] PickupTaskQueryParameters parameters)
    {
        var result = await service.GetPagedAsync(parameters);
        return Ok(ApiResponse<PagedResult<PickupTaskDto>>.Ok(result));
    }

    /// <summary>查询单个取货任务的售后来源、调度和履约详情。</summary>
    /// <param name="id">取货任务主键。</param>
    /// <returns>取货任务详情。</returns>
    [HttpGet("{id:guid}")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<PickupTaskDto>>> GetById(Guid id)
    {
        var result = await service.GetByIdAsync(id);
        return Ok(ApiResponse<PickupTaskDto>.Ok(result));
    }

    /// <summary>为尚未开始的取货任务分配或更换启用司机。</summary>
    /// <param name="id">取货任务主键。</param>
    /// <param name="dto">司机、计划上门时间和调度备注。</param>
    /// <returns>分配后的取货任务详情。</returns>
    [HttpPut("{id:guid}/assign")]
    [ResourcePermission(PermissionActions.Update)]
    public async Task<ActionResult<ApiResponse<PickupTaskDto>>> Assign(Guid id, [FromBody] AssignPickupTaskDto dto)
    {
        var result = await service.AssignAsync(id, dto);
        return Ok(ApiResponse<PickupTaskDto>.Ok(result));
    }

    /// <summary>开始执行已分配的取货任务并记录开始时间。</summary>
    /// <param name="id">取货任务主键。</param>
    /// <returns>进入取货中状态的任务详情。</returns>
    [HttpPost("{id:guid}/start")]
    [ResourcePermission(PermissionActions.Update)]
    public async Task<ActionResult<ApiResponse<PickupTaskDto>>> Start(Guid id)
    {
        var result = await service.StartAsync(id);
        return Ok(ApiResponse<PickupTaskDto>.Ok(result));
    }

    /// <summary>完成取货中的任务，使其可以作为销售退货入库来源。</summary>
    /// <param name="id">取货任务主键。</param>
    /// <returns>已完成取货任务详情。</returns>
    [HttpPost("{id:guid}/complete")]
    [ResourcePermission(PermissionActions.Update)]
    public async Task<ActionResult<ApiResponse<PickupTaskDto>>> Complete(Guid id)
    {
        var result = await service.CompleteAsync(id);
        return Ok(ApiResponse<PickupTaskDto>.Ok(result));
    }
}
