using Domain.Entities;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace Infrastructure.Repositories;

/// <summary>
///     客户协议价仓储。
/// </summary>
public class CustomerProtocolRepository(ApplicationDbContext context)
    : NamedCodeRepository<CustomerProtocol>(context), ICustomerProtocolRepository
{
    private readonly ApplicationDbContext _context = context;

    /// <inheritdoc />
    public override async Task<CustomerProtocol?> GetByIdAsync(Guid id)
    {
        return await DbSet
            .Include(x => x.Quotation)
            .Include(x => x.Goods)
                .ThenInclude(x => x.Goods)
            .Include(x => x.Goods)
                .ThenInclude(x => x.GoodsUnit)
            .Include(x => x.Customers)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <inheritdoc />
    public async Task ReplaceCustomerRelationsAsync(Guid customerProtocolId, IEnumerable<Guid>? customerIds)
    {
        var relations = await _context.Set<CustomerProtocolCustomer>()
            .Where(x => x.CustomerProtocolId == customerProtocolId)
            .ToListAsync();
        _context.Set<CustomerProtocolCustomer>().RemoveRange(relations);

        var newRelations = customerIds?
            .Where(x => x != Guid.Empty)
            .Distinct()
            .Select(customerId => new CustomerProtocolCustomer
            {
                CustomerProtocolId = customerProtocolId,
                CustomerId = customerId
            }) ?? [];

        await _context.Set<CustomerProtocolCustomer>().AddRangeAsync(newRelations);
    }
}
