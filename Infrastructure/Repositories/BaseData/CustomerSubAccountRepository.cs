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
///     客户子账号仓储。
/// </summary>
public class CustomerSubAccountRepository(ApplicationDbContext context)
    : Repository<CustomerSubAccount>(context), ICustomerSubAccountRepository
{
    /// <inheritdoc />
    public override async Task<CustomerSubAccount?> GetByIdAsync(Guid id)
    {
        return await DbSet
            .Include(x => x.Company)
            .Include(x => x.Customer)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByUsernameAsync(string username, Guid? excludeId = null)
    {
        var normalizedUsername = username.Trim();
        var query = DbSet.Where(x => x.Username == normalizedUsername);
        if (excludeId.HasValue)
        {
            query = query.Where(x => x.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    /// <inheritdoc />
    public async Task<List<CustomerSubAccount>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        var idList = ids.Distinct().ToList();
        return idList.Count == 0
            ? []
            : await DbSet.Where(x => idList.Contains(x.Id)).ToListAsync();
    }
}
