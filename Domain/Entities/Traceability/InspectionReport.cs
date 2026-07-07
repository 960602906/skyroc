using Domain.Entities.Purchases;
using Domain.Entities.Storage;

namespace Domain.Entities.Traceability;

/// <summary>
/// 检测报告主单，按采购入库单登记商品质量检测机构、时间和整单结论快照。
/// </summary>
public class InspectionReport : BaseEntity
{
    /// <summary>
    /// 检测报告业务唯一编号。
    /// </summary>
    public string InspectionNo { get; set; } = string.Empty;

    /// <summary>
    /// 来源采购入库单主键，报告商品明细必须来自该入库单。
    /// </summary>
    public Guid StockInOrderId { get; set; }

    /// <summary>
    /// 建报时的来源采购入库单编号快照。
    /// </summary>
    public string InNoSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 接收入库商品的仓库主键。
    /// </summary>
    public Guid WareId { get; set; }

    /// <summary>
    /// 建报时的仓库名称快照。
    /// </summary>
    public string WareNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 供货供应商主键；来源入库单没有供应商时为空。
    /// </summary>
    public Guid? SupplierId { get; set; }

    /// <summary>
    /// 建报时的供应商名称快照。
    /// </summary>
    public string? SupplierNameSnapshot { get; set; }

    /// <summary>
    /// 出具检测结论的检测机构名称。
    /// </summary>
    public string InspectionOrg { get; set; } = string.Empty;

    /// <summary>
    /// 商品抽样时间（UTC）；未记录时为空。
    /// </summary>
    public DateTime? SampleTime { get; set; }

    /// <summary>
    /// 检测完成时间（UTC）。
    /// </summary>
    public DateTime InspectTime { get; set; }

    /// <summary>
    /// 报告整单检测结论，初始为待定。
    /// </summary>
    public InspectionConclusion Conclusion { get; set; } = InspectionConclusion.Pending;

    /// <summary>
    /// 报告级业务备注，对全部报告商品生效。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 来源采购入库单。
    /// </summary>
    public virtual StockInOrder StockInOrder { get; set; } = null!;

    /// <summary>
    /// 接收入库商品的仓库。
    /// </summary>
    public virtual Ware Ware { get; set; } = null!;

    /// <summary>
    /// 供货供应商档案。
    /// </summary>
    public virtual Supplier? Supplier { get; set; }

    /// <summary>
    /// 报告商品明细集合；随主单删除而删除。
    /// </summary>
    public virtual ICollection<InspectionReportGoods> Goods { get; set; } = new List<InspectionReportGoods>();

    /// <summary>
    /// 报告附件集合；随主单删除而删除。
    /// </summary>
    public virtual ICollection<InspectionAttachment> Attachments { get; set; } = new List<InspectionAttachment>();
}
