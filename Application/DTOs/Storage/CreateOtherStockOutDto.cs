using Application.Serialization;
using System.Text.Json.Serialization;

namespace Application.DTOs.Storage;

/// <summary>
/// 其他出库创建请求，由授权人员手工扣减指定批次库存。
/// </summary>
public class CreateOtherStockOutDto
{
    /// <summary>
    /// 发出商品的仓库主键。
    /// </summary>
    public Guid WareId { get; set; }

    /// <summary>
    /// 发起其他出库业务的部门主键。
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// 计划或实际出库时间（UTC）。
    /// </summary>
    [JsonConverter(typeof(FixedDateTimeJsonConverter))]
    public DateTime OutTime { get; set; }

    /// <summary>
    /// 其他出库单级业务备注，应说明手工扣减原因。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 其他出库商品行，至少包含一项且每行必须选择库存批次。
    /// </summary>
    public List<CreateStockOutDetailDto> Details { get; set; } = [];
}
