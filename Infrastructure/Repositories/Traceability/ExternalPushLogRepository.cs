using Domain.Entities.Traceability;
using Domain.Interfaces;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

/// <summary>
/// 外部报送日志仓储实现，复用通用仓储提供只追加日志的读取能力。
/// </summary>
public class ExternalPushLogRepository(ApplicationDbContext context)
    : Repository<ExternalPushLog>(context), IExternalPushLogRepository;
