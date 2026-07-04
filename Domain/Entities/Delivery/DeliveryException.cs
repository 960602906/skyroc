using Domain.Entities.Customers;

namespace Domain.Entities.Delivery;

/// <summary>
/// 配送异常实体，记录配送过程中上报的异常及其处理状态。
/// </summary>
public class DeliveryException : BaseEntity
{
    /// <summary>
    /// 配送异常业务唯一编号。
    /// </summary>
    public string ExceptionNo { get; set; } = string.Empty;

    /// <summary>
    /// 关联配送任务主键；历史上未关联任务的异常记录可为空，新登记异常必须填写。
    /// </summary>
    public Guid? DeliveryTaskId { get; set; }

    /// <summary>
    /// 上报异常的司机主键；未指定司机时可为空。
    /// </summary>
    public Guid? DriverId { get; set; }

    /// <summary>
    /// 异常涉及的客户主键；未指定客户时可为空。
    /// </summary>
    public Guid? CustomerId { get; set; }

    /// <summary>
    /// 异常描述，说明配送过程中发生的具体问题。
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 异常处理状态：待处理或已处理。
    /// </summary>
    public DeliveryExceptionStatus HandleStatus { get; set; } = DeliveryExceptionStatus.Pending;

    /// <summary>
    /// 异常处理说明，记录处理动作和结果。
    /// </summary>
    public string? HandleRemark { get; set; }

    /// <summary>
    /// 异常处理完成时间（UTC）。
    /// </summary>
    public DateTime? HandleTime { get; set; }

    /// <summary>
    /// 异常所属配送任务。
    /// </summary>
    public virtual DeliveryTask? DeliveryTask { get; set; }

    /// <summary>
    /// 上报异常的司机。
    /// </summary>
    public virtual Driver? Driver { get; set; }

    /// <summary>
    /// 异常涉及的客户。
    /// </summary>
    public virtual Customer? Customer { get; set; }
}
