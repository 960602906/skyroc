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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task ReplaceCustomerRelationsAsync(Guid quotationId, IEnumerable<Guid>? customerIds)
    {
        // 差量同步，避免「先全删再全插」在同一 SaveChanges 中对复合主键 (customer_id, quotation_id)
        // 产生 delete+insert 冲突（尤其是保留原客户再追加时）。
        var desiredIds = customerIds?
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToHashSet() ?? [];

        var existing = await _context.Set<CustomerQuotation>()
            .Where(x => x.QuotationId == quotationId)
            .ToListAsync();

        var existingIds = existing.Select(x => x.CustomerId).ToHashSet();

        var toRemove = existing.Where(x => !desiredIds.Contains(x.CustomerId)).ToList();
        if (toRemove.Count > 0)
        {
            _context.Set<CustomerQuotation>().RemoveRange(toRemove);
        }

        var toAdd = desiredIds
            .Where(id => !existingIds.Contains(id))
            .Select(customerId => new CustomerQuotation
            {
                QuotationId = quotationId,
                CustomerId = customerId,
                IsDefault = false
            })
            .ToList();

        if (toAdd.Count > 0)
        {
            await _context.Set<CustomerQuotation>().AddRangeAsync(toAdd);
        }
    }
}
