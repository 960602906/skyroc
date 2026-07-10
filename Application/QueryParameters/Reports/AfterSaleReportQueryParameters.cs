using Domain.Entities.AfterSales;

namespace Application.QueryParameters.Reports;

/// <summary>
/// 售后报表分页查询条件，支持日期、客户、商品、原因、类型和处理方式筛选。
/// </summary>
public class AfterSaleReportQueryParameters : PagedQueryParameters
{
    /// <summary>售后建单时间起点（UTC，包含）。</summary>
    public DateTime? DateStart { get; set; }

    /// <summary>售后建单时间终点（UTC，包含）。</summary>
    public DateTime? DateEnd { get; set; }

    /// <summary>客户主键；为空时统计全部客户。</summary>
    public Guid? CustomerId { get; set; }

    /// <summary>商品主键集合；为空时不按商品过滤。</summary>
    public List<Guid>? GoodsIds { get; set; }

    /// <summary>售后原因；为空时统计全部原因。</summary>
    public AfterSaleReasonType? ReasonType { get; set; }

    /// <summary>售后申请类型；为空时统计全部类型。</summary>
    public AfterSaleType? AfterSaleType { get; set; }

    /// <summary>售后处理方式；为空时统计全部处理方式。</summary>
    public AfterSaleHandleType? HandleType { get; set; }

    /// <summary>模糊匹配售后单号、客户和商品名称或编码的关键字。</summary>
    public string? Keyword { get; set; }
}
