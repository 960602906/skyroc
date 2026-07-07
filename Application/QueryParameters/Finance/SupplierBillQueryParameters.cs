using System.Linq.Expressions;
using Domain.Entities.Finance;

namespace Application.QueryParameters.Finance;

/// <summary>
/// 供应商待结单据分页查询条件，支持按单据日期、供应商、状态和待结余额筛选。
/// </summary>
public class SupplierBillQueryParameters : PagedQueryParameters
{
    /// <summary>单据日期起点（UTC，包含）。</summary>
    public DateTime? DateStart { get; set; }

    /// <summary>单据日期终点（UTC，包含）。</summary>
    public DateTime? DateEnd { get; set; }

    /// <summary>模糊匹配单据号、来源单号或供应商名称的关键字。</summary>
    public string? Keyword { get; set; }

    /// <summary>单据所属供应商主键。</summary>
    public Guid? SupplierId { get; set; }

    /// <summary>待结单据来源类型。</summary>
    public SupplierBillSourceType? SourceType { get; set; }

    /// <summary>供应商待结单据结款状态。</summary>
    public SupplierBillStatus? BillStatus { get; set; }

    /// <summary>是否仅返回仍有未结余额的单据。</summary>
    public bool PendingOnly { get; set; }

    /// <summary>构造可由 EF Core 翻译的供应商待结单据筛选表达式。</summary>
    public Expression<Func<SupplierBill, bool>> QueryBuild()
    {
        var keyword = Keyword?.Trim();
        return x =>
            (string.IsNullOrWhiteSpace(keyword)
             || x.BillNo.Contains(keyword)
             || x.SourceDocumentNoSnapshot.Contains(keyword)
             || x.SupplierNameSnapshot.Contains(keyword))
            && (!DateStart.HasValue || x.BillDate >= DateStart.Value)
            && (!DateEnd.HasValue || x.BillDate <= DateEnd.Value)
            && (!SupplierId.HasValue || x.SupplierId == SupplierId.Value)
            && (!SourceType.HasValue || x.SourceType == SourceType.Value)
            && (!BillStatus.HasValue || x.BillStatus == BillStatus.Value)
            && (!PendingOnly || x.SettledAmount < x.DocumentAmount);
    }
}
