using Application.DTOs.System;
using Application.Interfaces.System;
using Application.QueryParameters.System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using Shared.Constants;
using SkyRoc.Authorization;

namespace SkyRoc.Controllers;

/// <summary>关键操作和登录安全审计日志的只读查询接口。</summary>
[ApiController]
[Route("api/logs")]
[Authorize]
[PermissionResource(PermissionCodes.System.Logs.Resource)]
public class LogsController(ISystemSupportService service) : ControllerBase
{
    /// <summary>分页查询关键操作审计日志。</summary>
    /// <param name="query">模块、操作类型、成功状态和 UTC 时间范围筛选。</param>
    /// <returns>按发生时间倒序的操作日志。</returns>
    [HttpGet("operations")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<PagedResult<OperationLogDto>>>> GetOperations([FromQuery] OperationLogQueryParameters query) => Ok(ApiResponse<PagedResult<OperationLogDto>>.Ok(await service.GetOperationLogsAsync(query)));

    /// <summary>分页查询登录审计日志。</summary>
    /// <param name="query">登录名、成功状态和 UTC 时间范围筛选。</param>
    /// <returns>按登录时间倒序的登录日志。</returns>
    [HttpGet("logins")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<PagedResult<LoginLogDto>>>> GetLogins([FromQuery] LoginLogQueryParameters query) => Ok(ApiResponse<PagedResult<LoginLogDto>>.Ok(await service.GetLoginLogsAsync(query)));
}
