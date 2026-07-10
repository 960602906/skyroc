using Domain.Entities.Purchases;

namespace Domain.ReadModels.Reports;

/// <summary>
/// 采购出入库报表仓储筛选条件。
/// </summary>
public sealed class PurchaseInOutReportFilter
{
    /// <summary>采购库存业务日期起点（UTC，包含）。</summary>
    public DateTime? DateStart { get; init; }

    /// <summary>采购库存业务日期终点（UTC，包含）。</summary>
    public DateTime? DateEnd { get; init; }

    /// <summary>仓库主键；为空时统计全部仓库。</summary>
    public Guid? WareId { get; init; }

    /// <summary>供应商主键；为空时统计全部供应商。</summary>
    public Guid? SupplierId { get; init; }

    /// <summary>采购员主键；为空时统计全部采购员。</summary>
    public Guid? PurchaserId { get; init; }

    /// <summary>采购模式；为空时统计全部采购模式。</summary>
    public PurchasePattern? PurchasePattern { get; init; }

    /// <summary>商品主键集合；为空时不按商品过滤。</summary>
    public IReadOnlyCollection<Guid> GoodsIds { get; init; } = [];

    /// <summary>模糊匹配单号、供应商、采购员、商品名称或商品编码的关键字。</summary>
    public string? Keyword { get; init; }
}
