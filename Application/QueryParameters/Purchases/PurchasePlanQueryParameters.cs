using System.Linq.Expressions;
using Domain.Entities.Purchases;
using Shared.Constants;

namespace Application.QueryParameters;

/// <summary>
/// 采购计划分页查询参数。
/// </summary>
public class PurchasePlanQueryParameters : PagedQueryParameters
{
    /// <summary>
    /// 计划编号或商品名称/编码关键字，模糊匹配。
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 计划交期起始时间（含），UTC。
    /// </summary>
    public DateTime? PlanDateStart { get; set; }

    /// <summary>
    /// 计划交期截止时间（含），UTC。
    /// </summary>
    public DateTime? PlanDateEnd { get; set; }

    /// <summary>
    /// 采购模式筛选：供应商直供或市场自采。
    /// </summary>
    public PurchasePattern? PurchasePattern { get; set; }

    /// <summary>
    /// 采购单生成状态筛选。
    /// </summary>
    public PurchasePlanStatus? PurchaseStatus { get; set; }

    /// <summary>
    /// 供应商主键筛选。
    /// </summary>
    public Guid? SupplierId { get; set; }

    /// <summary>
    /// 采购员主键筛选。
    /// </summary>
    public Guid? PurchaserId { get; set; }

    /// <summary>
    /// 商品主键筛选，命中包含该商品的采购计划。
    /// </summary>
    public Guid? GoodsId { get; set; }

    /// <summary>
    /// 启用禁用状态筛选。
    /// </summary>
    public Status? Status { get; set; }

    /// <summary>
    /// 构建采购计划查询表达式。
    /// </summary>
    public Expression<Func<PurchasePlan, bool>> QueryBuild()
    {
        var keyword = Keyword?.Trim();

        return x =>
            (string.IsNullOrWhiteSpace(keyword)
             || x.PlanNo.Contains(keyword)
             || x.Details.Any(detail => detail.GoodsNameSnapshot.Contains(keyword)
                                        || detail.GoodsCodeSnapshot.Contains(keyword)))
            && (!PlanDateStart.HasValue || x.PlanDate >= PlanDateStart.Value)
            && (!PlanDateEnd.HasValue || x.PlanDate <= PlanDateEnd.Value)
            && (!PurchasePattern.HasValue || x.PurchasePattern == PurchasePattern.Value)
            && (!PurchaseStatus.HasValue || x.PurchaseStatus == PurchaseStatus.Value)
            && (!SupplierId.HasValue || x.SupplierId == SupplierId.Value)
            && (!PurchaserId.HasValue || x.PurchaserId == PurchaserId.Value)
            && (!GoodsId.HasValue || x.Details.Any(detail => detail.GoodsId == GoodsId.Value))
            && (!Status.HasValue || x.Status == Status.Value);
    }
}
