using Domain.Entities.AfterSales;

namespace Domain.ReadModels.Reports;

/// <summary>
/// 按售后类型、原因和处理方式汇总的售后报表读模型。
/// </summary>
public sealed class AfterSaleSummaryReadModel
{
    /// <summary>售后申请类型。</summary>
    public AfterSaleType AfterSaleType { get; init; }

    /// <summary>售后原因分类。</summary>
    public AfterSaleReasonType ReasonType { get; init; }

    /// <summary>售后处理方式。</summary>
    public AfterSaleHandleType HandleType { get; init; }

    /// <summary>退款或减免的基础单位数量；补货、换货、客户沟通计为 0。</summary>
    public decimal RefundBaseQuantity { get; init; }

    /// <summary>退款或减免金额；补货、换货、客户沟通为 0。</summary>
    public decimal RefundAmount { get; init; }

    /// <summary>参与汇总的售后单数。</summary>
    public int AfterSaleCount { get; init; }

    /// <summary>参与汇总的客户数。</summary>
    public int CustomerCount { get; init; }
}
