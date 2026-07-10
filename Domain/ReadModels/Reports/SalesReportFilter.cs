namespace Domain.ReadModels.Reports;

/// <summary>
/// 销售报表筛选条件，描述订单日期、客户、标签、商品和区域范围。
/// </summary>
public sealed class SalesReportFilter
{
    /// <summary>订单日期起点（UTC，包含）。</summary>
    public DateTime? DateStart { get; init; }

    /// <summary>订单日期终点（UTC，包含）。</summary>
    public DateTime? DateEnd { get; init; }

    /// <summary>客户主键；为空时统计全部客户。</summary>
    public Guid? CustomerId { get; init; }

    /// <summary>客户标签主键集合；为空时不按标签过滤。</summary>
    public IReadOnlyCollection<Guid> CustomerTagIds { get; init; } = [];

    /// <summary>商品分类主键集合；为空时不按分类过滤。</summary>
    public IReadOnlyCollection<Guid> GoodsTypeIds { get; init; } = [];

    /// <summary>商品主键集合；为空时不按商品过滤。</summary>
    public IReadOnlyCollection<Guid> GoodsIds { get; init; } = [];

    /// <summary>模糊匹配订单号、客户和商品名称或编码的关键字。</summary>
    public string? Keyword { get; init; }

    /// <summary>按销售订单配送地址快照模糊匹配的区域关键字。</summary>
    public string? AreaKeyword { get; init; }
}
