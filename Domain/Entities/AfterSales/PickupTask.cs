using Domain.Entities.Delivery;
using Domain.Entities.Storage;

namespace Domain.Entities.AfterSales;

/// <summary>
/// 售后取货任务，按一条退货商品明细记录司机分配、取货地址和执行状态。
/// </summary>
public class PickupTask : BaseEntity
{
    /// <summary>
    /// 取货任务业务唯一编号。
    /// </summary>
    public string TaskNo { get; set; } = string.Empty;

    /// <summary>
    /// 所属售后单主键，必须与关联售后商品的所属售后单一致。
    /// </summary>
    public Guid AfterSaleId { get; set; }

    /// <summary>
    /// 需要回收商品的售后商品明细主键，每条明细最多生成一个任务。
    /// </summary>
    public Guid AfterSaleGoodsId { get; set; }

    /// <summary>
    /// 执行取货任务的司机主键；待分配时为空。
    /// </summary>
    public Guid? DriverId { get; set; }

    /// <summary>
    /// 最近一次分配时的司机姓名快照。
    /// </summary>
    public string? DriverNameSnapshot { get; set; }

    /// <summary>
    /// 最近一次分配时的司机电话快照。
    /// </summary>
    public string? DriverPhoneSnapshot { get; set; }

    /// <summary>
    /// 取货联系人的姓名快照。
    /// </summary>
    public string? ContactNameSnapshot { get; set; }

    /// <summary>
    /// 取货联系人的电话快照。
    /// </summary>
    public string? ContactPhoneSnapshot { get; set; }

    /// <summary>
    /// 司机执行任务使用的取货地址快照。
    /// </summary>
    public string PickupAddressSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 当前取货任务履约状态，初始为待分配。
    /// </summary>
    public PickupTaskStatus PickupStatus { get; set; } = PickupTaskStatus.PendingAssign;

    /// <summary>
    /// 计划上门取货时间（UTC）；未预约时为空。
    /// </summary>
    public DateTime? PlannedPickupTime { get; set; }

    /// <summary>
    /// 最近一次分配司机的时间（UTC）。
    /// </summary>
    public DateTime? AssignedTime { get; set; }

    /// <summary>
    /// 司机开始执行取货任务的时间（UTC）。
    /// </summary>
    public DateTime? StartedTime { get; set; }

    /// <summary>
    /// 退货商品取回并完成任务的时间（UTC）。
    /// </summary>
    public DateTime? CompletedTime { get; set; }

    /// <summary>
    /// 取货调度或执行备注，对处理人员可见。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 所属售后单。
    /// </summary>
    public virtual AfterSale AfterSale { get; set; } = null!;

    /// <summary>
    /// 需要回收商品的售后商品明细。
    /// </summary>
    public virtual AfterSaleGoods AfterSaleGoods { get; set; } = null!;

    /// <summary>
    /// 当前分配的取货司机。
    /// </summary>
    public virtual Driver? Driver { get; set; }

    /// <summary>
    /// 由当前已完成取货任务生成的销售退货入库明细；尚未衔接入库时为空。
    /// </summary>
    public virtual StockInDetail? StockInDetail { get; set; }
}
