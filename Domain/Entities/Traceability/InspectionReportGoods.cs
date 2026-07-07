using Domain.Entities.Goods;
using Domain.Entities.Storage;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace Domain.Entities.Traceability;

/// <summary>
/// 检测报告商品明细，按入库商品行保存商品、单位、批次快照和单品检测结论。
/// </summary>
public class InspectionReportGoods : BaseEntity
{
    /// <summary>
    /// 所属检测报告主键。
    /// </summary>
    public Guid InspectionReportId { get; set; }

    /// <summary>
    /// 来源采购入库商品明细主键，同一报告内每行入库明细最多登记一次。
    /// </summary>
    public Guid StockInDetailId { get; set; }

    /// <summary>
    /// 送检商品主键。
    /// </summary>
    public Guid GoodsId { get; set; }

    /// <summary>
    /// 建报时的商品名称快照。
    /// </summary>
    public string GoodsNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 建报时的商品编码快照。
    /// </summary>
    public string GoodsCodeSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 建报时的商品分类名称快照；商品未挂分类时为空。
    /// </summary>
    public string? GoodsTypeNameSnapshot { get; set; }

    /// <summary>
    /// 送检数量使用的计量单位主键，与来源入库明细的入库单位一致。
    /// </summary>
    public Guid GoodsUnitId { get; set; }

    /// <summary>
    /// 建报时的送检计量单位名称快照。
    /// </summary>
    public string GoodsUnitNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 本次送检的商品数量，按送检单位计量且必须大于零。
    /// </summary>
    public decimal SampleQuantity { get; set; }

    /// <summary>
    /// 建报时的入库批次号快照，用于追溯到具体库存批次。
    /// </summary>
    public string BatchNoSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 当前商品行的检测结论，初始为待定。
    /// </summary>
    public InspectionConclusion Conclusion { get; set; } = InspectionConclusion.Pending;

    /// <summary>
    /// 仅针对当前报告商品行的检测说明或不合格原因。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 所属检测报告主单。
    /// </summary>
    public virtual InspectionReport InspectionReport { get; set; } = null!;

    /// <summary>
    /// 来源采购入库商品明细。
    /// </summary>
    public virtual StockInDetail StockInDetail { get; set; } = null!;

    /// <summary>
    /// 送检商品档案。
    /// </summary>
    public virtual GoodsEntity Goods { get; set; } = null!;

    /// <summary>
    /// 送检计量单位。
    /// </summary>
    public virtual GoodsUnit GoodsUnit { get; set; } = null!;
}
