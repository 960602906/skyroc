using Domain.Entities.Delivery;
namespace Application.DTOs.Delivery;

/// <summary>
/// 配送任务响应，返回来源销售出库、客户、司机、承运商、路线和当前履约状态快照。
/// </summary>
public class DeliveryTaskDto : BaseDto
{
    /// <summary>
    /// 配送任务业务编号。
    /// </summary>
    public string TaskNo { get; set; } = string.Empty;

    /// <summary>
    /// 来源销售出库单主键。
    /// </summary>
    public Guid StockOutOrderId { get; set; }

    /// <summary>
    /// 来源销售出库单编号。
    /// </summary>
    public string StockOutOrderNo { get; set; } = string.Empty;

    /// <summary>
    /// 来源销售订单主键。
    /// </summary>
    public Guid SaleOrderId { get; set; }

    /// <summary>
    /// 来源销售订单编号。
    /// </summary>
    public string SaleOrderNo { get; set; } = string.Empty;

    /// <summary>
    /// 收货客户主键。
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// 任务生成时的客户名称快照。
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// 收货联系人姓名快照。
    /// </summary>
    public string? ContactName { get; set; }

    /// <summary>
    /// 收货联系人电话快照。
    /// </summary>
    public string? ContactPhone { get; set; }

    /// <summary>
    /// 收货地址快照。
    /// </summary>
    public string? DeliveryAddress { get; set; }

    /// <summary>
    /// 发货仓库主键。
    /// </summary>
    public Guid WareId { get; set; }

    /// <summary>
    /// 发货仓库名称快照。
    /// </summary>
    public string WareName { get; set; } = string.Empty;

    /// <summary>
    /// 当前分配司机主键；待分配时为空。
    /// </summary>
    public Guid? DriverId { get; set; }

    /// <summary>
    /// 当前分配司机姓名快照。
    /// </summary>
    public string? DriverName { get; set; }

    /// <summary>
    /// 当前分配司机电话快照。
    /// </summary>
    public string? DriverPhone { get; set; }

    /// <summary>
    /// 当前承运商主键；自营司机为空。
    /// </summary>
    public Guid? CarrierId { get; set; }

    /// <summary>
    /// 当前承运商名称快照。
    /// </summary>
    public string? CarrierName { get; set; }

    /// <summary>
    /// 当前规划路线主键；尚未规划时为空。
    /// </summary>
    public Guid? RouteId { get; set; }

    /// <summary>
    /// 当前规划路线名称快照。
    /// </summary>
    public string? RouteName { get; set; }

    /// <summary>
    /// 客户在当前路线内的配送顺序。
    /// </summary>
    public int? RouteSequence { get; set; }

    /// <summary>
    /// 当前配送履约状态。
    /// </summary>
    public DeliveryTaskStatus DeliveryStatus { get; set; }

    /// <summary>
    /// 来源销售出库时间（UTC）。
    /// </summary>
    public DateTime OutTime { get; set; }

    /// <summary>
    /// 最近一次分配司机时间（UTC）。
    /// </summary>
    public DateTime? AssignedTime { get; set; }

    /// <summary>
    /// 最近一次路线规划时间（UTC）。
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
    /// 配送调度备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 本任务产生的客户签收回单；尚未签收时为空。
    /// </summary>
    public OrderReceiptDto? Receipt { get; set; }
}
