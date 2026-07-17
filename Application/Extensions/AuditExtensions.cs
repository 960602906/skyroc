using Application.Interfaces;
using Domain.Entities;

namespace Application.Extensions;

/// <summary>
/// 实体审计字段填充扩展（CreateTime/UpdateTime 由 DbContext.SaveChanges 自动填充，此处不设置）。
/// </summary>
public static class AuditExtensions
{
    /// <summary>写入创建人审计字段。</summary>
    public static void ApplyCreateAudit(this BaseEntity entity, ICurrentUserService currentUser)
    {
        entity.CreateBy = currentUser.GetUserId();
        entity.CreateName = currentUser.GetUserName();
    }

    /// <summary>写入更新人审计字段。</summary>
    public static void ApplyUpdateAudit(this BaseEntity entity, ICurrentUserService currentUser)
    {
        entity.UpdateBy = currentUser.GetUserId();
        entity.UpdateName = currentUser.GetUserName();
    }
}
