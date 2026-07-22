using Domain.Entities.Goods;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace Domain.Entities.AI;

/// <summary>
/// AI 订单草稿商品明细，保存人工确认前重新校验价格和单位所需的最小业务快照。
/// </summary>
public class AiOrderDraftDetail : BaseEntity
{
    /// <summary>
    /// 所属 AI 订单草稿主键。
    /// </summary>
    public Guid AiOrderDraftId { get; set; }

    /// <summary>
    /// 商品行在草稿中的稳定展示顺序，从 1 开始。
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 下单商品主键。
    /// </summary>
    public Guid GoodsId { get; set; }

    /// <summary>
    /// 草稿生成时的商品名称快照。
    /// </summary>
    public string GoodsNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 草稿生成时的商品编码快照。
    /// </summary>
    public string GoodsCodeSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 下单数量使用的商品单位主键。
    /// </summary>
    public Guid GoodsUnitId { get; set; }

    /// <summary>
    /// 草稿生成时的下单单位名称快照。
    /// </summary>
    public string GoodsUnitNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 按下单单位计量的业务数量。
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// 草稿生成时换算到商品基础单位的数量。
    /// </summary>
    public decimal BaseQuantity { get; set; }

    /// <summary>
    /// 商品基础单位主键。
    /// </summary>
    public Guid? BaseUnitId { get; set; }

    /// <summary>
    /// 草稿生成时的基础单位名称快照。
    /// </summary>
    public string? BaseUnitNameSnapshot { get; set; }

    /// <summary>
    /// 下单单位换算为基础单位的比例快照。
    /// </summary>
    public decimal UnitConversion { get; set; } = 1m;

    /// <summary>
    /// 草稿生成时解析或由用户提供的固定单价。
    /// </summary>
    public decimal FixedPrice { get; set; }

    /// <summary>
    /// 固定单价对应的计价单位主键。
    /// </summary>
    public Guid FixedGoodsUnitId { get; set; }

    /// <summary>
    /// 草稿生成时的计价单位名称快照。
    /// </summary>
    public string FixedGoodsUnitNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 固定单价的业务来源。
    /// </summary>
    public AiOrderDraftPriceSource PriceSource { get; set; } = AiOrderDraftPriceSource.Unresolved;

    /// <summary>
    /// 协议价商品或报价商品来源记录主键；未解析或用户提供价格时为空。
    /// </summary>
    public Guid? PriceSourceRecordId { get; set; }

    /// <summary>
    /// 价格来源记录在草稿生成时的最后更新时间（UTC），用于确认时识别变化。
    /// </summary>
    public DateTime? PriceSourceUpdatedTimeSnapshot { get; set; }

    /// <summary>
    /// 价格来源在草稿生成时要求的最小起订数量；没有限制时为空。
    /// </summary>
    public decimal? MinimumOrderQuantitySnapshot { get; set; }

    /// <summary>
    /// 用户提供的当前商品行业务备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 所属 AI 订单草稿。
    /// </summary>
    public virtual AiOrderDraft AiOrderDraft { get; set; } = null!;

    /// <summary>
    /// 关联商品档案。
    /// </summary>
    public virtual GoodsEntity Goods { get; set; } = null!;

    /// <summary>
    /// 下单数量使用的商品单位。
    /// </summary>
    public virtual GoodsUnit GoodsUnit { get; set; } = null!;

    /// <summary>
    /// 商品基础单位。
    /// </summary>
    public virtual GoodsUnit? BaseUnit { get; set; }

    /// <summary>
    /// 固定单价对应的计价单位。
    /// </summary>
    public virtual GoodsUnit FixedGoodsUnit { get; set; } = null!;
}
