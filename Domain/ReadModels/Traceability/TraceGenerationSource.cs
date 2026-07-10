namespace Domain.ReadModels.Traceability;

/// <summary>
/// 为销售订单商品生成溯源记录时使用的只读来源投影，聚合销售出库批次、采购入库和检测报告快照。
/// </summary>
public class TraceGenerationSource
{
    /// <summary>销售订单主键。</summary>
    public Guid SaleOrderId { get; set; }

    /// <summary>销售订单编号快照。</summary>
    public string SaleOrderNo { get; set; } = string.Empty;

    /// <summary>销售订单商品明细主键。</summary>
    public Guid SaleOrderDetailId { get; set; }

    /// <summary>下单客户主键。</summary>
    public Guid CustomerId { get; set; }

    /// <summary>下单客户名称快照。</summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>商品主键。</summary>
    public Guid GoodsId { get; set; }

    /// <summary>商品名称快照。</summary>
    public string GoodsName { get; set; } = string.Empty;

    /// <summary>商品编码快照。</summary>
    public string GoodsCode { get; set; } = string.Empty;

    /// <summary>商品分类名称快照。</summary>
    public string? GoodsTypeName { get; set; }

    /// <summary>采购入库商品明细主键。</summary>
    public Guid StockInDetailId { get; set; }

    /// <summary>同一销售批次可定位的采购入库明细数量；非一时表示来源歧义。</summary>
    public int StockInSourceCount { get; set; }

    /// <summary>供货供应商主键。</summary>
    public Guid? SupplierId { get; set; }

    /// <summary>供货供应商名称快照。</summary>
    public string? SupplierName { get; set; }

    /// <summary>采购入库仓库主键。</summary>
    public Guid WareId { get; set; }

    /// <summary>采购入库仓库名称快照。</summary>
    public string WareName { get; set; } = string.Empty;

    /// <summary>销售出库扣减的批次号。</summary>
    public string BatchNo { get; set; } = string.Empty;

    /// <summary>关联的最新检测报告主键；未登记报告时为空。</summary>
    public Guid? InspectionReportId { get; set; }
}
