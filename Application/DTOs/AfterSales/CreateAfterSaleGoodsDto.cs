using Domain.Entities.AfterSales;

namespace Application.DTOs.AfterSales;

/// <summary>
/// 售后商品行请求；关联订单行时商品、单位和价格由服务端按订单快照确定。
/// </summary>
public class CreateAfterSaleGoodsDto
{
    /// <summary>来源销售订单商品行主键；客户独立申请商品时为空。</summary>
    public Guid? SaleOrderDetailId { get; set; }

    /// <summary>手工商品主键；关联订单行时可省略，填写时必须与订单行一致。</summary>
    public Guid? GoodsId { get; set; }

    /// <summary>手工申请单位主键；关联订单行时可省略，填写时必须与订单行一致。</summary>
    public Guid? GoodsUnitId { get; set; }

    /// <summary>申请退款或退货数量，按申请商品单位计量且必须大于零。</summary>
    public decimal ActualRefundQuantity { get; set; }

    /// <summary>手工商品核算单价；关联订单行时忽略并使用订单价格快照。</summary>
    public decimal? UnitPrice { get; set; }

    /// <summary>售后申请类型，决定是否需要回收商品。</summary>
    public AfterSaleType AfterSaleType { get; set; }

    /// <summary>原商品供货供应商主键；未知时可为空。</summary>
    public Guid? SupplierId { get; set; }

    /// <summary>承担售后责任的部门主键；尚未定责时可为空。</summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>售后原因分类。</summary>
    public AfterSaleReasonType ReasonType { get; set; }

    /// <summary>审核通过后的退款、补货、换货或其他处理方式。</summary>
    public AfterSaleHandleType HandleType { get; set; }

    /// <summary>当前商品行的原因补充或处理说明，最长 500 字符。</summary>
    public string? Remark { get; set; }
}
