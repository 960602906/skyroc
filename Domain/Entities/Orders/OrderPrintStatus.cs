namespace Domain.Entities.Orders;

/// <summary>
/// 销售订单打印状态。
/// </summary>
public enum OrderPrintStatus
{
    /// <summary>
    /// 订单尚未打印。
    /// </summary>
    NotPrinted = 0,
    /// <summary>
    /// 订单已经打印。
    /// </summary>
    Printed = 1
}
