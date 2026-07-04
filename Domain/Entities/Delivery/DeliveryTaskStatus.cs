namespace Domain.Entities.Delivery;

/// <summary>
/// 配送任务履约状态，控制司机分配、配送执行、异常和签收阶段的可用操作。
/// </summary>
public enum DeliveryTaskStatus
{
    /// <summary>
    /// 待分配：任务已从销售出库生成，尚未指定司机。
    /// </summary>
    PendingAssign = 1,

    /// <summary>
    /// 已分配：已指定司机，允许进行路线规划和配送准备。
    /// </summary>
    Assigned = 2,

    /// <summary>
    /// 配送中：司机已开始执行配送任务。
    /// </summary>
    Delivering = 3,

    /// <summary>
    /// 配送异常：任务存在待处理的配送异常。
    /// </summary>
    Exception = 4,

    /// <summary>
    /// 已签收：客户已完成签收，任务不得再重新分配。
    /// </summary>
    Signed = 5
}
