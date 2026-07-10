using Domain.Entities.Traceability;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// 检测报告仓储实现，聚合读取报告商品和附件，并在 PostgreSQL 事务中执行行级锁。
/// </summary>
public class InspectionReportRepository(ApplicationDbContext context)
    : Repository<InspectionReport>(context), IInspectionReportRepository
{
    /// <inheritdoc />
    public override Task<InspectionReport?> GetByIdAsync(Guid id) => GetDetailByIdAsync(id);

    /// <inheritdoc />
    public Task<InspectionReport?> GetDetailByIdAsync(Guid id) =>
        BuildDetailQuery().FirstOrDefaultAsync(x => x.Id == id);

    /// <inheritdoc />
    public Task<InspectionReport?> GetDetailByIdForUpdateAsync(Guid id)
    {
        if (!Context.Database.IsNpgsql())
        {
            return GetDetailByIdAsync(id);
        }

        var lockedReports = DbSet.FromSqlInterpolated(
            $"SELECT * FROM inspection_report WHERE id = {id} FOR UPDATE");
        return BuildDetailQuery(lockedReports).FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <inheritdoc />
    public Task<bool> ExistsInspectionNoAsync(string inspectionNo, Guid? excludeId = null)
    {
        var normalizedNo = inspectionNo.Trim();
        return DbSet.AnyAsync(x => x.InspectionNo == normalizedNo && (!excludeId.HasValue || x.Id != excludeId.Value));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> LockByIdsAsync(IReadOnlyCollection<Guid> ids)
    {
        var orderedIds = ids.Distinct().OrderBy(x => x).ToArray();
        if (orderedIds.Length == 0) return [];
        if (!Context.Database.IsNpgsql())
        {
            return await DbSet.Where(x => orderedIds.Contains(x.Id)).Select(x => x.Id).ToListAsync();
        }
        return await DbSet.FromSqlInterpolated(
                $"SELECT * FROM inspection_report WHERE id = ANY({orderedIds}) ORDER BY id FOR UPDATE")
            .Select(x => x.Id)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<(Guid InspectionReportId, Guid StockInDetailId)>> GetLockedGoodsSourceIdsAsync(
        IReadOnlyCollection<Guid> ids)
    {
        var orderedIds = ids.Distinct().OrderBy(x => x).ToArray();
        if (orderedIds.Length == 0) return [];
        var rows = await Context.Set<InspectionReportGoods>().AsNoTracking()
            .Where(x => orderedIds.Contains(x.InspectionReportId))
            .Select(x => new { x.InspectionReportId, x.StockInDetailId })
            .ToListAsync();
        return rows.Select(x => (x.InspectionReportId, x.StockInDetailId)).ToList();
    }

    private IQueryable<InspectionReport> BuildDetailQuery(IQueryable<InspectionReport>? source = null)
    {
        return (source ?? DbSet)
            .Include(x => x.Goods)
            .Include(x => x.Attachments)
            .AsSplitQuery();
    }
}
