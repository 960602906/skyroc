using Domain.Entities.AfterSales;

namespace Domain.Interfaces;

/// <summary>
/// 售后审核轨迹仓储；轨迹只追加，不提供业务层修改或删除操作。
/// </summary>
public interface IAfterSaleAuditLogRepository : IRepository<AfterSaleAuditLog>;
