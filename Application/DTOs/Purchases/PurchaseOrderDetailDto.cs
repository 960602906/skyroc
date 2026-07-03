namespace Application.DTOs.Purchases;

/// <summary>
/// 采购单商品明细 DTO，返回商品、单位、数量、金额和计划来源快照。
/// </summary>
public class PurchaseOrderDetailDto : BaseDto
{
    /// <summary>
    /// 所属采购单主键。
    /// </summary>
    public Guid PurchaseOrderId { get; set; }

    /// <summary>
    /// 采购商品主键。
    /// </summary>
    public Guid GoodsId { get; set; }

    /// <summary>
    /// 采购发生时的商品名称快照。
    /// </summary>
    public string GoodsName { get; set; } = string.Empty;

    /// <summary>
    /// 采购发生时的商品编码快照。
    /// </summary>
    public string GoodsCode { get; set; } = string.Empty;

    /// <summary>
    /// 商品规格、品牌和产地等历史详情快照。
    /// </summary>
    public string? GoodsInfo { get; set; }

    /// <summary>
    /// 采购单位主键。
    /// </summary>
    public Guid PurchaseUnitId { get; set; }

    /// <summary>
    /// 采购发生时的采购单位名称快照。
    /// </summary>
    public string PurchaseUnitName { get; set; } = string.Empty;

    /// <summary>
    /// 业务需求数量，按采购单位计量。
    /// </summary>
    public decimal RequiredQuantity { get; set; }

    /// <summary>
    /// 本单采购数量，按采购单位计量。
    /// </summary>
    public decimal PurchaseQuantity { get; set; }

    /// <summary>
    /// 采购单价，币种沿用系统业务币种。
    /// </summary>
    public decimal PurchasePrice { get; set; }

    /// <summary>
    /// 采购金额，为采购数量乘以单价后的金额快照。
    /// </summary>
    public decimal PurchaseTotalPrice { get; set; }

    /// <summary>
    /// 商品生产日期，仅记录自然日；未知时为空。
    /// </summary>
    public DateOnly? ProductDate { get; set; }

    /// <summary>
    /// 当前采购商品行的业务备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 当前商品行占用的采购计划来源集合。
    /// </summary>
    public List<PurchaseOrderPlanRelationDto> PlanRelations { get; set; } = [];
}
