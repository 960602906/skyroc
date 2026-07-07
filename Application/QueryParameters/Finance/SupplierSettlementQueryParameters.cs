using System.Linq.Expressions;
using Domain.Entities.Finance;

namespace Application.QueryParameters.Finance;

/// <summary>
/// 供应商结算单分页查询条件，支持按制单日期、结款日期、供应商、状态和关键字筛选。
/// </summary>
public class SupplierSettlementQueryParameters : PagedQueryParameters
{
    /// <summary>制单时间起点（UTC，包含）。</summary>
    public DateTime? CreatedStart { get; set; }

    /// <summary>制单时间终点（UTC，包含）。</summary>
    public DateTime? CreatedEnd { get; set; }

    /// <summary>结款日期起点（UTC，包含）。</summary>
    public DateTime? SettlementStart { get; set; }

    /// <summary>结款日期终点（UTC，包含）。</summary>
    public DateTime? SettlementEnd { get; set; }

    /// <summary>模糊匹配结算单号、流水号或供应商名称的关键字。</summary>
    public string? Keyword { get; set; }

    /// <summary>结算供应商主键。</summary>
    public Guid? SupplierId { get; set; }

    /// <summary>供应商结算单状态。</summary>
    public SupplierSettlementStatus? SettlementStatus { get; set; }

    /// <summary>构造可由 EF Core 翻译的供应商结算单筛选表达式。</summary>
    public Expression<Func<SupplierSettlement, bool>> QueryBuild()
    {
        var keyword = Keyword?.Trim();
        return x =>
            (string.IsNullOrWhiteSpace(keyword)
             || x.SettlementNo.Contains(keyword)
             || (x.SerialNo != null && x.SerialNo.Contains(keyword))
             || x.SupplierNameSnapshot.Contains(keyword))
            && (!CreatedStart.HasValue || x.CreateTime >= CreatedStart.Value)
            && (!CreatedEnd.HasValue || x.CreateTime <= CreatedEnd.Value)
            && (!SettlementStart.HasValue || x.SettlementDate >= SettlementStart.Value)
            && (!SettlementEnd.HasValue || x.SettlementDate <= SettlementEnd.Value)
            && (!SupplierId.HasValue || x.SupplierId == SupplierId.Value)
            && (!SettlementStatus.HasValue || x.SettlementStatus == SettlementStatus.Value);
    }
}
