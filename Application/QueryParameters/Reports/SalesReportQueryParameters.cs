namespace Application.QueryParameters.Reports;

/// <summary>
/// 销售报表分页查询条件，支持日期、客户、标签、商品、分类和区域筛选。
/// </summary>
public class SalesReportQueryParameters : PagedQueryParameters
{
    /// <summary>订单日期起点（UTC，包含）。</summary>
    public DateTime? DateStart { get; set; }

    /// <summary>订单日期终点（UTC，包含）。</summary>
    public DateTime? DateEnd { get; set; }

    /// <summary>客户主键；为空时统计全部客户。</summary>
    public Guid? CustomerId { get; set; }

    /// <summary>客户标签主键集合；为空时不按标签过滤。</summary>
    public List<Guid>? CustomerTagIds { get; set; }

    /// <summary>商品分类主键集合；按所选分类当前名称匹配订单明细分类名称快照，与分类汇总展示口径一致。</summary>
    public List<Guid>? GoodsTypeIds { get; set; }

    /// <summary>商品主键集合；为空时不按商品过滤。</summary>
    public List<Guid>? GoodsIds { get; set; }

    /// <summary>模糊匹配订单号、客户和商品名称或编码的关键字。</summary>
    public string? Keyword { get; set; }

    /// <summary>按销售订单配送地址快照模糊匹配的区域关键字。</summary>
    public string? AreaKeyword { get; set; }
}
