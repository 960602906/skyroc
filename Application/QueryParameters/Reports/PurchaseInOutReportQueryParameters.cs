using Domain.Entities.Purchases;

namespace Application.QueryParameters.Reports;

/// <summary>
/// 采购出入库报表分页查询条件，按采购入库与采购退货出库统计采购履约流入流出。
/// </summary>
public class PurchaseInOutReportQueryParameters : PagedQueryParameters
{
    /// <summary>采购库存业务日期起点（UTC，包含）；采购入库按入库时间、采购退货按出库时间判断。</summary>
    public DateTime? DateStart { get; set; }

    /// <summary>采购库存业务日期终点（UTC，包含）；采购入库按入库时间、采购退货按出库时间判断。</summary>
    public DateTime? DateEnd { get; set; }

    /// <summary>仓库主键；为空时统计全部仓库。</summary>
    public Guid? WareId { get; set; }

    /// <summary>供应商主键；为空时统计全部供应商或未指定供应商的采购业务。</summary>
    public Guid? SupplierId { get; set; }

    /// <summary>采购员主键；为空时统计全部采购员或未指定采购员的采购业务。</summary>
    public Guid? PurchaserId { get; set; }

    /// <summary>采购模式；为空时统计供应商直供和市场自采。</summary>
    public PurchasePattern? PurchasePattern { get; set; }

    /// <summary>商品主键集合；为空时不按商品过滤。</summary>
    public List<Guid>? GoodsIds { get; set; }

    /// <summary>模糊匹配单号、供应商、采购员、商品名称或商品编码的关键字。</summary>
    public string? Keyword { get; set; }
}
