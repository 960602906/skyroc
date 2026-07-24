namespace Application.DTOs.Storage;

/// <summary>
/// 其他出库草稿编辑请求，整单替换仓库、部门和商品批次明细。
/// </summary>
public class UpdateOtherStockOutDto
{
    /// <summary>
    /// 待编辑的其他出库单主键。
    /// </summary>
    public Guid Id { get; set; }

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
    public DateTime OutTime { get; set; }

    /// <summary>
    /// 其他出库单级业务备注，应说明手工扣减原因。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 其他出库商品行完整集合，至少包含一项。
    /// </summary>
    public List<UpdateStockOutDetailDto> Details { get; set; } = [];
}
