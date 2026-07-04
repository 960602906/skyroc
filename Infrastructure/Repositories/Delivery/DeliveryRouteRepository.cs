using Domain.Entities.Delivery;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
///     配送路线仓储，负责路线及其客户关系的读取与整体替换。
/// </summary>
public class DeliveryRouteRepository(ApplicationDbContext context)
    : NamedCodeRepository<DeliveryRoute>(context), IDeliveryRouteRepository
{
    private readonly ApplicationDbContext _context = context;

    /// <inheritdoc />
    public override async Task<DeliveryRoute?> GetByIdAsync(Guid id)
    {
        return await DbSet
            .Include(x => x.CustomerRoutes)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <inheritdoc />
    public async Task ReplaceCustomerRelationsAsync(Guid routeId, IEnumerable<Guid>? customerIds)
    {
        var relations = await _context.Set<CustomerRoute>()
            .Where(x => x.RouteId == routeId)
            .ToListAsync();
        _context.Set<CustomerRoute>().RemoveRange(relations);

        var newRelations = customerIds?
            .Where(x => x != Guid.Empty)
            .Distinct()
            .Select((customerId, index) => new CustomerRoute
            {
                RouteId = routeId,
                CustomerId = customerId,
                Sort = index
            }) ?? [];

        await _context.Set<CustomerRoute>().AddRangeAsync(newRelations);
    }
}
