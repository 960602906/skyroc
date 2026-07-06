using System.Linq.Expressions;
using Domain.Entities.AfterSales;

namespace Application.QueryParameters.AfterSales;

/// <summary>
/// 售后单分页查询条件，支持按时间、单号、客户、状态和商品处理类型筛选。
/// </summary>
public class AfterSaleQueryParameters : PagedQueryParameters
{
    /// <summary>建单时间起点（UTC，包含）。</summary>
    public DateTime? DateStart { get; set; }

    /// <summary>建单时间终点（UTC，包含）。</summary>
    public DateTime? DateEnd { get; set; }

    /// <summary>模糊匹配售后单号、订单号或客户名称的关键字。</summary>
    public string? Keyword { get; set; }

    /// <summary>售后业务状态。</summary>
    public AfterSaleStatus? AfterStatus { get; set; }

    /// <summary>客户主键。</summary>
    public Guid? CustomerId { get; set; }

    /// <summary>来源销售订单主键。</summary>
    public Guid? SaleOrderId { get; set; }

    /// <summary>商品售后申请类型。</summary>
    public AfterSaleType? AfterSaleType { get; set; }

    /// <summary>商品售后处理方式。</summary>
    public AfterSaleHandleType? HandleType { get; set; }

    /// <summary>构造可由 EF Core 翻译的售后筛选表达式。</summary>
    public Expression<Func<AfterSale, bool>> QueryBuild()
    {
        var keyword = Keyword?.Trim();
        return x =>
            (string.IsNullOrWhiteSpace(keyword)
             || x.AfterSaleNo.Contains(keyword)
             || (x.SaleOrderNoSnapshot != null && x.SaleOrderNoSnapshot.Contains(keyword))
             || x.CustomerNameSnapshot.Contains(keyword))
            && (!DateStart.HasValue || x.CreateTime >= DateStart.Value)
            && (!DateEnd.HasValue || x.CreateTime <= DateEnd.Value)
            && (!AfterStatus.HasValue || x.AfterStatus == AfterStatus.Value)
            && (!CustomerId.HasValue || x.CustomerId == CustomerId.Value)
            && (!SaleOrderId.HasValue || x.SaleOrderId == SaleOrderId.Value)
            && (!AfterSaleType.HasValue || x.Goods.Any(item => item.AfterSaleType == AfterSaleType.Value))
            && (!HandleType.HasValue || x.Goods.Any(item => item.HandleType == HandleType.Value));
    }
}
