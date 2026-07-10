using Domain.Entities.Traceability;
namespace Application.DTOs.Traceability;
/// <summary>外部报送日志响应，展示业务编号、平台、状态、响应时间、失败摘要和重试次数；报文内容不对接口返回。</summary>
public class ExternalPushLogDto : BaseDto
{
    /// <summary>报送来源业务类型。</summary>
    public ExternalPushBusinessType BusinessType { get; set; }
    /// <summary>报送来源业务主键。</summary>
    public Guid BusinessId { get; set; }
    /// <summary>报送来源业务编号快照。</summary>
    public string BusinessNo { get; set; } = string.Empty;
    /// <summary>目标外部平台稳定编码。</summary>
    public string PlatformCode { get; set; } = string.Empty;
    /// <summary>报送结果状态。</summary>
    public ExternalPushStatus PushStatus { get; set; }
    /// <summary>报送发起时间（UTC）。</summary>
    public DateTime PushTime { get; set; }
    /// <summary>外部响应时间（UTC）。</summary>
    public DateTime? ResponseTime { get; set; }
    /// <summary>失败错误摘要。</summary>
    public string? ErrorMessage { get; set; }
    /// <summary>本次记录前的重试次数。</summary>
    public int RetryCount { get; set; }
}
