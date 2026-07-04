using Application.Serialization;
using System.Text.Json.Serialization;

namespace Application.DTOs.Delivery;

/// <summary>
/// 订单签收回单响应，返回任务来源、客户签收事实、回单归档状态和商品验收明细。
/// </summary>
public class OrderReceiptDto : BaseDto
{
    /// <summary>
    /// 签收回单业务编号。
    /// </summary>
    public string ReceiptNo { get; set; } = string.Empty;

    /// <summary>
    /// 对应配送任务主键。
    /// </summary>
    public Guid DeliveryTaskId { get; set; }

    /// <summary>
    /// 对应销售订单主键。
    /// </summary>
    public Guid SaleOrderId { get; set; }

    /// <summary>
    /// 对应销售出库单主键。
    /// </summary>
    public Guid StockOutOrderId { get; set; }

    /// <summary>
    /// 客户侧实际签收人姓名。
    /// </summary>
    public string SignerName { get; set; } = string.Empty;

    /// <summary>
    /// 客户完成签收的时间（UTC）。
    /// </summary>
    [JsonConverter(typeof(FixedDateTimeJsonConverter))]
    public DateTime SignedTime { get; set; }

    /// <summary>
    /// 签收时记录的交付说明或客户意见。
    /// </summary>
    public string? SignRemark { get; set; }

    /// <summary>
    /// 纸质扫描件或电子回单的可访问地址；尚未回单时为空。
    /// </summary>
    public string? ReceiptImageUrl { get; set; }

    /// <summary>
    /// 回单资料确认归档的时间（UTC）；尚未回单时为空。
    /// </summary>
    [JsonConverter(typeof(FixedNullableDateTimeJsonConverter))]
    public DateTime? ReturnedTime { get; set; }

    /// <summary>
    /// 回单归档说明。
    /// </summary>
    public string? ReturnRemark { get; set; }

    /// <summary>
    /// 本次配送商品的客户验收结果。
    /// </summary>
    public List<OrderCheckDetailDto> CheckDetails { get; set; } = [];
}
