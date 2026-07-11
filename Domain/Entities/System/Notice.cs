namespace Domain.Entities.System;

/// <summary>
/// 通知公告，保存后台发布给系统用户的标题、正文和发布状态。
/// </summary>
public class Notice : BaseEntity
{
    /// <summary>公告标题，供列表和消息入口展示。</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>公告纯文本正文；服务拒绝 HTML 标记，前端按文本而非 HTML 渲染。</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>公告发布状态：草稿不向业务端展示，已发布可被读取。</summary>
    public NoticeStatus NoticeStatus { get; set; }

    /// <summary>最近一次发布的 UTC 时间；草稿或撤回状态为空。</summary>
    public DateTime? PublishedTime { get; set; }
}
