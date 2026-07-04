namespace Domain.Entities.Delivery;

/// <summary>
/// 配送异常处理状态。
/// </summary>
public enum DeliveryExceptionStatus
{
    /// <summary>
    /// 待处理：异常已登记但尚未跟进处理。
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 已处理：异常已跟进并完成闭环。
    /// </summary>
    Handled = 1
}
