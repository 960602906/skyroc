using Domain.Entities.Finance;
using Shared.Constants;

namespace Application.DTOs.Finance;

/// <summary>
/// 供应商待结单据响应，展示采购入库或退货形成的应付、已结金额和剩余待结金额。
/// </summary>
public class SupplierBillDto : BaseDto
{
    /// <summary>供应商待结单据业务唯一编号。</summary>
    public string BillNo { get; set; } = string.Empty;

    /// <summary>单据所属供应商主键。</summary>
    public Guid SupplierId { get; set; }

    /// <summary>单据生成时的供应商名称快照。</summary>
    public string SupplierName { get; set; } = string.Empty;

    /// <summary>待结单据来源类型。</summary>
    public SupplierBillSourceType SourceType { get; set; }

    /// <summary>来源采购入库单主键；采购退货来源时为空。</summary>
    public Guid? StockInOrderId { get; set; }

    /// <summary>来源采购退货出库单主键；采购入库来源时为空。</summary>
    public Guid? StockOutOrderId { get; set; }

    /// <summary>来源出入库单业务编号快照。</summary>
    public string SourceDocumentNo { get; set; } = string.Empty;

    /// <summary>单据业务日期（UTC）。</summary>
    public DateTime BillDate { get; set; }

    /// <summary>来源单据绝对金额合计。</summary>
    public decimal DocumentAmount { get; set; }

    /// <summary>当前待结绝对金额，采购入库为正、采购退货为负。</summary>
    public decimal PayableAmount { get; set; }

    /// <summary>已结款绝对金额。</summary>
    public decimal SettledAmount { get; set; }

    /// <summary>剩余待结绝对金额，按全局金额精度计算。</summary>
    public decimal PendingAmount => NumericPrecision.RoundMoney(Math.Max(0m, DocumentAmount - SettledAmount));

    /// <summary>供应商待结单据结款状态。</summary>
    public SupplierBillStatus BillStatus { get; set; }

    /// <summary>待结单据备注。</summary>
    public string? Remark { get; set; }
}
