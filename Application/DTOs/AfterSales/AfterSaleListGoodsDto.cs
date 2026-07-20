using Domain.Entities.AfterSales;

namespace Application.DTOs.AfterSales;

/// <summary>
/// 售后分页列表中的商品处理摘要。
/// </summary>
public class AfterSaleListGoodsDto
{
    /// <summary>
    /// 商品售后申请类型，区分仅退款和退货退款。
    /// </summary>
    public AfterSaleType AfterSaleType { get; set; }

    /// <summary>
    /// 商品售后处理方式，用于列表展示本单处理方向。
    /// </summary>
    public AfterSaleHandleType HandleType { get; set; }

    /// <summary>
    /// 当前商品行的退款或减免金额，按系统业务币种计量。
    /// </summary>
    public decimal RefundAmount { get; set; }
}
