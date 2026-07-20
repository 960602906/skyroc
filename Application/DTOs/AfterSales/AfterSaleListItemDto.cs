using System.Text.Json.Serialization;
using Application.Serialization;
using Domain.Entities.AfterSales;
using Shared.Constants;

namespace Application.DTOs.AfterSales;

/// <summary>
/// 售后分页列表项，仅包含列表展示与操作状态判断需要的数据。
/// </summary>
public class AfterSaleListItemDto
{
    /// <summary>
    /// 售后单主键。
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 售后单创建时间（UTC）。
    /// </summary>
    [JsonConverter(typeof(FixedNullableDateTimeJsonConverter))]
    public DateTime? CreateTime { get; set; }

    /// <summary>
    /// 售后单业务唯一编号。
    /// </summary>
    public string AfterSaleNo { get; set; } = string.Empty;

    /// <summary>
    /// 来源销售订单主键；无来源订单时为空。
    /// </summary>
    public Guid? SaleOrderId { get; set; }

    /// <summary>
    /// 建单时的来源销售订单编号快照。
    /// </summary>
    public string? SaleOrderNo { get; set; }

    /// <summary>
    /// 售后建单时的客户名称快照。
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// 售后建单时的原订单金额，按系统业务币种计量。
    /// </summary>
    public decimal OrderPrice { get; set; }

    /// <summary>
    /// 售后处理后的客户结算金额，按系统业务币种计量。
    /// </summary>
    public decimal SettlementPrice { get; set; }

    /// <summary>
    /// 全部商品行退款或减免金额合计，按系统金额精度舍入。
    /// </summary>
    public decimal TotalRefundAmount => NumericPrecision.RoundMoney(Goods.Sum(x => x.RefundAmount));

    /// <summary>
    /// 当前售后业务状态。
    /// </summary>
    public AfterSaleStatus AfterStatus { get; set; }

    /// <summary>
    /// 售后联系人姓名快照。
    /// </summary>
    public string? ContactName { get; set; }

    /// <summary>
    /// 售后联系人电话快照。
    /// </summary>
    public string? ContactPhone { get; set; }

    /// <summary>
    /// 列表展示需要的商品申请类型、处理方式和退款金额摘要。
    /// </summary>
    public List<AfterSaleListGoodsDto> Goods { get; set; } = [];

    /// <summary>
    /// 最新一次售后审核动作；从未提交或审核时为空。
    /// </summary>
    public AfterSaleAuditAction? LatestAuditAction { get; set; }

    /// <summary>
    /// 是否已经生成取货任务，用于判断当前售后是否允许反审核。
    /// </summary>
    public bool HasPickupTasks { get; set; }
}
