using System.Linq.Expressions;
using Domain.Entities.AfterSales;

namespace Application.QueryParameters.AfterSales;

/// <summary>
/// 售后取货任务分页条件，支持按售后、客户、司机、状态和计划时间筛选。
/// </summary>
public class PickupTaskQueryParameters : PagedQueryParameters
{
    /// <summary>模糊匹配任务号、售后单号、客户名称或商品名称的关键字。</summary>
    public string? Keyword { get; set; }

    /// <summary>所属售后单主键。</summary>
    public Guid? AfterSaleId { get; set; }

    /// <summary>当前分配司机主键。</summary>
    public Guid? DriverId { get; set; }

    /// <summary>取货任务状态。</summary>
    public PickupTaskStatus? PickupStatus { get; set; }

    /// <summary>计划上门时间起点（UTC，包含）。</summary>
    public DateTime? PlannedStart { get; set; }

    /// <summary>计划上门时间终点（UTC，包含）。</summary>
    public DateTime? PlannedEnd { get; set; }

    /// <summary>构造可由 EF Core 翻译的取货任务筛选表达式。</summary>
    public Expression<Func<PickupTask, bool>> QueryBuild()
    {
        var keyword = Keyword?.Trim();
        return x =>
            (string.IsNullOrWhiteSpace(keyword)
             || x.TaskNo.Contains(keyword)
             || x.AfterSale.AfterSaleNo.Contains(keyword)
             || x.AfterSale.CustomerNameSnapshot.Contains(keyword)
             || x.AfterSaleGoods.GoodsNameSnapshot.Contains(keyword))
            && (!AfterSaleId.HasValue || x.AfterSaleId == AfterSaleId.Value)
            && (!DriverId.HasValue || x.DriverId == DriverId.Value)
            && (!PickupStatus.HasValue || x.PickupStatus == PickupStatus.Value)
            && (!PlannedStart.HasValue || x.PlannedPickupTime >= PlannedStart.Value)
            && (!PlannedEnd.HasValue || x.PlannedPickupTime <= PlannedEnd.Value);
    }
}
