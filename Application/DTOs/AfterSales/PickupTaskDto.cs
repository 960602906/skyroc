using Application.Serialization;
using Domain.Entities.AfterSales;
using System.Text.Json.Serialization;

namespace Application.DTOs.AfterSales;

/// <summary>
/// 售后取货任务响应，包含售后来源、退货商品、司机、执行时间和退货入库追溯信息。
/// </summary>
public class PickupTaskDto : BaseDto
{
    /// <summary>取货任务业务唯一编号。</summary>
    public string TaskNo { get; set; } = string.Empty;

    /// <summary>所属售后单主键。</summary>
    public Guid AfterSaleId { get; set; }

    /// <summary>所属售后单业务编号。</summary>
    public string AfterSaleNo { get; set; } = string.Empty;

    /// <summary>发起退货的客户主键。</summary>
    public Guid CustomerId { get; set; }

    /// <summary>售后建单时的客户名称快照。</summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>需要回收商品的售后商品明细主键。</summary>
    public Guid AfterSaleGoodsId { get; set; }

    /// <summary>需要回收的商品主键。</summary>
    public Guid GoodsId { get; set; }

    /// <summary>售后建单时的商品名称快照。</summary>
    public string GoodsName { get; set; } = string.Empty;

    /// <summary>批准退货数量，按售后申请单位计量。</summary>
    public decimal Quantity { get; set; }

    /// <summary>售后申请使用的计量单位名称快照。</summary>
    public string GoodsUnitName { get; set; } = string.Empty;

    /// <summary>当前分配的司机主键；待分配时为空。</summary>
    public Guid? DriverId { get; set; }

    /// <summary>最近一次分配时的司机姓名快照。</summary>
    public string? DriverName { get; set; }

    /// <summary>最近一次分配时的司机电话快照。</summary>
    public string? DriverPhone { get; set; }

    /// <summary>取货联系人姓名快照。</summary>
    public string? ContactName { get; set; }

    /// <summary>取货联系人电话快照。</summary>
    public string? ContactPhone { get; set; }

    /// <summary>司机执行任务使用的取货地址快照。</summary>
    public string PickupAddress { get; set; } = string.Empty;

    /// <summary>当前取货履约状态。</summary>
    public PickupTaskStatus PickupStatus { get; set; }

    /// <summary>计划上门取货时间（UTC）。</summary>
    [JsonConverter(typeof(FixedNullableDateTimeJsonConverter))]
    public DateTime? PlannedPickupTime { get; set; }

    /// <summary>最近一次分配司机时间（UTC）。</summary>
    [JsonConverter(typeof(FixedNullableDateTimeJsonConverter))]
    public DateTime? AssignedTime { get; set; }

    /// <summary>司机开始执行取货时间（UTC）。</summary>
    [JsonConverter(typeof(FixedNullableDateTimeJsonConverter))]
    public DateTime? StartedTime { get; set; }

    /// <summary>退货商品取回完成时间（UTC）。</summary>
    [JsonConverter(typeof(FixedNullableDateTimeJsonConverter))]
    public DateTime? CompletedTime { get; set; }

    /// <summary>由当前任务生成的销售退货入库单主键；尚未衔接时为空。</summary>
    public Guid? StockInOrderId { get; set; }

    /// <summary>取货调度或执行备注。</summary>
    public string? Remark { get; set; }
}
