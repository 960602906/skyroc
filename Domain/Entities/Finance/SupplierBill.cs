using Domain.Entities.Purchases;
using Domain.Entities.Storage;

namespace Domain.Entities.Finance;

/// <summary>
/// 供应商待结单据主表，按采购入库或采购退货出库汇总应付金额和后续结算余额。
/// </summary>
public class SupplierBill : BaseEntity
{
    /// <summary>
    /// 供应商待结单据业务唯一编号。
    /// </summary>
    public string BillNo { get; set; } = string.Empty;

    /// <summary>
    /// 单据所属供应商主键。
    /// </summary>
    public Guid SupplierId { get; set; }

    /// <summary>
    /// 单据生成时的供应商名称快照。
    /// </summary>
    public string SupplierNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 待结单据来源类型，决定应付金额方向。
    /// </summary>
    public SupplierBillSourceType SourceType { get; set; }

    /// <summary>
    /// 来源采购入库单主键；仅采购入库来源时填写。
    /// </summary>
    public Guid? StockInOrderId { get; set; }

    /// <summary>
    /// 来源采购退货出库单主键；仅采购退货来源时填写。
    /// </summary>
    public Guid? StockOutOrderId { get; set; }

    /// <summary>
    /// 来源出入库单业务编号快照。
    /// </summary>
    public string SourceDocumentNoSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 单据生成或最近一次同步的业务日期（UTC）。
    /// </summary>
    public DateTime BillDate { get; set; }

    /// <summary>
    /// 来源单据绝对金额合计，按系统业务币种计量且始终为非负数。
    /// </summary>
    public decimal DocumentAmount { get; set; }

    /// <summary>
    /// 当前待结绝对金额，采购入库为正、采购退货为负，按系统业务币种计量。
    /// </summary>
    public decimal PayableAmount { get; set; }

    /// <summary>
    /// 已结款绝对金额，后续供应商结算流程按此字段回写。
    /// </summary>
    public decimal SettledAmount { get; set; }

    /// <summary>
    /// 供应商待结单据状态，初始为待结款。
    /// </summary>
    public SupplierBillStatus BillStatus { get; set; } = SupplierBillStatus.Pending;

    /// <summary>
    /// 待结单据备注，记录人工调整说明或同步异常说明。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 单据所属供应商档案。
    /// </summary>
    public virtual Supplier Supplier { get; set; } = null!;

    /// <summary>
    /// 来源采购入库单。
    /// </summary>
    public virtual StockInOrder? StockInOrder { get; set; }

    /// <summary>
    /// 来源采购退货出库单。
    /// </summary>
    public virtual StockOutOrder? StockOutOrder { get; set; }

    /// <summary>
    /// 待结单据明细集合，包含入库或退货商品行。
    /// </summary>
    public virtual ICollection<SupplierBillDetail> Details { get; set; } = new List<SupplierBillDetail>();
}
