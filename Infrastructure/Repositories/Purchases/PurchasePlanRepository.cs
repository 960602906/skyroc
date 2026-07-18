using System.Linq.Expressions;
using Domain.Entities.Purchases;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// 采购计划仓储，读取时聚合明细、采购单位和订单来源关系。
/// </summary>
public class PurchasePlanRepository(ApplicationDbContext context)
    : Repository<PurchasePlan>(context), IPurchasePlanRepository
{
    private readonly ApplicationDbContext _context = context;

    /// <inheritdoc />
    public override async Task<PurchasePlan?> GetByIdAsync(Guid id)
    {
        return await BuildDetailQuery().FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <inheritdoc />
    public override async Task<(IEnumerable<PurchasePlan> Data, int Total)> GetPagedAsync(
        Expression<Func<PurchasePlan, bool>>? predicate,
        int pageNumber,
        int pageSize,
        Expression<Func<PurchasePlan, object>>? orderBy = null,
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
    public async Task<bool> ExistsPlanNoAsync(string planNo, Guid? excludeId = null)
    {
        var normalizedPlanNo = planNo.Trim();
        return await DbSet.AnyAsync(x =>
            x.PlanNo == normalizedPlanNo
            && (!excludeId.HasValue || x.Id != excludeId.Value));
    }

    /// <inheritdoc />
    public async Task<PurchasePlanDetail?> GetDetailByIdAsync(Guid detailId)
    {
        return await _context.PurchasePlanDetails
            .Include(x => x.PurchasePlan)
                .ThenInclude(x => x.Details)
            .FirstOrDefaultAsync(x => x.Id == detailId);
    }

    /// <summary>
    /// 构建包含供应商、采购员、明细及订单来源关系的采购计划查询。
    /// </summary>
    /// <returns>预加载导航属性的可查询序列。</returns>
    private IQueryable<PurchasePlan> BuildDetailQuery()
    {
        return DbSet
            .Include(x => x.Supplier)
            .Include(x => x.Purchaser)
            .Include(x => x.Details)
                .ThenInclude(x => x.Goods)
            .Include(x => x.Details)
                .ThenInclude(x => x.PurchaseUnit)
            .Include(x => x.Details)
                .ThenInclude(x => x.OrderRelations)
                    .ThenInclude(x => x.SaleOrder)
            .AsSplitQuery();
    }
}
