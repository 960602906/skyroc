using Domain.Entities.AfterSales;
using Domain.Interfaces;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

/// <summary>
/// 售后审核轨迹仓储实现，业务服务仅追加轨迹记录。
/// </summary>
public class AfterSaleAuditLogRepository(ApplicationDbContext context)
    : Repository<AfterSaleAuditLog>(context), IAfterSaleAuditLogRepository;
