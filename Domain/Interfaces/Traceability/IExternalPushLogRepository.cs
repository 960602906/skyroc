using Domain.Entities.Traceability;

namespace Domain.Interfaces;

/// <summary>
/// 外部报送日志仓储接口，承载只追加的报送结果分页查询。
/// </summary>
public interface IExternalPushLogRepository : IRepository<ExternalPushLog>;
