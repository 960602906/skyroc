using Domain.Entities.Goods;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace Domain.Entities.Purchases;

/// <summary>
/// 采购单商品明细，保存商品、采购单位、数量、价格和生产日期快照。
/// </summary>
public class PurchaseOrderDetail : BaseEntity
{
    /// <summary>
    /// 所属采购单主键。
    /// </summary>
    public Guid PurchaseOrderId { get; set; }

    /// <summary>
    /// 关联商品主键。
    /// </summary>
    public Guid GoodsId { get; set; }

    /// <summary>
    /// 采购发生时的商品名称快照。
    /// </summary>
    public string GoodsNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 采购发生时的商品编码快照。
    /// </summary>
    public string GoodsCodeSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 采购发生时的商品详情序列化快照，用于还原历史规格等补充信息。
    /// </summary>
    public string? GoodsInfoSnapshot { get; set; }

    /// <summary>
    /// 采购计量单位主键。
    /// </summary>
    public Guid PurchaseUnitId { get; set; }

    /// <summary>
    /// 采购发生时的采购单位名称快照。
    /// </summary>
    public string PurchaseUnitNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 来源计划或手工录入的需求数量，按采购单位计量。
    /// </summary>
    public decimal RequiredQuantity { get; set; }

    /// <summary>
    /// 本采购单确认采购的数量，按采购单位计量。
    /// </summary>
    public decimal PurchaseQuantity { get; set; }

    /// <summary>
    /// 采购单价，币种沿用系统业务币种。
    /// </summary>
    public decimal PurchasePrice { get; set; }

    /// <summary>
    /// 采购金额，为采购数量乘以采购单价后的金额快照。
    /// </summary>
    public decimal PurchaseTotalPrice { get; set; }

    /// <summary>
    /// 商品生产日期，仅记录自然日；未知时可为空。
    /// </summary>
    public DateOnly? ProductDate { get; set; }

    /// <summary>
    /// 仅针对当前采购商品行的业务备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 所属采购单。
    /// </summary>
    public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;

    /// <summary>
    /// 关联商品档案。
    /// </summary>
    public virtual GoodsEntity Goods { get; set; } = null!;

    /// <summary>
    /// 采购计量单位。
    /// </summary>
    public virtual GoodsUnit PurchaseUnit { get; set; } = null!;

    /// <summary>
    /// 当前采购商品行占用的采购计划明细关系集合。
    /// </summary>
    public virtual ICollection<PurchaseOrderPlanRelation> PlanRelations { get; set; } = new List<PurchaseOrderPlanRelation>();
}
