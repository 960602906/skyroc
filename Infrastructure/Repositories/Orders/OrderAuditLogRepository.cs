using Domain.Entities.Orders;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// 订单审核记录仓储。
/// </summary>
public class OrderAuditLogRepository(ApplicationDbContext context)
    : Repository<OrderAuditLog>(context), IOrderAuditLogRepository
{
    public override async Task<OrderAuditLog?> GetByIdAsync(Guid id)
    {
        return await DbSet
            .Include(x => x.AuditUser)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<List<OrderAuditLog>> GetBySaleOrderIdAsync(Guid saleOrderId)
    {
        return await DbSet
            .Include(x => x.AuditUser)
            .Where(x => x.SaleOrderId == saleOrderId)
            .OrderBy(x => x.AuditTime)
            .ToListAsync();
    }
}
