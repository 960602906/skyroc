namespace Application.DTOs.System;

/// <summary>新建或编辑通知公告的请求。</summary>
public class UpsertNoticeDto
{
    /// <summary>公告标题，去除首尾空白后长度为 1 至 200 个字符。</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>公告正文，去除首尾空白后长度为 1 至 20,000 个字符。</summary>
    public string Content { get; set; } = string.Empty;
}
