namespace Application.DTOs.Delivery;

/// <summary>
/// 配送异常登记请求，司机和客户由任务快照自动带入，避免客户端伪造关联。
/// </summary>
public class CreateDeliveryExceptionDto
{
    /// <summary>
    /// 发生异常的配送任务主键，任务必须已分配司机且尚未签收。
    /// </summary>
    public Guid DeliveryTaskId { get; set; }

    /// <summary>
    /// 异常事实描述，去除首尾空白后长度不得超过 1000 个字符。
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
