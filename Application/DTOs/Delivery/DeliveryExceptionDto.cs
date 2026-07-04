using Application.Serialization;
using Domain.Entities.Delivery;
using System.Text.Json.Serialization;

namespace Application.DTOs.Delivery;

/// <summary>
/// 配送异常响应，返回所属任务、司机、客户、异常事实和处理状态。
/// </summary>
public class DeliveryExceptionDto : BaseDto
{
    /// <summary>
    /// 配送异常业务编号。
    /// </summary>
    public string ExceptionNo { get; set; } = string.Empty;

    /// <summary>
    /// 所属配送任务主键。
    /// </summary>
    public Guid? DeliveryTaskId { get; set; }

    /// <summary>
    /// 所属配送任务编号。
    /// </summary>
    public string? DeliveryTaskNo { get; set; }

    /// <summary>
    /// 上报异常的司机主键。
    /// </summary>
    public Guid? DriverId { get; set; }

    /// <summary>
    /// 上报异常的司机姓名。
    /// </summary>
    public string? DriverName { get; set; }

    /// <summary>
    /// 异常涉及的客户主键。
    /// </summary>
    public Guid? CustomerId { get; set; }

    /// <summary>
    /// 异常涉及的客户名称。
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// 配送异常事实描述。
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 异常处理状态：0 待处理，1 已处理。
    /// </summary>
    public DeliveryExceptionStatus HandleStatus { get; set; }

    /// <summary>
    /// 异常处理说明。
    /// </summary>
    public string? HandleRemark { get; set; }

    /// <summary>
    /// 异常处理完成时间（UTC）。
    /// </summary>
    [JsonConverter(typeof(FixedNullableDateTimeJsonConverter))]
    public DateTime? HandleTime { get; set; }
}
