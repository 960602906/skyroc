namespace Application.DTOs.Traceability;
/// <summary>可创建检测报告的采购入库单响应，仅返回已审核采购入库。</summary>
public class InspectionStockInOrderDto
{
    /// <summary>采购入库单主键。</summary>
    public Guid Id { get; set; }
    /// <summary>采购入库单编号。</summary>
    public string InNo { get; set; } = string.Empty;
    /// <summary>入库仓库主键。</summary>
    public Guid WareId { get; set; }
    /// <summary>入库仓库名称快照。</summary>
    public string WareName { get; set; } = string.Empty;
    /// <summary>供货供应商主键。</summary>
    public Guid? SupplierId { get; set; }
    /// <summary>供货供应商名称快照。</summary>
    public string? SupplierName { get; set; }
    /// <summary>审核入库时间（UTC）。</summary>
    public DateTime? AuditTime { get; set; }
}
