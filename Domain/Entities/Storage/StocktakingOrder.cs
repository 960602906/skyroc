namespace Domain.Entities.Storage;

/// <summary>
/// 库存盘点主单，保存指定仓库盘点时点、账实汇总和调整执行状态。
/// </summary>
public class StocktakingOrder : BaseEntity
{
    /// <summary>
    /// 盘点单业务编号，在全部盘点单中保持唯一。
    /// </summary>
    public string StocktakingNo { get; set; } = string.Empty;

    /// <summary>
    /// 盘点单业务状态，审核后才允许生成差异调整流水。
    /// </summary>
    public StockDocumentStatus BusinessStatus { get; set; } = StockDocumentStatus.Draft;

    /// <summary>
    /// 被盘点仓库主键。
    /// </summary>
    public Guid WareId { get; set; }

    /// <summary>
    /// 盘点创建时的仓库名称快照。
    /// </summary>
    public string WareNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 盘点库存快照生成时间（UTC）。
    /// </summary>
    public DateTime StocktakingTime { get; set; }

    /// <summary>
    /// 盘点明细账面数量合计，按各商品基础单位分别求和，仅用于展示。
    /// </summary>
    public decimal TotalBookQuantity { get; set; }

    /// <summary>
    /// 盘点明细实盘数量合计，按各商品基础单位分别求和，仅用于展示。
    /// </summary>
    public decimal TotalActualQuantity { get; set; }

    /// <summary>
    /// 实盘数量减账面数量的差异合计，正数盘盈、负数盘亏。
    /// </summary>
    public decimal TotalDifferenceQuantity { get; set; }

    /// <summary>
    /// 是否已生成盘点差异流水，用于阻止重复调整库存。
    /// </summary>
    public bool IsAdjustmentApplied { get; set; }

    /// <summary>
    /// 盘点差异流水生成完成时间（UTC）；未调整时为空。
    /// </summary>
    public DateTime? AdjustmentTime { get; set; }

    /// <summary>
    /// 最近一次审核人的系统用户主键。
    /// </summary>
    public Guid? AuditUserId { get; set; }

    /// <summary>
    /// 最近一次审核时的用户名称快照。
    /// </summary>
    public string? AuditUserNameSnapshot { get; set; }

    /// <summary>
    /// 最近一次审核通过时间（UTC）。
    /// </summary>
    public DateTime? AuditTime { get; set; }

    /// <summary>
    /// 最近一次反审核人的系统用户主键。
    /// </summary>
    public Guid? ReverseUserId { get; set; }

    /// <summary>
    /// 最近一次反审核时的用户名称快照。
    /// </summary>
    public string? ReverseUserNameSnapshot { get; set; }

    /// <summary>
    /// 最近一次反审核完成时间（UTC）。
    /// </summary>
    public DateTime? ReverseTime { get; set; }

    /// <summary>
    /// 盘点范围、原因或差异说明。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 被盘点仓库。
    /// </summary>
    public virtual Ware Ware { get; set; } = null!;

    /// <summary>
    /// 盘点商品批次明细集合；随主单删除而删除。
    /// </summary>
    public virtual ICollection<StocktakingDetail> Details { get; set; } = new List<StocktakingDetail>();
}
