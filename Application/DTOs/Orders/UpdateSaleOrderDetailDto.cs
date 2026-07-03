namespace Application.DTOs.Orders;

/// <summary>
/// 更新销售订单商品明细 DTO；ID 为空表示编辑时新增明细。
/// </summary>
public class UpdateSaleOrderDetailDto
{
    public Guid? Id { get; set; }

    public Guid GoodsId { get; set; }

    public Guid GoodsUnitId { get; set; }

    public decimal Quantity { get; set; }

    public decimal FixedPrice { get; set; }

    public Guid FixedGoodsUnitId { get; set; }

    public string? Remark { get; set; }

    public string? InnerRemark { get; set; }
}
