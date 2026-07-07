using System.Linq.Expressions;
using Domain.Entities.Finance;

namespace Application.QueryParameters.Finance;

/// <summary>
/// 客户账单分页查询条件，支持按账单日期、客户、状态和待结余额筛选。
/// </summary>
public class CustomerBillQueryParameters : PagedQueryParameters
{
    /// <summary>账单日期起点（UTC，包含）。</summary>
    public DateTime? DateStart { get; set; }

    /// <summary>账单日期终点（UTC，包含）。</summary>
    public DateTime? DateEnd { get; set; }

    /// <summary>模糊匹配账单号、订单号或客户名称的关键字。</summary>
    public string? Keyword { get; set; }

    /// <summary>账单所属客户主键。</summary>
    public Guid? CustomerId { get; set; }

    /// <summary>客户账单结款状态。</summary>
    public CustomerBillStatus? BillStatus { get; set; }

    /// <summary>是否仅返回仍有未结余额的账单。</summary>
    public bool PendingOnly { get; set; }

    /// <summary>构造可由 EF Core 翻译的客户账单筛选表达式。</summary>
    public Expression<Func<CustomerBill, bool>> QueryBuild()
    {
        var keyword = Keyword?.Trim();
        return x =>
            (string.IsNullOrWhiteSpace(keyword)
             || x.BillNo.Contains(keyword)
             || x.SaleOrderNoSnapshot.Contains(keyword)
             || x.CustomerNameSnapshot.Contains(keyword))
            && (!DateStart.HasValue || x.BillDate >= DateStart.Value)
            && (!DateEnd.HasValue || x.BillDate <= DateEnd.Value)
            && (!CustomerId.HasValue || x.CustomerId == CustomerId.Value)
            && (!BillStatus.HasValue || x.BillStatus == BillStatus.Value)
            && (!PendingOnly || x.SettledAmount < x.ReceivableAmount);
    }
}
