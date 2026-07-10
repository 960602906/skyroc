using System.Linq.Expressions;
using Domain.Entities.Traceability;
namespace Application.QueryParameters.Traceability;
/// <summary>检测报告分页查询条件，支持仓库、供应商、结论、检测时间和编号关键字筛选。</summary>
public class InspectionReportQueryParameters : PagedQueryParameters
{
    /// <summary>仓库主键。</summary>
    public Guid? WareId { get; set; }
    /// <summary>供应商主键。</summary>
    public Guid? SupplierId { get; set; }
    /// <summary>检测结论。</summary>
    public InspectionConclusion? Conclusion { get; set; }
    /// <summary>检测完成时间起点（UTC，包含）。</summary>
    public DateTime? InspectStart { get; set; }
    /// <summary>检测完成时间终点（UTC，包含）。</summary>
    public DateTime? InspectEnd { get; set; }
    /// <summary>模糊匹配报告编号、来源入库单号、机构、仓库或供应商名称。</summary>
    public string? Keyword { get; set; }
    /// <summary>构造可由 EF Core 翻译的检测报告筛选表达式。</summary>
    public Expression<Func<InspectionReport, bool>> QueryBuild()
    {
        var keyword = Keyword?.Trim();
        return x => (string.IsNullOrWhiteSpace(keyword) || x.InspectionNo.Contains(keyword)
                     || x.InNoSnapshot.Contains(keyword) || x.InspectionOrg.Contains(keyword)
                     || x.WareNameSnapshot.Contains(keyword)
                     || (x.SupplierNameSnapshot != null && x.SupplierNameSnapshot.Contains(keyword)))
                    && (!WareId.HasValue || x.WareId == WareId.Value)
                    && (!SupplierId.HasValue || x.SupplierId == SupplierId.Value)
                    && (!Conclusion.HasValue || x.Conclusion == Conclusion.Value)
                    && (!InspectStart.HasValue || x.InspectTime >= InspectStart.Value)
                    && (!InspectEnd.HasValue || x.InspectTime <= InspectEnd.Value);
    }
}
