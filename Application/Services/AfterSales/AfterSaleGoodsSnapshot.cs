namespace Application.Services;

/// <summary>
/// 售后商品构建过程中由订单或商品档案确定的权威业务快照。
/// </summary>
internal sealed class AfterSaleGoodsSnapshot
{
    /// <summary>商品主键。</summary>
    public required Guid GoodsId { get; init; }

    /// <summary>商品名称快照。</summary>
    public required string GoodsName { get; init; }

    /// <summary>商品编码快照。</summary>
    public required string GoodsCode { get; init; }

    /// <summary>商品分类名称快照。</summary>
    public string? GoodsTypeName { get; init; }

    /// <summary>申请商品单位主键。</summary>
    public required Guid GoodsUnitId { get; init; }

    /// <summary>申请商品单位名称快照。</summary>
    public required string GoodsUnitName { get; init; }

    /// <summary>商品基础单位主键。</summary>
    public Guid? BaseUnitId { get; init; }

    /// <summary>商品基础单位名称快照。</summary>
    public string? BaseUnitName { get; init; }

    /// <summary>申请单位换算为基础单位的比例。</summary>
    public required decimal ConversionRate { get; init; }

    /// <summary>按基础单位计量的售后数量。</summary>
    public required decimal BaseQuantity { get; init; }

    /// <summary>按申请单位计量的核算单价。</summary>
    public required decimal UnitPrice { get; init; }

    /// <summary>当前商品行的退款或减免金额。</summary>
    public required decimal RefundAmount { get; init; }

    /// <summary>原商品供货供应商主键。</summary>
    public Guid? SupplierId { get; init; }

    /// <summary>供应商名称快照。</summary>
    public string? SupplierName { get; init; }

    /// <summary>售后责任部门主键。</summary>
    public Guid? DepartmentId { get; init; }

    /// <summary>责任部门名称快照。</summary>
    public string? DepartmentName { get; init; }
}
