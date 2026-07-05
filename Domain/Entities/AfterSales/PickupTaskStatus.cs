namespace Domain.Entities.AfterSales;

/// <summary>
/// 售后取货任务状态，控制司机分配、取货执行和完成阶段。
/// </summary>
public enum PickupTaskStatus
{
    /// <summary>
    /// 待分配：任务已生成，尚未指定取货司机。
    /// </summary>
    PendingAssign = 1,

    /// <summary>
    /// 待取货：司机已分配，尚未开始执行。
    /// </summary>
    PendingPickup = 2,

    /// <summary>
    /// 取货中：司机已开始回收客户退货商品。
    /// </summary>
    PickingUp = 3,

    /// <summary>
    /// 已完成：退货商品已经取回，任务不得再次执行。
    /// </summary>
    Completed = 4,

    /// <summary>
    /// 已取消：任务不再执行且不得进入退货入库。
    /// </summary>
    Cancelled = 5
}
