namespace Domain.ReadModels.Reports;

/// <summary>
/// 库存出入库报表仓储筛选条件。
/// </summary>
public sealed class StockReportFilter
{
    /// <summary>库存业务日期起点（UTC，包含）。</summary>
    public DateTime? DateStart { get; init; }

    /// <summary>库存业务日期终点（UTC，包含）。</summary>
    public DateTime? DateEnd { get; init; }

    /// <summary>仓库主键；为空时统计全部仓库。</summary>
    public Guid? WareId { get; init; }

    /// <summary>商品主键集合；为空时不按商品过滤。</summary>
    public IReadOnlyCollection<Guid> GoodsIds { get; init; } = [];

    /// <summary>模糊匹配仓库、商品名称或商品编码的关键字。</summary>
    public string? Keyword { get; init; }
}
