namespace Application.DTOs.Delivery;

/// <summary>
/// 配送异常处理请求，记录处理动作和结果并恢复任务可执行状态。
/// </summary>
public class HandleDeliveryExceptionDto
{
    /// <summary>
    /// 异常处理动作、结果及后续配送安排。
    /// </summary>
    public string HandleRemark { get; set; } = string.Empty;
}
