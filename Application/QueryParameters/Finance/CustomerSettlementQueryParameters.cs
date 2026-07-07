using System.Linq.Expressions;
using Domain.Entities.Finance;

namespace Application.QueryParameters.Finance;

/// <summary>
/// 客户结款凭证分页查询条件，支持按制单日期、结款日期、客户、状态和关键字筛选。
/// </summary>
public class CustomerSettlementQueryParameters : PagedQueryParameters
{
    /// <summary>制单时间起点（UTC，包含）。</summary>
    public DateTime? CreatedStart { get; set; }

    /// <summary>制单时间终点（UTC，包含）。</summary>
    public DateTime? CreatedEnd { get; set; }

    /// <summary>结款日期起点（UTC，包含）。</summary>
    public DateTime? SettlementStart { get; set; }

    /// <summary>结款日期终点（UTC，包含）。</summary>
    public DateTime? SettlementEnd { get; set; }

    /// <summary>模糊匹配凭证号、流水号或客户名称的关键字。</summary>
    public string? Keyword { get; set; }

    /// <summary>结款客户主键。</summary>
    public Guid? CustomerId { get; set; }

    /// <summary>客户结款凭证状态。</summary>
    public CustomerSettlementStatus? SettlementStatus { get; set; }

    /// <summary>构造可由 EF Core 翻译的客户结款凭证筛选表达式。</summary>
    public Expression<Func<CustomerSettlement, bool>> QueryBuild()
    {
        var keyword = Keyword?.Trim();
        return x =>
            (string.IsNullOrWhiteSpace(keyword)
             || x.SettlementNo.Contains(keyword)
             || (x.SerialNo != null && x.SerialNo.Contains(keyword))
             || x.CustomerNameSnapshot.Contains(keyword))
            && (!CreatedStart.HasValue || x.CreateTime >= CreatedStart.Value)
            && (!CreatedEnd.HasValue || x.CreateTime <= CreatedEnd.Value)
            && (!SettlementStart.HasValue || x.SettlementDate >= SettlementStart.Value)
            && (!SettlementEnd.HasValue || x.SettlementDate <= SettlementEnd.Value)
            && (!CustomerId.HasValue || x.CustomerId == CustomerId.Value)
            && (!SettlementStatus.HasValue || x.SettlementStatus == SettlementStatus.Value);
    }
}
