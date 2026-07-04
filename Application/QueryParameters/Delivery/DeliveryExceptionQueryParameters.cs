using System.Linq.Expressions;
using Domain.Entities.Delivery;

namespace Application.QueryParameters;

/// <summary>
/// 配送异常分页查询参数，支持任务、司机、客户、处理状态和登记时间筛选。
/// </summary>
public class DeliveryExceptionQueryParameters : PagedQueryParameters
{
    /// <summary>
    /// 异常编号、任务编号或异常描述关键字，采用包含匹配。
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 配送任务主键筛选。
    /// </summary>
    public Guid? DeliveryTaskId { get; set; }

    /// <summary>
    /// 司机主键筛选。
    /// </summary>
    public Guid? DriverId { get; set; }

    /// <summary>
    /// 客户主键筛选。
    /// </summary>
    public Guid? CustomerId { get; set; }

    /// <summary>
    /// 异常处理状态筛选。
    /// </summary>
    public DeliveryExceptionStatus? HandleStatus { get; set; }

    /// <summary>
    /// 异常登记时间起始（含），UTC。
    /// </summary>
    public DateTime? CreateTimeStart { get; set; }

    /// <summary>
    /// 异常登记时间截止（含），UTC。
    /// </summary>
    public DateTime? CreateTimeEnd { get; set; }

    /// <summary>
    /// 构建配送异常组合筛选表达式。
    /// </summary>
    /// <returns>可由仓储执行的异常筛选条件。</returns>
    public Expression<Func<DeliveryException, bool>> QueryBuild()
    {
        var keyword = Keyword?.Trim();
        return x =>
            (string.IsNullOrWhiteSpace(keyword)
                || x.ExceptionNo.Contains(keyword)
                || x.Description.Contains(keyword)
                || (x.DeliveryTask != null && x.DeliveryTask.TaskNo.Contains(keyword)))
            && (!DeliveryTaskId.HasValue || x.DeliveryTaskId == DeliveryTaskId.Value)
            && (!DriverId.HasValue || x.DriverId == DriverId.Value)
            && (!CustomerId.HasValue || x.CustomerId == CustomerId.Value)
            && (!HandleStatus.HasValue || x.HandleStatus == HandleStatus.Value)
            && (!CreateTimeStart.HasValue || x.CreateTime >= CreateTimeStart.Value)
            && (!CreateTimeEnd.HasValue || x.CreateTime <= CreateTimeEnd.Value);
    }
}
