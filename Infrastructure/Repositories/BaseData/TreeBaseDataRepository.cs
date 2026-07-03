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
///     树形基础资料仓储基类。
/// </summary>
public abstract class TreeBaseDataRepository<TEntity>(ApplicationDbContext context)
    : NamedCodeRepository<TEntity>(context), ITreeBaseDataRepository<TEntity>
    where TEntity : BaseEntity
{
    /// <inheritdoc />
    public virtual async Task<List<TEntity>> GetAllTreeSourceAsync()
    {
        return await DbSet
            .AsNoTracking()
            .OrderBy(x => EF.Property<int>(x, "Sort"))
            .ThenBy(x => x.Id)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<bool> HasChildrenAsync(Guid parentId)
    {
        return await DbSet.AnyAsync(x => EF.Property<Guid?>(x, "ParentId") == parentId);
    }
}
