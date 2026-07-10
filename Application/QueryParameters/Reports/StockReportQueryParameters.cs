namespace Application.QueryParameters.Reports;

/// <summary>
/// 库存出入库报表分页查询条件，支持按业务日期、仓库、商品和关键字筛选。
/// </summary>
public class StockReportQueryParameters : PagedQueryParameters
{
    /// <summary>库存业务日期起点（UTC，包含）；入库按入库时间、出库按出库时间判断。</summary>
    public DateTime? DateStart { get; set; }

    /// <summary>库存业务日期终点（UTC，包含）；入库按入库时间、出库按出库时间判断。</summary>
    public DateTime? DateEnd { get; set; }

    /// <summary>仓库主键；为空时统计全部仓库。</summary>
    public Guid? WareId { get; set; }

    /// <summary>商品主键集合；为空时不按商品过滤。</summary>
    public List<Guid>? GoodsIds { get; set; }

    /// <summary>模糊匹配仓库、商品名称或商品编码的关键字。</summary>
    public string? Keyword { get; set; }
}
