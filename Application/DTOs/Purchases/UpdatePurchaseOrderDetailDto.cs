namespace Application.DTOs.Purchases;

/// <summary>
/// 编辑采购单时的商品行请求，可重新声明该行占用的采购计划数量。
/// </summary>
public class UpdatePurchaseOrderDetailDto
{
    /// <summary>
    /// 原商品行主键；新增商品行时为空，非空时必须属于当前采购单。
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// 采购商品主键。
    /// </summary>
    public Guid GoodsId { get; set; }

    /// <summary>
    /// 采购单位主键，必须属于所选商品。
    /// </summary>
    public Guid PurchaseUnitId { get; set; }

    /// <summary>
    /// 业务需求数量，按采购单位计量；省略时等于采购数量。
    /// </summary>
    public decimal? RequiredQuantity { get; set; }

    /// <summary>
    /// 本单采购数量，按采购单位计量且必须大于零。
    /// </summary>
    public decimal PurchaseQuantity { get; set; }

    /// <summary>
    /// 采购单价，币种沿用系统业务币种且不得为负数。
    /// </summary>
    public decimal PurchasePrice { get; set; }

    /// <summary>
    /// 商品生产日期，仅记录自然日；未知时可为空。
    /// </summary>
    public DateOnly? ProductDate { get; set; }

    /// <summary>
    /// 仅针对当前采购商品行的备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 当前商品行的采购计划来源；有来源时占用数量合计必须等于采购数量。
    /// </summary>
    public List<PurchaseOrderPlanAllocationDto> PlanAllocations { get; set; } = [];
}
