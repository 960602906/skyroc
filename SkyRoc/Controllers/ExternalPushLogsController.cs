using Application.DTOs.Traceability;
using Application.Interfaces;
using Application.QueryParameters.Traceability;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using Shared.Constants;
using SkyRoc.Authorization;

namespace SkyRoc.Controllers;

/// <summary>外部报送日志控制器，提供监管或溯源平台报送状态、脱敏报文和失败摘要查询。</summary>
[ApiController]
[Route("api/traceability/push-logs")]
[Authorize]
[PermissionResource(PermissionCodes.Business.Traceability.Resource)]
public class ExternalPushLogsController(ITraceabilityService service) : ControllerBase
{
    /// <summary>分页查询外部平台报送日志。</summary>
    /// <param name="parameters">业务类型、来源主键、平台、状态、报送时间和关键字筛选条件。</param>
    /// <returns>外部报送日志分页结果。</returns>
    [HttpGet]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<PagedResult<ExternalPushLogDto>>>> GetPaged(
        [FromQuery] ExternalPushLogQueryParameters parameters)
    {
        return Ok(ApiResponse<PagedResult<ExternalPushLogDto>>.Ok(await service.GetExternalPushLogsAsync(parameters)));
    }
}
