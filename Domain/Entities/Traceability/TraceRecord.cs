using Domain.Entities.Customers;
using Domain.Entities.Orders;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace Domain.Entities.Traceability;

/// <summary>
/// 商品溯源记录，将销售订单商品行与采购入库来源和检测报告串联，支撑二维码详情展示。
/// </summary>
public class TraceRecord : BaseEntity
{
    /// <summary>
    /// 溯源记录业务唯一编号，作为二维码详情页的对外访问标识。
    /// </summary>
    public string TraceNo { get; set; } = string.Empty;

    /// <summary>
    /// 销售来源订单主键。
    /// </summary>
    public Guid SaleOrderId { get; set; }

    /// <summary>
    /// 生成溯源时的销售订单编号快照。
    /// </summary>
    public string SaleOrderNoSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 销售来源订单商品明细主键，每行订单商品最多生成一条溯源记录。
    /// </summary>
    public Guid SaleOrderDetailId { get; set; }

    /// <summary>
    /// 下单客户主键。
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// 生成溯源时的客户名称快照。
    /// </summary>
    public string CustomerNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 溯源商品主键。
    /// </summary>
    public Guid GoodsId { get; set; }

    /// <summary>
    /// 生成溯源时的商品名称快照。
    /// </summary>
    public string GoodsNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 生成溯源时的商品编码快照。
    /// </summary>
    public string GoodsCodeSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 生成溯源时的商品分类名称快照；商品未挂分类时为空。
    /// </summary>
    public string? GoodsTypeNameSnapshot { get; set; }

    /// <summary>
    /// 采购来源入库商品明细主键；尚未匹配到入库来源时为空。
    /// </summary>
    public Guid? StockInDetailId { get; set; }

    /// <summary>
    /// 采购来源供应商主键；入库来源缺失或没有供应商时为空。
    /// </summary>
    public Guid? SupplierId { get; set; }

    /// <summary>
    /// 生成溯源时的供应商名称快照。
    /// </summary>
    public string? SupplierNameSnapshot { get; set; }

    /// <summary>
    /// 采购来源入库仓库主键；入库来源缺失时为空。
    /// </summary>
    public Guid? WareId { get; set; }

    /// <summary>
    /// 生成溯源时的入库仓库名称快照。
    /// </summary>
    public string? WareNameSnapshot { get; set; }

    /// <summary>
    /// 生成溯源时的入库批次号快照；入库来源缺失时为空。
    /// </summary>
    public string? BatchNoSnapshot { get; set; }

    /// <summary>
    /// 关联检测报告主键；来源入库商品尚未出具报告时为空。
    /// </summary>
    public Guid? InspectionReportId { get; set; }

    /// <summary>
    /// 溯源记录备注，记录来源匹配差异或人工补录说明。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 销售来源订单。
    /// </summary>
    public virtual SaleOrder SaleOrder { get; set; } = null!;

    /// <summary>
    /// 销售来源订单商品明细。
    /// </summary>
    public virtual SaleOrderDetail SaleOrderDetail { get; set; } = null!;

    /// <summary>
    /// 下单客户档案。
    /// </summary>
    public virtual Customer Customer { get; set; } = null!;

    /// <summary>
    /// 溯源商品档案。
    /// </summary>
    public virtual GoodsEntity Goods { get; set; } = null!;

    /// <summary>
    /// 采购来源入库商品明细。
    /// </summary>
    public virtual StockInDetail? StockInDetail { get; set; }

    /// <summary>
    /// 采购来源供应商档案。
    /// </summary>
    public virtual Supplier? Supplier { get; set; }

    /// <summary>
    /// 采购来源入库仓库。
    /// </summary>
    public virtual Ware? Ware { get; set; }

    /// <summary>
    /// 关联的检测报告。
    /// </summary>
    public virtual InspectionReport? InspectionReport { get; set; }
}
