using System.Linq.Expressions;
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
    public override async Task<DeliveryException?> GetByIdAsync(Guid id)
    {
        return await BuildDetailQuery().FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <inheritdoc />
    public async Task<DeliveryException?> GetByIdForUpdateAsync(Guid id)
    {
        if (!Context.Database.IsNpgsql())
        {
            return await GetByIdAsync(id);
        }

        var lockedExceptions = DbSet.FromSqlInterpolated(
            $"SELECT * FROM delivery_exception WHERE id = {id} FOR UPDATE");
        return await BuildDetailQuery(lockedExceptions).FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <inheritdoc />
    public async Task<bool> HasPendingExceptionsAsync(Guid deliveryTaskId, Guid excludeExceptionId)
    {
        return await DbSet.AnyAsync(x => x.DeliveryTaskId == deliveryTaskId
                                         && x.Id != excludeExceptionId
                                         && x.HandleStatus == DeliveryExceptionStatus.Pending);
    }

    /// <inheritdoc />
    public override async Task<(IEnumerable<DeliveryException> Data, int Total)> GetPagedAsync(
        Expression<Func<DeliveryException, bool>>? predicate,
        int pageNumber,
        int pageSize,
        Expression<Func<DeliveryException, object>>? orderBy = null,
        bool isDescending = false)
    {
        return await PagedFromQueryAsync(
            BuildDetailQuery().AsNoTracking(),
            predicate,
            pageNumber,
            pageSize,
            orderBy,
            isDescending);
    }

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

    /// <summary>
    /// 构建包含配送任务、司机和客户导航的配送异常查询。
    /// </summary>
    /// <returns>配送异常完整查询。</returns>
    private IQueryable<DeliveryException> BuildDetailQuery(IQueryable<DeliveryException>? source = null)
    {
        return (source ?? DbSet)
            .Include(x => x.DeliveryTask)
            .Include(x => x.Driver)
            .Include(x => x.Customer);
    }
}
