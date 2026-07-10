namespace Application.DTOs.Traceability;

/// <summary>二维码公开溯源信息，仅包含消费者查询商品来源所需的固化快照。</summary>
public class PublicTraceQrRecordDto
{
    /// <summary>二维码对外使用的溯源业务编号。</summary>
    public string TraceNo { get; set; } = string.Empty;
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
}
