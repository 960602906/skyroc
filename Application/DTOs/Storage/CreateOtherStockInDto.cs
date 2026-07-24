namespace Application.DTOs.Storage;

/// <summary>
/// 其他入库创建请求，由授权人员手工增加库存，创建结果始终为草稿。
/// </summary>
public class CreateOtherStockInDto
{
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
    /// 其他入库商品行，至少包含一项。
    /// </summary>
    public List<CreateStockInDetailDto> Details { get; set; } = [];
}
