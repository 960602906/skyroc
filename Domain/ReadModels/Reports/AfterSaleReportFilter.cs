using Domain.Entities.AfterSales;

namespace Domain.ReadModels.Reports;

/// <summary>
/// 售后报表筛选条件，描述建单日期、客户、商品和售后原因处理范围。
/// </summary>
public sealed class AfterSaleReportFilter
{
    /// <summary>售后建单时间起点（UTC，包含）。</summary>
    public DateTime? DateStart { get; init; }

    /// <summary>售后建单时间终点（UTC，包含）。</summary>
    public DateTime? DateEnd { get; init; }

    /// <summary>客户主键；为空时统计全部客户。</summary>
    public Guid? CustomerId { get; init; }

    /// <summary>商品主键集合；为空时不按商品过滤。</summary>
    public IReadOnlyCollection<Guid> GoodsIds { get; init; } = [];

    /// <summary>售后原因；为空时统计全部原因。</summary>
    public AfterSaleReasonType? ReasonType { get; init; }

    /// <summary>售后申请类型；为空时统计全部类型。</summary>
    public AfterSaleType? AfterSaleType { get; init; }

    /// <summary>售后处理方式；为空时统计全部处理方式。</summary>
    public AfterSaleHandleType? HandleType { get; init; }

    /// <summary>模糊匹配售后单号、客户和商品名称或编码的关键字。</summary>
    public string? Keyword { get; init; }
}
