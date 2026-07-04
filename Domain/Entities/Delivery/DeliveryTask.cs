using Domain.Entities.Customers;
using Domain.Entities.Orders;
using Domain.Entities.Storage;

namespace Domain.Entities.Delivery;

/// <summary>
/// 配送任务实体，每张已审核销售出库单只能生成一条任务，并固化客户、仓库、司机和路线履约快照。
/// </summary>
public class DeliveryTask : BaseEntity
{
    /// <summary>
    /// 配送任务业务唯一编号。
    /// </summary>
    public string TaskNo { get; set; } = string.Empty;

    /// <summary>
    /// 来源销售出库单主键，同一出库单只能关联一条配送任务。
    /// </summary>
    public Guid StockOutOrderId { get; set; }

    /// <summary>
    /// 来源销售订单主键，用于后续签收和订单状态联动。
    /// </summary>
    public Guid SaleOrderId { get; set; }

    /// <summary>
    /// 收货客户主键。
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// 任务生成时的客户名称快照。
    /// </summary>
    public string CustomerNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 任务生成时的收货联系人姓名快照。
    /// </summary>
    public string? ContactNameSnapshot { get; set; }

    /// <summary>
    /// 任务生成时的收货联系人电话快照。
    /// </summary>
    public string? ContactPhoneSnapshot { get; set; }

    /// <summary>
    /// 任务生成时的收货地址快照。
    /// </summary>
    public string? DeliveryAddressSnapshot { get; set; }

    /// <summary>
    /// 发货仓库主键。
    /// </summary>
    public Guid WareId { get; set; }

    /// <summary>
    /// 任务生成时的发货仓库名称快照。
    /// </summary>
    public string WareNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 执行任务的司机主键；待分配时为空。
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
    /// 最近一次分配时的承运商主键；自营司机可为空。
    /// </summary>
    public Guid? CarrierId { get; set; }

    /// <summary>
    /// 最近一次分配时的承运商名称快照。
    /// </summary>
    public string? CarrierNameSnapshot { get; set; }

    /// <summary>
    /// 智能规划匹配的配送路线主键；客户未配置路线时为空。
    /// </summary>
    public Guid? RouteId { get; set; }

    /// <summary>
    /// 最近一次规划时的配送路线名称快照。
    /// </summary>
    public string? RouteNameSnapshot { get; set; }

    /// <summary>
    /// 客户在匹配路线内的配送顺序，数值越小越优先。
    /// </summary>
    public int? RouteSequence { get; set; }

    /// <summary>
    /// 当前配送履约状态。
    /// </summary>
    public DeliveryTaskStatus DeliveryStatus { get; set; } = DeliveryTaskStatus.PendingAssign;

    /// <summary>
    /// 来源销售出库单的实际出库时间（UTC）。
    /// </summary>
    public DateTime OutTime { get; set; }

    /// <summary>
    /// 最近一次分配司机的时间（UTC）。
    /// </summary>
    public DateTime? AssignedTime { get; set; }

    /// <summary>
    /// 最近一次执行路线规划的时间（UTC）。
    /// </summary>
    public DateTime? PlannedTime { get; set; }

    /// <summary>
    /// 司机开始执行配送任务的时间（UTC）；尚未开始时为空。
    /// </summary>
    public DateTime? StartedTime { get; set; }

    /// <summary>
    /// 客户完成本任务签收的时间（UTC）；尚未签收时为空。
    /// </summary>
    public DateTime? SignedTime { get; set; }

    /// <summary>
    /// 配送调度备注，对司机可见。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 来源销售出库单。
    /// </summary>
    public virtual StockOutOrder StockOutOrder { get; set; } = null!;

    /// <summary>
    /// 来源销售订单。
    /// </summary>
    public virtual SaleOrder SaleOrder { get; set; } = null!;

    /// <summary>
    /// 收货客户档案。
    /// </summary>
    public virtual Customer Customer { get; set; } = null!;

    /// <summary>
    /// 发货仓库档案。
    /// </summary>
    public virtual Ware Ware { get; set; } = null!;

    /// <summary>
    /// 当前分配的司机。
    /// </summary>
    public virtual Driver? Driver { get; set; }

    /// <summary>
    /// 当前司机所属承运商。
    /// </summary>
    public virtual Carrier? Carrier { get; set; }

    /// <summary>
    /// 当前规划的配送路线。
    /// </summary>
    public virtual DeliveryRoute? Route { get; set; }

    /// <summary>
    /// 任务执行期间登记的配送异常集合。
    /// </summary>
    public virtual ICollection<DeliveryException> Exceptions { get; set; } = new List<DeliveryException>();

    /// <summary>
    /// 本任务产生的客户签收回单；尚未签收时为空。
    /// </summary>
    public virtual OrderReceipt? Receipt { get; set; }
}
