namespace Domain.Entities.Traceability;

/// <summary>
/// 外部报送状态，跟踪单次报送从发起到外部平台确认的结果。
/// </summary>
public enum ExternalPushStatus
{
    /// <summary>
    /// 待报送：报送任务已登记，尚未收到外部平台的处理结果。
    /// </summary>
    Pending = 1,

    /// <summary>
    /// 报送成功：外部平台已确认接收本次报送数据。
    /// </summary>
    Success = 2,

    /// <summary>
    /// 报送失败：外部平台返回错误或调用异常，可重试报送。
    /// </summary>
    Failed = 3
}
