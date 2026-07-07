namespace Domain.Entities.Traceability;

/// <summary>
/// 外部报送日志，只追加记录向外部监管或溯源平台每次报送的请求、响应和结果状态。
/// </summary>
public class ExternalPushLog : BaseEntity
{
    /// <summary>
    /// 报送来源业务类型：销售订单、检测报告或溯源记录。
    /// </summary>
    public ExternalPushBusinessType BusinessType { get; set; }

    /// <summary>
    /// 报送来源业务主键，按业务类型指向订单、报告或溯源记录，不建立数据库外键。
    /// </summary>
    public Guid BusinessId { get; set; }

    /// <summary>
    /// 报送发起时的来源业务编号快照。
    /// </summary>
    public string BusinessNoSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// 目标外部平台的稳定标识编码，由应用层维护取值。
    /// </summary>
    public string PlatformCode { get; set; } = string.Empty;

    /// <summary>
    /// 当前报送结果状态，初始为待报送。
    /// </summary>
    public ExternalPushStatus PushStatus { get; set; } = ExternalPushStatus.Pending;

    /// <summary>
    /// 报送发起时间（UTC）。
    /// </summary>
    public DateTime PushTime { get; set; }

    /// <summary>
    /// 外部平台返回响应的时间（UTC）；尚未响应时为空。
    /// </summary>
    public DateTime? ResponseTime { get; set; }

    /// <summary>
    /// 报送请求报文的脱敏序列化内容。
    /// </summary>
    public string? RequestContent { get; set; }

    /// <summary>
    /// 外部平台响应报文的脱敏序列化内容。
    /// </summary>
    public string? ResponseContent { get; set; }

    /// <summary>
    /// 报送失败时记录的错误摘要；成功时为空。
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 当前业务在本次记录前已重试报送的次数，首次报送为零。
    /// </summary>
    public int RetryCount { get; set; }
}
