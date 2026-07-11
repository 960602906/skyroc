namespace Domain.Entities.System;

/// <summary>
/// 通知公告的可见性状态。
/// </summary>
public enum NoticeStatus
{
    /// <summary>仅管理员可维护，尚未对业务端发布。</summary>
    Draft = 0,

    /// <summary>已发布，允许具备读取权限的用户查看。</summary>
    Published = 1
}
