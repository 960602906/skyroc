using System.Linq.Expressions;
using Domain.Entities.Delivery;

namespace Application.QueryParameters;

/// <summary>
/// 配送任务分页查询参数，支持来源单号、客户、司机、承运商、路线、状态和出库时间筛选。
/// </summary>
public class DeliveryTaskQueryParameters : PagedQueryParameters
{
    /// <summary>
    /// 任务编号、出库单号、销售订单号或客户名称关键字，采用包含匹配。
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 客户主键筛选。
    /// </summary>
    public Guid? CustomerId { get; set; }

    /// <summary>
    /// 司机主键筛选。
    /// </summary>
    public Guid? DriverId { get; set; }

    /// <summary>
    /// 承运商主键筛选。
    /// </summary>
    public Guid? CarrierId { get; set; }

    /// <summary>
    /// 配送路线主键筛选。
    /// </summary>
    public Guid? RouteId { get; set; }

    /// <summary>
    /// 配送履约状态筛选。
    /// </summary>
    public DeliveryTaskStatus? DeliveryStatus { get; set; }

    /// <summary>
    /// 来源出库时间起始（含），UTC。
    /// </summary>
    public DateTime? OutTimeStart { get; set; }

    /// <summary>
    /// 来源出库时间截止（含），UTC。
    /// </summary>
    public DateTime? OutTimeEnd { get; set; }

    /// <summary>
    /// 构建配送任务组合筛选表达式。
    /// </summary>
    /// <param name="driverTasksOnly">是否仅返回已经分配司机的任务。</param>
    /// <returns>可由仓储执行的任务筛选条件。</returns>
    public Expression<Func<DeliveryTask, bool>> QueryBuild(bool driverTasksOnly = false)
    {
        var keyword = Keyword?.Trim();
        return x =>
            (!driverTasksOnly || x.DriverId.HasValue)
            && (string.IsNullOrWhiteSpace(keyword)
                || x.TaskNo.Contains(keyword)
                || x.StockOutOrder.OutNo.Contains(keyword)
                || x.SaleOrder.OrderNo.Contains(keyword)
                || x.CustomerNameSnapshot.Contains(keyword))
            && (!CustomerId.HasValue || x.CustomerId == CustomerId.Value)
            && (!DriverId.HasValue || x.DriverId == DriverId.Value)
            && (!CarrierId.HasValue || x.CarrierId == CarrierId.Value)
            && (!RouteId.HasValue || x.RouteId == RouteId.Value)
            && (!DeliveryStatus.HasValue || x.DeliveryStatus == DeliveryStatus.Value)
            && (!OutTimeStart.HasValue || x.OutTime >= OutTimeStart.Value)
            && (!OutTimeEnd.HasValue || x.OutTime <= OutTimeEnd.Value);
    }
}
