using Domain.Entities.Delivery;
using Domain.Entities.Storage;

namespace Domain.Entities.Orders;

/// <summary>
/// 订单签收回单，按配送任务保存客户签收事实、纸质或电子回单及商品验收明细。
/// </summary>
public class OrderReceipt : BaseEntity
{
    /// <summary>
    /// 签收回单业务唯一编号。
    /// </summary>
    public string ReceiptNo { get; set; } = string.Empty;

    /// <summary>
    /// 对应配送任务主键；每个配送任务只能产生一张签收回单。
    /// </summary>
    public Guid DeliveryTaskId { get; set; }

    /// <summary>
    /// 对应销售订单主键，用于汇总整单签收和回单状态。
    /// </summary>
    public Guid SaleOrderId { get; set; }

    /// <summary>
    /// 对应销售出库单主键，用于追溯本次实际交付商品。
    /// </summary>
    public Guid StockOutOrderId { get; set; }

    /// <summary>
    /// 客户侧实际签收人姓名。
    /// </summary>
    public string SignerName { get; set; } = string.Empty;

    /// <summary>
    /// 客户完成签收的时间（UTC）。
    /// </summary>
    public DateTime SignedTime { get; set; }

    /// <summary>
    /// 签收时记录的交付说明或客户意见。
    /// </summary>
    public string? SignRemark { get; set; }

    /// <summary>
    /// 纸质扫描件或电子回单的可访问地址；完成回单前为空。
    /// </summary>
    public string? ReceiptImageUrl { get; set; }

    /// <summary>
    /// 回单资料确认归档的时间（UTC）；尚未回单时为空。
    /// </summary>
    public DateTime? ReturnedTime { get; set; }

    /// <summary>
    /// 回单归档说明，例如纸质件编号或缺页补充信息。
    /// </summary>
    public string? ReturnRemark { get; set; }

    /// <summary>
    /// 对应配送任务。
    /// </summary>
    public virtual DeliveryTask DeliveryTask { get; set; } = null!;

    /// <summary>
    /// 对应销售订单。
    /// </summary>
    public virtual SaleOrder SaleOrder { get; set; } = null!;

    /// <summary>
    /// 对应销售出库单。
    /// </summary>
    public virtual StockOutOrder StockOutOrder { get; set; } = null!;

    /// <summary>
    /// 本次配送所含商品的客户验收明细。
    /// </summary>
    public virtual ICollection<OrderCheckDetail> CheckDetails { get; set; } = new List<OrderCheckDetail>();
}
