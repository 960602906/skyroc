namespace Application.DTOs.Storage;

/// <summary>
/// 其他入库草稿编辑请求，整单替换主单字段与商品行。
/// </summary>
public class UpdateOtherStockInDto
{
    /// <summary>
    /// 待编辑的其他入库单主键。
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 接收入库商品的仓库主键。
    /// </summary>
    public Guid WareId { get; set; }

    /// <summary>
    /// 发起入库业务的部门主键。
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// 计划或实际入库时间（UTC）。
    /// </summary>
    public DateTime InTime { get; set; }

    /// <summary>
    /// 入库单级业务备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 其他入库商品行完整集合，至少包含一项。
    /// </summary>
    public List<UpdateStockInDetailDto> Details { get; set; } = [];
}
