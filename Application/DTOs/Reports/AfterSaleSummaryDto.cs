using Domain.Entities.AfterSales;

namespace Application.DTOs.Reports;

/// <summary>
/// 售后汇总响应，按申请类型、原因和处理方式统计已完成售后商品。
/// </summary>
public class AfterSaleSummaryDto
{
    /// <summary>售后申请类型。</summary>
    public AfterSaleType AfterSaleType { get; set; }

    /// <summary>售后原因分类。</summary>
    public AfterSaleReasonType ReasonType { get; set; }

    /// <summary>售后处理方式。</summary>
    public AfterSaleHandleType HandleType { get; set; }

    /// <summary>退款或减免的基础单位数量；补货、换货、客户沟通计为 0。</summary>
    public decimal RefundBaseQuantity { get; set; }

    /// <summary>退款或减免金额；补货、换货、客户沟通为 0。</summary>
    public decimal RefundAmount { get; set; }

    /// <summary>参与汇总的售后单数。</summary>
    public int AfterSaleCount { get; set; }

    /// <summary>参与汇总的客户数。</summary>
    public int CustomerCount { get; set; }
}
