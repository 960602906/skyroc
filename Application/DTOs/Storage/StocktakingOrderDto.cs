using Domain.Entities.Storage;
namespace Application.DTOs.Storage;

/// <summary>
/// 库存盘点主单 DTO，返回仓库快照、账实汇总、审核状态和批次差异明细。
/// </summary>
public class StocktakingOrderDto : BaseDto
{
    /// <summary>
    /// 全局唯一的盘点单业务编号。
    /// </summary>
    public string StocktakingNo { get; set; } = string.Empty;

    /// <summary>
    /// 盘点单状态：草稿、待审核或已审核；已审核表示差异库存已生效。
    /// </summary>
    public StockDocumentStatus BusinessStatus { get; set; }

    /// <summary>
    /// 被盘点仓库主键。
    /// </summary>
    public Guid WareId { get; set; }

    /// <summary>
    /// 盘点创建时的仓库名称快照。
    /// </summary>
    public string WareName { get; set; } = string.Empty;

    /// <summary>
    /// 服务端生成批次账面快照的时间（UTC）。
    /// </summary>
    public DateTime StocktakingTime { get; set; }

    /// <summary>
    /// 明细账面数量合计，按各商品基础单位分别求和，仅用于展示。
    /// </summary>
    public decimal TotalBookQuantity { get; set; }

    /// <summary>
    /// 明细实盘数量合计，按各商品基础单位分别求和，仅用于展示。
    /// </summary>
    public decimal TotalActualQuantity { get; set; }

    /// <summary>
    /// 实盘减账面的差异数量合计；正数盘盈、负数盘亏。
    /// </summary>
    public decimal TotalDifferenceQuantity { get; set; }

    /// <summary>
    /// 是否已生成库存差异流水；为真时禁止再次审核调整。
    /// </summary>
    public bool IsAdjustmentApplied { get; set; }

    /// <summary>
    /// 库存差异流水生成完成时间（UTC）；尚未调整时为空。
    /// </summary>
    public DateTime? AdjustmentTime { get; set; }

    /// <summary>
    /// 最近一次审核人的系统用户主键。
    /// </summary>
    public Guid? AuditUserId { get; set; }

    /// <summary>
    /// 最近一次审核时的用户名称快照。
    /// </summary>
    public string? AuditUserName { get; set; }

    /// <summary>
    /// 最近一次审核通过时间（UTC）。
    /// </summary>
    public DateTime? AuditTime { get; set; }

    /// <summary>
    /// 盘点范围、原因或差异说明。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 本次盘点包含的库存批次差异明细。
    /// </summary>
    public List<StocktakingDetailDto> Details { get; set; } = [];
}
