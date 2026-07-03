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
///     客户标签仓储。
/// </summary>
public class CustomerTagRepository(ApplicationDbContext context)
    : TreeBaseDataRepository<CustomerTag>(context), ICustomerTagRepository
{
    private readonly ApplicationDbContext _context = context;

    /// <inheritdoc />
    public async Task<bool> HasCustomersAsync(Guid tagId)
    {
        return await _context.Set<CustomerTagRelation>().AnyAsync(x => x.CustomerTagId == tagId);
    }
}
