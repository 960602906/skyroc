namespace Application.DTOs.Storage;

/// <summary>
/// 销售出库草稿编辑请求，整单替换来源、业务方和商品批次明细。
/// </summary>
public class UpdateSaleStockOutDto
{
    /// <summary>
    /// 待编辑的销售出库单主键。
    /// </summary>
    public Guid Id { get; set; }

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
    public DateTime OutTime { get; set; }

    /// <summary>
    /// 销售出库单级业务备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 销售出库商品行完整集合，至少包含一项。
    /// </summary>
    public List<UpdateStockOutDetailDto> Details { get; set; } = [];
}
