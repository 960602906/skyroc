using Application.DTOs.System;
using Application.interfaces.System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using Shared.Constants;
using SkyRoc.Authorization;

namespace SkyRoc.Controllers;

/// <summary>通知公告的查询、草稿维护、发布撤回和删除接口。</summary>
[ApiController]
[Route("api/notices")]
[Authorize]
[PermissionResource(PermissionCodes.System.Notices.Resource)]
public class NoticesController(ISystemSupportService service) : ControllerBase
{
    /// <summary>分页查询通知公告。</summary>
    /// <param name="current">从 1 开始的页码。</param>
    /// <param name="size">每页记录数，最大由全局分页约束限制。</param>
    /// <param name="includeDraft">是否包含草稿公告。</param>
    /// <returns>公告分页数据。</returns>
    [HttpGet]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<PagedResult<NoticeDto>>>> GetPaged([FromQuery] int current = 1, [FromQuery] int size = 20, [FromQuery] bool includeDraft = false) => Ok(ApiResponse<PagedResult<NoticeDto>>.Ok(await service.GetNoticesAsync(current, size, includeDraft)));

    /// <summary>新建草稿通知公告。</summary>
    /// <param name="dto">公告标题和正文。</param>
    /// <returns>新建草稿。</returns>
    [HttpPost]
    [ResourcePermission(PermissionActions.Create)]
    public async Task<ActionResult<ApiResponse<NoticeDto>>> Create(UpsertNoticeDto dto) => Ok(ApiResponse<NoticeDto>.Ok(await service.CreateNoticeAsync(dto)));

    /// <summary>更新通知公告内容，不改变当前发布状态。</summary>
    /// <param name="id">公告主键。</param>
    /// <param name="dto">公告标题和正文。</param>
    /// <returns>更新后的公告。</returns>
    [HttpPut("{id:guid}")]
    [ResourcePermission(PermissionActions.Update)]
    public async Task<ActionResult<ApiResponse<NoticeDto>>> Update(Guid id, UpsertNoticeDto dto) => Ok(ApiResponse<NoticeDto>.Ok(await service.UpdateNoticeAsync(id, dto)));

    /// <summary>发布或撤回通知公告。</summary>
    /// <param name="id">公告主键。</param>
    /// <param name="dto">目标发布状态。</param>
    /// <returns>更新状态后的公告。</returns>
    [HttpPatch("{id:guid}/status")]
    [ResourcePermission(PermissionActions.Update)]
    public async Task<ActionResult<ApiResponse<NoticeDto>>> UpdateStatus(Guid id, UpdateNoticeStatusDto dto) => Ok(ApiResponse<NoticeDto>.Ok(await service.UpdateNoticeStatusAsync(id, dto)));

    /// <summary>删除通知公告。</summary>
    /// <param name="id">公告主键。</param>
    /// <returns>删除成功标记。</returns>
    [HttpDelete("{id:guid}")]
    [ResourcePermission(PermissionActions.Delete)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id) { await service.DeleteNoticeAsync(id); return Ok(ApiResponse<bool>.Ok(true)); }
}
