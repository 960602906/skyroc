using Application.DTOs.System;
using Application.interfaces.System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using Shared.Constants;
using SkyRoc.Authorization;

namespace SkyRoc.Controllers;

/// <summary>运营服务时段、小程序下单和分拣权重的受保护管理接口。</summary>
[ApiController]
[Route("api/system-settings")]
[Authorize]
[PermissionResource(PermissionCodes.System.Operations.Resource)]
public class SystemSettingsController(ISystemSupportService service) : ControllerBase
{
    /// <summary>查询运营服务时段。</summary>
    /// <param name="includeDisabled">为 true 时包含已停用时段，默认仅返回启用时段。</param>
    /// <returns>按顺序排列的服务时段集合。</returns>
    [HttpGet("service-periods")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ServicePeriodDto>>>> GetServicePeriods([FromQuery] bool includeDisabled = false) => Ok(ApiResponse<IReadOnlyList<ServicePeriodDto>>.Ok(await service.GetServicePeriodsAsync(includeDisabled)));

    /// <summary>查询单个运营服务时段。</summary>
    /// <param name="id">服务时段主键。</param>
    /// <returns>服务时段详情。</returns>
    [HttpGet("service-periods/{id:guid}")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<ServicePeriodDto>>> GetServicePeriod(Guid id) => Ok(ApiResponse<ServicePeriodDto>.Ok(await service.GetServicePeriodAsync(id)));

    /// <summary>新增运营服务时段。</summary>
    /// <param name="dto">时段名称、时间边界、顺序和启用状态。</param>
    /// <returns>新建服务时段。</returns>
    [HttpPost("service-periods")]
    [ResourcePermission(PermissionActions.Create)]
    public async Task<ActionResult<ApiResponse<ServicePeriodDto>>> CreateServicePeriod(UpsertServicePeriodDto dto) => Ok(ApiResponse<ServicePeriodDto>.Ok(await service.CreateServicePeriodAsync(dto)));

    /// <summary>完整更新运营服务时段。</summary>
    /// <param name="id">服务时段主键。</param>
    /// <param name="dto">时段名称、时间边界、顺序和启用状态。</param>
    /// <returns>更新后的服务时段。</returns>
    [HttpPut("service-periods/{id:guid}")]
    [ResourcePermission(PermissionActions.Update)]
    public async Task<ActionResult<ApiResponse<ServicePeriodDto>>> UpdateServicePeriod(Guid id, UpsertServicePeriodDto dto) => Ok(ApiResponse<ServicePeriodDto>.Ok(await service.UpdateServicePeriodAsync(id, dto)));

    /// <summary>删除运营服务时段。</summary>
    /// <param name="id">服务时段主键。</param>
    /// <returns>删除成功标记。</returns>
    [HttpDelete("service-periods/{id:guid}")]
    [ResourcePermission(PermissionActions.Delete)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteServicePeriod(Guid id) { await service.DeleteServicePeriodAsync(id); return Ok(ApiResponse<bool>.Ok(true)); }

    /// <summary>读取小程序下单全局设置。</summary>
    /// <returns>下单开关与提前下单天数。</returns>
    [HttpGet("mini-program-order")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<MiniProgramOrderSettingsDto>>> GetMiniProgramOrderSettings() => Ok(ApiResponse<MiniProgramOrderSettingsDto>.Ok(await service.GetMiniProgramOrderSettingsAsync()));

    /// <summary>保存小程序下单全局设置。</summary>
    /// <param name="dto">下单开关与提前下单天数。</param>
    /// <returns>保存后的设置。</returns>
    [HttpPut("mini-program-order")]
    [ResourcePermission(PermissionActions.Update)]
    public async Task<ActionResult<ApiResponse<MiniProgramOrderSettingsDto>>> SaveMiniProgramOrderSettings(MiniProgramOrderSettingsDto dto) => Ok(ApiResponse<MiniProgramOrderSettingsDto>.Ok(await service.SaveMiniProgramOrderSettingsAsync(dto)));

    /// <summary>读取分拣排序权重设置。</summary>
    /// <returns>当前分拣权重。</returns>
    [HttpGet("sorting-weights")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<SortingWeightSettingsDto>>> GetSortingWeightSettings() => Ok(ApiResponse<SortingWeightSettingsDto>.Ok(await service.GetSortingWeightSettingsAsync()));

    /// <summary>保存分拣排序权重设置。</summary>
    /// <param name="dto">订单时间、路线和客户权重。</param>
    /// <returns>保存后的分拣权重。</returns>
    [HttpPut("sorting-weights")]
    [ResourcePermission(PermissionActions.Update)]
    public async Task<ActionResult<ApiResponse<SortingWeightSettingsDto>>> SaveSortingWeightSettings(SortingWeightSettingsDto dto) => Ok(ApiResponse<SortingWeightSettingsDto>.Ok(await service.SaveSortingWeightSettingsAsync(dto)));
}
