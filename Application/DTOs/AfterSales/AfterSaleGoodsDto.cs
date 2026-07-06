using Domain.Entities.AfterSales;

namespace Application.DTOs.AfterSales;

/// <summary>
/// 售后商品响应，展示来源订单行、数量金额快照及后续处理方式。
/// </summary>
public class AfterSaleGoodsDto : BaseDto
{
    /// <summary>来源销售订单商品行主键；客户独立申请时为空。</summary>
    public Guid? SaleOrderDetailId { get; set; }

    /// <summary>售后商品主键。</summary>
    public Guid GoodsId { get; set; }

    /// <summary>建单时的商品名称快照。</summary>
    public string GoodsName { get; set; } = string.Empty;

    /// <summary>建单时的商品编码快照。</summary>
    public string GoodsCode { get; set; } = string.Empty;

    /// <summary>建单时的商品分类名称快照。</summary>
    public string? GoodsTypeName { get; set; }

    /// <summary>申请退款或退货使用的商品单位主键。</summary>
    public Guid GoodsUnitId { get; set; }

    /// <summary>申请商品单位名称快照。</summary>
    public string GoodsUnitName { get; set; } = string.Empty;

    /// <summary>商品基础单位主键。</summary>
    public Guid? BaseUnitId { get; set; }

    /// <summary>商品基础单位名称快照。</summary>
    public string? BaseUnitName { get; set; }

    /// <summary>申请单位换算为基础单位的比例。</summary>
    public decimal ConversionRate { get; set; }

    /// <summary>售后申请类型，区分仅退款与退货退款。</summary>
    public AfterSaleType AfterSaleType { get; set; }

    /// <summary>批准退款或退货数量，按申请商品单位计量。</summary>
    public decimal ActualRefundQuantity { get; set; }

    /// <summary>批准数量换算到基础单位后的数量。</summary>
    public decimal BaseRefundQuantity { get; set; }

    /// <summary>售后核算单价，按申请商品单位及系统业务币种计量。</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>当前商品行退款或减免金额，按系统业务币种计量。</summary>
    public decimal RefundAmount { get; set; }

    /// <summary>原商品供货供应商主键。</summary>
    public Guid? SupplierId { get; set; }

    /// <summary>供应商名称快照。</summary>
    public string? SupplierName { get; set; }

    /// <summary>承担售后责任的部门主键。</summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>责任部门名称快照。</summary>
    public string? DepartmentName { get; set; }

    /// <summary>售后原因分类。</summary>
    public AfterSaleReasonType ReasonType { get; set; }

    /// <summary>审核通过后采用的业务处理方式。</summary>
    public AfterSaleHandleType HandleType { get; set; }

    /// <summary>当前商品行的原因补充或处理说明。</summary>
    public string? Remark { get; set; }
}
