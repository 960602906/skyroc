using Application.DTOs.Delivery;
using Application.Interfaces;
using Application.QueryParameters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using Shared.Constants;
using SkyRoc.Authorization;

namespace SkyRoc.Controllers;

/// <summary>
/// 配送异常控制器，提供任务异常登记、查询和处理闭环接口。
/// </summary>
[ApiController]
[Route("api/delivery-exceptions")]
[Authorize]
[PermissionResource(PermissionCodes.Business.Delivery.Resource)]
public class DeliveryExceptionsController(IDeliveryExceptionService service) : ControllerBase
{
    /// <summary>
    /// 分页查询配送异常及其任务、司机和客户信息。需要配送读取权限。
    /// </summary>
    /// <param name="parameters">异常分页与任务、司机、客户、状态等筛选参数。</param>
    /// <returns>配送异常分页结果。</returns>
    [HttpGet]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<PagedResult<DeliveryExceptionDto>>>> GetPaged(
        [FromQuery] DeliveryExceptionQueryParameters parameters)
    {
        var result = await service.GetPagedAsync(parameters);
        return Ok(ApiResponse<PagedResult<DeliveryExceptionDto>>.Ok(result));
    }

    /// <summary>
    /// 查询配送异常详情。需要配送读取权限。
    /// </summary>
    /// <param name="id">配送异常主键。</param>
    /// <returns>配送异常详情。</returns>
    [HttpGet("{id:guid}")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<DeliveryExceptionDto>>> GetById(Guid id)
    {
        var result = await service.GetByIdAsync(id);
        return Ok(ApiResponse<DeliveryExceptionDto>.Ok(result));
    }

    /// <summary>
    /// 为已分配且尚未签收的配送任务登记异常，并同步任务异常状态。需要配送创建权限。
    /// </summary>
    /// <param name="dto">异常所属任务和事实描述。</param>
    /// <returns>新登记的配送异常。</returns>
    [HttpPost]
    [ResourcePermission(PermissionActions.Create)]
    public async Task<ActionResult<ApiResponse<DeliveryExceptionDto>>> Create(
        [FromBody] CreateDeliveryExceptionDto dto)
    {
        var result = await service.CreateAsync(dto);
        return Ok(ApiResponse<DeliveryExceptionDto>.Ok(result));
    }

    /// <summary>
    /// 完成待处理配送异常；没有其他待处理异常时恢复任务执行状态。需要配送更新权限。
    /// </summary>
    /// <param name="id">配送异常主键。</param>
    /// <param name="dto">异常处理动作与结果。</param>
    /// <returns>处理完成后的配送异常。</returns>
    [HttpPut("{id:guid}/handle")]
    [ResourcePermission(PermissionActions.Update)]
    public async Task<ActionResult<ApiResponse<DeliveryExceptionDto>>> Handle(
        Guid id,
        [FromBody] HandleDeliveryExceptionDto dto)
    {
        var result = await service.HandleAsync(id, dto);
        return Ok(ApiResponse<DeliveryExceptionDto>.Ok(result));
    }
}
