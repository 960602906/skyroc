using Application.Serialization;
using System.Text.Json.Serialization;

namespace Application.DTOs.Storage;

/// <summary>
/// 销售退货入库草稿编辑请求，整单替换主单字段与商品行。
/// </summary>
public class UpdateSalesReturnStockInDto
{
    /// <summary>
    /// 待编辑的销售退货入库单主键。
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 来源售后单主键；有值时不得更换，且全部商品行必须保留原取货任务来源。
    /// </summary>
    public Guid? AfterSaleId { get; set; }

    /// <summary>
    /// 接收退货商品的仓库主键。
    /// </summary>
    public Guid WareId { get; set; }

    /// <summary>
    /// 退货客户主键。
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// 发起入库业务的部门主键。
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// 计划或实际入库时间（UTC）。
    /// </summary>
    [JsonConverter(typeof(FixedDateTimeJsonConverter))]
    public DateTime InTime { get; set; }

    /// <summary>
    /// 入库单级业务备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 销售退货入库商品行完整集合，至少包含一项。
    /// </summary>
    public List<UpdateStockInDetailDto> Details { get; set; } = [];
}
