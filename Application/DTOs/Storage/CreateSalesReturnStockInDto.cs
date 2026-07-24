namespace Application.DTOs.Storage;

/// <summary>
/// 销售退货入库创建请求，记录客户退回商品，创建结果始终为草稿。
/// </summary>
public class CreateSalesReturnStockInDto
{
    /// <summary>
    /// 来源售后单主键；通过已完成取货任务办理入库时必填，手工退货入库时为空。
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
    public DateTime InTime { get; set; }

    /// <summary>
    /// 入库单级业务备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 销售退货入库商品行，至少包含一项。
    /// </summary>
    public List<CreateStockInDetailDto> Details { get; set; } = [];
}
