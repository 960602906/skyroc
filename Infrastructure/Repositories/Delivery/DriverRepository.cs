using Domain.Entities.Delivery;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
///     司机仓储，按主键读取时预加载所属承运商。
/// </summary>
public class DriverRepository(ApplicationDbContext context)
    : NamedCodeRepository<Driver>(context), IDriverRepository
{
    /// <inheritdoc />
    public override async Task<Driver?> GetByIdAsync(Guid id)
    {
        return await DbSet
            .Include(x => x.Carrier)
            .FirstOrDefaultAsync(x => x.Id == id);
    }
}
