using Domain.Entities.AfterSales;
using Shared.Constants;

namespace Application.DTOs.AfterSales;

/// <summary>
/// 售后单响应，包含订单客户快照、处理状态、商品行和完整审核轨迹。
/// </summary>
public class AfterSaleDto : BaseDto
{
    /// <summary>售后单业务唯一编号。</summary>
    public string AfterSaleNo { get; set; } = string.Empty;

    /// <summary>来源销售订单主键；客户独立申请时为空。</summary>
    public Guid? SaleOrderId { get; set; }

    /// <summary>来源销售订单编号快照。</summary>
    public string? SaleOrderNo { get; set; }

    /// <summary>发起售后的客户主键。</summary>
    public Guid CustomerId { get; set; }

    /// <summary>售后建单时的客户名称快照。</summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>售后来源稳定标识，例如后台建单。</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>当前售后业务状态。</summary>
    public AfterSaleStatus AfterStatus { get; set; }

    /// <summary>建单时原订单结算金额，按系统业务币种计量。</summary>
    public decimal OrderPrice { get; set; }

    /// <summary>扣除本单退款或减免后的客户结算金额。</summary>
    public decimal SettlementPrice { get; set; }

    /// <summary>全部商品行退款或减免金额合计。</summary>
    public decimal TotalRefundAmount => NumericPrecision.RoundMoney(Goods.Sum(x => x.RefundAmount));

    /// <summary>售后联系人姓名快照。</summary>
    public string? ContactName { get; set; }

    /// <summary>售后联系人电话快照。</summary>
    public string? ContactPhone { get; set; }

    /// <summary>需要回收实物时使用的取货地址快照。</summary>
    public string? PickupAddress { get; set; }

    /// <summary>对全部商品行生效的售后备注。</summary>
    public string? Remark { get; set; }

    /// <summary>售后商品行，按商品编码和主键稳定排序。</summary>
    public List<AfterSaleGoodsDto> Goods { get; set; } = [];

    /// <summary>提交、审核、驳回、重提和反审核轨迹。</summary>
    public List<AfterSaleAuditLogDto> AuditLogs { get; set; } = [];
}
