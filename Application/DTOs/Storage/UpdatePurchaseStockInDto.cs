using Application.Serialization;
using Domain.Entities.Purchases;
using System.Text.Json.Serialization;

namespace Application.DTOs.Storage;

/// <summary>
/// 采购入库草稿编辑请求，整单替换主单字段与商品行。
/// </summary>
public class UpdatePurchaseStockInDto
{
    /// <summary>
    /// 待编辑的采购入库单主键。
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 接收入库商品的仓库主键。
    /// </summary>
    public Guid WareId { get; set; }

    /// <summary>
    /// 来源采购单主键；用于回填供应商、采购员和采购模式并支持追溯。
    /// </summary>
    public Guid? PurchaseOrderId { get; set; }

    /// <summary>
    /// 供货供应商主键；供应商直供采购入库时必填。
    /// </summary>
    public Guid? SupplierId { get; set; }

    /// <summary>
    /// 发起入库业务的部门主键。
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// 负责采购到货的采购员主键。
    /// </summary>
    public Guid? PurchaserId { get; set; }

    /// <summary>
    /// 采购模式：供应商直供或市场自采。
    /// </summary>
    public PurchasePattern PurchasePattern { get; set; } = PurchasePattern.SupplierDirect;

    /// <summary>
    /// 计划或实际入库时间（UTC）。
    /// </summary>
    [JsonConverter(typeof(FixedDateTimeJsonConverter))]
    public DateTime InTime { get; set; }

    /// <summary>
    /// 预计到货时间（UTC）；尚未确认时可为空。
    /// </summary>
    [JsonConverter(typeof(FixedNullableDateTimeJsonConverter))]
    public DateTime? ExpectedArrivalTime { get; set; }

    /// <summary>
    /// 入库单级业务备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 采购入库商品行完整集合，至少包含一项。
    /// </summary>
    public List<UpdateStockInDetailDto> Details { get; set; } = [];
}
