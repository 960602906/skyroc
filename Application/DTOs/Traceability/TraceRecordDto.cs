namespace Application.DTOs.Traceability;
/// <summary>商品溯源记录响应，展示订单商品与采购批次、供应商、仓库和报告快照。</summary>
public class TraceRecordDto : BaseDto
{
    /// <summary>二维码对外使用的溯源业务编号。</summary>
    public string TraceNo { get; set; } = string.Empty;
    /// <summary>销售订单主键。</summary>
    public Guid SaleOrderId { get; set; }
    /// <summary>销售订单编号快照。</summary>
    public string SaleOrderNo { get; set; } = string.Empty;
    /// <summary>销售订单商品明细主键。</summary>
    public Guid SaleOrderDetailId { get; set; }
    /// <summary>下单客户名称快照。</summary>
    public string CustomerName { get; set; } = string.Empty;
    /// <summary>商品名称快照。</summary>
    public string GoodsName { get; set; } = string.Empty;
    /// <summary>商品编码快照。</summary>
    public string GoodsCode { get; set; } = string.Empty;
    /// <summary>商品分类名称快照。</summary>
    public string? GoodsTypeName { get; set; }
    /// <summary>供货供应商名称快照。</summary>
    public string? SupplierName { get; set; }
    /// <summary>采购入库仓库名称快照。</summary>
    public string? WareName { get; set; }
    /// <summary>采购入库批次号快照。</summary>
    public string? BatchNo { get; set; }
    /// <summary>关联检测报告主键。</summary>
    public Guid? InspectionReportId { get; set; }
    /// <summary>人工补录或来源差异备注。</summary>
    public string? Remark { get; set; }
}
