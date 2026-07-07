using Domain.Entities.Finance;
using Shared.Constants;

namespace Application.DTOs.Finance;

/// <summary>
/// 客户账单响应，展示订单签收应收、售后冲减、已结金额和剩余待结金额。
/// </summary>
public class CustomerBillDto : BaseDto
{
    /// <summary>客户账单业务唯一编号。</summary>
    public string BillNo { get; set; } = string.Empty;

    /// <summary>账单所属客户主键。</summary>
    public Guid CustomerId { get; set; }

    /// <summary>账单生成时的客户名称快照。</summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>来源销售订单主键。</summary>
    public Guid SaleOrderId { get; set; }

    /// <summary>来源销售订单业务编号快照。</summary>
    public string SaleOrderNo { get; set; } = string.Empty;

    /// <summary>账单业务日期（UTC）。</summary>
    public DateTime BillDate { get; set; }

    /// <summary>订单签收形成的正向应收金额。</summary>
    public decimal OrderAmount { get; set; }

    /// <summary>售后完成形成的应收调整金额，负数表示冲减。</summary>
    public decimal AfterSaleAdjustmentAmount { get; set; }

    /// <summary>当前账单应收金额。</summary>
    public decimal ReceivableAmount { get; set; }

    /// <summary>已结款金额。</summary>
    public decimal SettledAmount { get; set; }

    /// <summary>剩余待结金额，按全局金额精度计算。</summary>
    public decimal PendingAmount => NumericPrecision.RoundMoney(Math.Max(0m, ReceivableAmount - SettledAmount));

    /// <summary>客户账单结款状态。</summary>
    public CustomerBillStatus BillStatus { get; set; }

    /// <summary>账单备注。</summary>
    public string? Remark { get; set; }
}
