using Application.Serialization;
using System.Text.Json.Serialization;

namespace Application.DTOs.Storage;

/// <summary>
/// 销售出库创建请求，可手工创建或关联已审核销售订单，创建结果始终为草稿。
/// </summary>
public class CreateSaleStockOutDto
{
    /// <summary>
    /// 发出商品的仓库主键；关联订单已指定仓库时必须保持一致。
    /// </summary>
    public Guid WareId { get; set; }

    /// <summary>
    /// 来源销售订单主键；手工销售出库可为空。
    /// </summary>
    public Guid? SaleOrderId { get; set; }

    /// <summary>
    /// 收货客户主键；关联订单时必须与订单客户一致。
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// 发起销售出库业务的部门主键。
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// 计划或实际出库时间（UTC）。
    /// </summary>
    [JsonConverter(typeof(FixedDateTimeJsonConverter))]
    public DateTime OutTime { get; set; }

    /// <summary>
    /// 销售出库单级业务备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 销售出库商品行，至少包含一项且每行必须选择库存批次。
    /// </summary>
    public List<CreateStockOutDetailDto> Details { get; set; } = [];
}
