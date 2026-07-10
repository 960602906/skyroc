using System.Linq.Expressions;
using Domain.Entities.Traceability;
namespace Application.QueryParameters.Traceability;
/// <summary>外部报送日志分页查询条件，支持业务类型、业务主键、平台、状态和时间范围筛选。</summary>
public class ExternalPushLogQueryParameters : PagedQueryParameters
{
    /// <summary>来源业务类型。</summary>
    public ExternalPushBusinessType? BusinessType { get; set; }
    /// <summary>来源业务主键。</summary>
    public Guid? BusinessId { get; set; }
    /// <summary>目标平台编码。</summary>
    public string? PlatformCode { get; set; }
    /// <summary>报送结果状态。</summary>
    public ExternalPushStatus? PushStatus { get; set; }
    /// <summary>报送时间起点（UTC，包含）。</summary>
    public DateTime? PushStart { get; set; }
    /// <summary>报送时间终点（UTC，包含）。</summary>
    public DateTime? PushEnd { get; set; }
    /// <summary>模糊匹配来源业务编号、目标平台或错误摘要。</summary>
    public string? Keyword { get; set; }
    /// <summary>构造可由 EF Core 翻译的报送日志筛选表达式。</summary>
    public Expression<Func<ExternalPushLog, bool>> QueryBuild()
    {
        var keyword = Keyword?.Trim();
        var platform = PlatformCode?.Trim();
        return x => (!BusinessType.HasValue || x.BusinessType == BusinessType.Value)
                    && (!BusinessId.HasValue || x.BusinessId == BusinessId.Value)
                    && (string.IsNullOrWhiteSpace(platform) || x.PlatformCode == platform)
                    && (!PushStatus.HasValue || x.PushStatus == PushStatus.Value)
                    && (!PushStart.HasValue || x.PushTime >= PushStart.Value)
                    && (!PushEnd.HasValue || x.PushTime <= PushEnd.Value)
                    && (string.IsNullOrWhiteSpace(keyword) || x.BusinessNoSnapshot.Contains(keyword)
                        || x.PlatformCode.Contains(keyword)
                        || (x.ErrorMessage != null && x.ErrorMessage.Contains(keyword)));
    }
}
