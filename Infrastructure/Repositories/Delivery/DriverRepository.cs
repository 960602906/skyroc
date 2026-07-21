using Domain.Entities.Delivery;
using Domain.Interfaces;
using Domain.ReadModels.BaseData;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Infrastructure.Repositories;

/// <summary>
///     司机仓储，按主键读取时预加载所属承运商。
/// </summary>
public class DriverRepository(ApplicationDbContext context)
    : NamedCodeRepository<Driver>(context), IDriverRepository
{
    /// <inheritdoc />
    public async Task<bool> HasDeliveryTasksAsync(Guid id)
    {
        return await Context.Set<DeliveryTask>().AnyAsync(x => x.DriverId == id);
    }

    /// <inheritdoc />
    public override async Task<List<SelectionOption>> GetBoundedSelectionOptionsAsync(int take)
    {
        return await DbSet
            .AsNoTracking()
            .Where(x => x.Status == Status.Enable)
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Code)
            .ThenBy(x => x.Id)
            .Select(x => new SelectionOption
            {
                Id = x.Id,
                Label = x.Name,
                SecondaryText = x.Phone
            })
            .Take(take)
            .ToListAsync();
    }

    /// <inheritdoc />
    public override async Task<Driver?> GetByIdAsync(Guid id)
    {
        return await DbSet
            .Include(x => x.Carrier)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <inheritdoc />
    public async Task<Driver?> GetByIdForUpdateAsync(Guid id)
    {
        if (!Context.Database.IsNpgsql())
        {
            return await GetByIdAsync(id);
        }

        var lockedDrivers = DbSet.FromSqlInterpolated($"SELECT * FROM driver WHERE id = {id} FOR UPDATE");
        return await lockedDrivers
            .Include(x => x.Carrier)
            .FirstOrDefaultAsync(x => x.Id == id);
    }
}
