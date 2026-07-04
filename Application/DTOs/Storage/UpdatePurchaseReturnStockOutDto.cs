using Application.Serialization;
using System.Text.Json.Serialization;

namespace Application.DTOs.Storage;

/// <summary>
/// 采购退货出库草稿编辑请求，整单替换供应商、仓库和商品批次明细。
/// </summary>
public class UpdatePurchaseReturnStockOutDto
{
    /// <summary>
    /// 待编辑的采购退货出库单主键。
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 发出退货商品的仓库主键。
    /// </summary>
    public Guid WareId { get; set; }

    /// <summary>
    /// 接收退货商品的供应商主键。
    /// </summary>
    public Guid SupplierId { get; set; }

    /// <summary>
    /// 发起采购退货业务的部门主键。
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// 计划或实际退货出库时间（UTC）。
    /// </summary>
    [JsonConverter(typeof(FixedDateTimeJsonConverter))]
    public DateTime OutTime { get; set; }

    /// <summary>
    /// 采购退货出库单级业务备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 采购退货商品行完整集合，至少包含一项。
    /// </summary>
    public List<UpdateStockOutDetailDto> Details { get; set; } = [];
}
