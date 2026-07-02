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
///     报价单仓储。
/// </summary>
public class QuotationRepository(ApplicationDbContext context)
    : NamedCodeRepository<Quotation>(context), IQuotationRepository
{
    private readonly ApplicationDbContext _context = context;

    public override async Task<Quotation?> GetByIdAsync(Guid id)
    {
        return await DbSet
            .Include(x => x.Goods)
                .ThenInclude(x => x.Goods)
            .Include(x => x.Goods)
                .ThenInclude(x => x.GoodsUnit)
            .Include(x => x.CustomerQuotations)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task ReplaceCustomerRelationsAsync(Guid quotationId, IEnumerable<Guid>? customerIds)
    {
        var relations = await _context.Set<CustomerQuotation>()
            .Where(x => x.QuotationId == quotationId)
            .ToListAsync();
        _context.Set<CustomerQuotation>().RemoveRange(relations);

        var newRelations = customerIds?
            .Where(x => x != Guid.Empty)
            .Distinct()
            .Select(customerId => new CustomerQuotation
            {
                QuotationId = quotationId,
                CustomerId = customerId,
                IsDefault = false
            }) ?? [];

        await _context.Set<CustomerQuotation>().AddRangeAsync(newRelations);
    }
}

