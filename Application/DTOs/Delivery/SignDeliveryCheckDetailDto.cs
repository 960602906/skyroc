using Domain.Entities.Orders;

namespace Application.DTOs.Delivery;

/// <summary>
/// 配送商品验收请求，按销售出库商品行提交客户确认数量和验收结论。
/// </summary>
public class SignDeliveryCheckDetailDto
{
    /// <summary>
    /// 本次配送中的销售出库商品明细主键。
    /// </summary>
    public Guid StockOutDetailId { get; set; }

    /// <summary>
    /// 客户实际确认数量，按商品基础单位计量，不能超过本次配送数量。
    /// </summary>
    public decimal AcceptedBaseQuantity { get; set; }

    /// <summary>
    /// 客户验收结论，只允许通过或拒绝，不接受待验收状态。
    /// </summary>
    public OrderCustomerCheckStatus CheckStatus { get; set; }

    /// <summary>
    /// 当前商品行的验收差异或拒收原因。
    /// </summary>
    public string? Remark { get; set; }
}
