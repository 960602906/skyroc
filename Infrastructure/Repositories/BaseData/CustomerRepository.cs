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
///     客户仓储。
/// </summary>
public class CustomerRepository(ApplicationDbContext context)
    : NamedCodeRepository<Customer>(context), ICustomerRepository
{
    private readonly ApplicationDbContext _context = context;

    public override async Task<Customer?> GetByIdAsync(Guid id)
    {
        return await DbSet
            .Include(x => x.Company)
            .Include(x => x.Quotation)
            .Include(x => x.DefaultWare)
            .Include(x => x.TagRelations)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task ReplaceTagRelationsAsync(Guid customerId, IEnumerable<Guid>? tagIds)
    {
        var relations = await _context.Set<CustomerTagRelation>()
            .Where(x => x.CustomerId == customerId)
            .ToListAsync();
        _context.Set<CustomerTagRelation>().RemoveRange(relations);

        var newRelations = tagIds?
            .Where(x => x != Guid.Empty)
            .Distinct()
            .Select(tagId => new CustomerTagRelation
            {
                CustomerId = customerId,
                CustomerTagId = tagId
            }) ?? [];

        await _context.Set<CustomerTagRelation>().AddRangeAsync(newRelations);
    }
}

