using Domain.Entities;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using Domain.Interfaces;
using Domain.ReadModels.BaseData;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace Infrastructure.Repositories;

/// <summary>
///     带名称和编码的基础资料仓储基类。
/// </summary>
public abstract class NamedCodeRepository<TEntity>(ApplicationDbContext context) : Repository<TEntity>(context),
    INamedCodeRepository<TEntity>
    where TEntity : BaseEntity
{
    /// <inheritdoc />
    public async Task<bool> ExistsByCodeAsync(string code, Guid? excludeId = null)
    {
        var normalizedCode = code.Trim();
        var query = DbSet.Where(x => EF.Property<string>(x, "Code") == normalizedCode);
        if (excludeId.HasValue)
        {
            query = query.Where(x => x.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null)
    {
        var normalizedName = name.Trim();
        var query = DbSet.Where(x => EF.Property<string>(x, "Name") == normalizedName);
        if (excludeId.HasValue)
        {
            query = query.Where(x => x.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    /// <inheritdoc />
    public virtual async Task<List<TEntity>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        var idList = ids.Distinct().ToList();
        return idList.Count == 0
            ? []
            : await DbSet.Where(x => idList.Contains(x.Id)).ToListAsync();
    }

    /// <inheritdoc />
    public virtual async Task<List<NamedCodeOption>> GetOptionsAsync()
    {
        // 数据库侧仅投影主键、名称和编码，避免加载明细导航属性造成多余带宽；Name/Code 通过 EF.Property 访问，兼容无统一实体接口的约定。
        return await DbSet
            .AsNoTracking()
            .OrderBy(x => EF.Property<string>(x, "Name"))
            .Select(x => new NamedCodeOption
            {
                Id = x.Id,
                Name = EF.Property<string>(x, "Name"),
                Code = EF.Property<string>(x, "Code")
            })
            .ToListAsync();
    }
}
