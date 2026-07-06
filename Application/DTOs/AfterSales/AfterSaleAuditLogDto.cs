using Application.Serialization;
using Domain.Entities.AfterSales;
using System.Text.Json.Serialization;

namespace Application.DTOs.AfterSales;

/// <summary>
/// 售后审核轨迹响应，记录状态变化、操作人和业务说明。
/// </summary>
public class AfterSaleAuditLogDto : BaseDto
{
    /// <summary>本次轨迹动作。</summary>
    public AfterSaleAuditAction Action { get; set; }

    /// <summary>动作执行前的售后状态。</summary>
    public AfterSaleStatus PreviousStatus { get; set; }

    /// <summary>动作执行后的售后状态。</summary>
    public AfterSaleStatus CurrentStatus { get; set; }

    /// <summary>执行动作的系统用户主键；用户已删除时为空。</summary>
    public Guid? AuditUserId { get; set; }

    /// <summary>执行动作时的用户名称快照。</summary>
    public string AuditUserName { get; set; } = string.Empty;

    /// <summary>动作发生时间（UTC），按固定日期时间格式输出。</summary>
    [JsonConverter(typeof(FixedDateTimeJsonConverter))]
    public DateTime AuditTime { get; set; }

    /// <summary>审核意见、驳回原因或反审核说明。</summary>
    public string? Remark { get; set; }
}
