using Application.DTOs;
using Domain.Entities.System;

namespace Application.DTOs.System;

/// <summary>通知公告响应。</summary>
public class NoticeDto : BaseDto
{
    /// <summary>公告标题。</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>公告正文。</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>公告可见性状态。</summary>
    public NoticeStatus NoticeStatus { get; set; }

    /// <summary>最近一次发布 UTC 时间；草稿为空。</summary>
    public DateTime? PublishedTime { get; set; }
}
