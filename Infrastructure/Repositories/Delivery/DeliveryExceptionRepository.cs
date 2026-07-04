using Domain.Entities.Delivery;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
///     配送异常仓储。
/// </summary>
public class DeliveryExceptionRepository(ApplicationDbContext context)
    : Repository<DeliveryException>(context), IDeliveryExceptionRepository
{
    /// <inheritdoc />
    public async Task<bool> ExistsByExceptionNoAsync(string exceptionNo, Guid? excludeId = null)
    {
        var normalizedExceptionNo = exceptionNo.Trim();
        var query = DbSet.Where(x => x.ExceptionNo == normalizedExceptionNo);
        if (excludeId.HasValue)
        {
            query = query.Where(x => x.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }
}
