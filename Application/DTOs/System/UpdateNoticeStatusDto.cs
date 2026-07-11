using Domain.Entities.System;

namespace Application.DTOs.System;

/// <summary>切换通知公告发布状态的请求。</summary>
public class UpdateNoticeStatusDto
{
    /// <summary>目标状态：草稿表示撤回，已发布表示立即对业务端可见。</summary>
    public NoticeStatus NoticeStatus { get; set; }
}
