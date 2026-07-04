using Domain.Entities.Orders;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// 订单签收回单仓储，负责回单与验收明细聚合读取及整单回单状态查询。
/// </summary>
public class OrderReceiptRepository(ApplicationDbContext context)
    : Repository<OrderReceipt>(context), IOrderReceiptRepository
{
    /// <inheritdoc />
    public override Task UpdateAsync(OrderReceipt entity)
    {
        Context.Entry(entity).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override async Task<OrderReceipt?> GetByIdAsync(Guid id)
    {
        return await BuildDetailQuery().FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <inheritdoc />
    public async Task<OrderReceipt?> GetByDeliveryTaskIdAsync(Guid deliveryTaskId)
    {
        return await BuildDetailQuery().FirstOrDefaultAsync(x => x.DeliveryTaskId == deliveryTaskId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OrderCheckDetail>> GetCheckDetailsBySaleOrderAsync(Guid saleOrderId)
    {
        return await Context.Set<OrderCheckDetail>()
            .AsNoTracking()
            .Where(x => x.OrderReceipt.SaleOrderId == saleOrderId)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<bool> HasUnreturnedReceiptsAsync(Guid saleOrderId, Guid excludeReceiptId)
    {
        return await DbSet.AnyAsync(x => x.SaleOrderId == saleOrderId
                                         && x.Id != excludeReceiptId
                                         && x.ReturnedTime == null);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsReceiptNoAsync(string receiptNo)
    {
        var normalized = receiptNo.Trim();
        return await DbSet.AnyAsync(x => x.ReceiptNo == normalized);
    }

    /// <summary>
    /// 构建包含配送任务、销售订单、出库单和商品验收明细的回单查询。
    /// </summary>
    /// <returns>签收回单完整聚合查询。</returns>
    private IQueryable<OrderReceipt> BuildDetailQuery()
    {
        return DbSet
            .Include(x => x.DeliveryTask)
            .Include(x => x.SaleOrder)
            .Include(x => x.StockOutOrder)
            .Include(x => x.CheckDetails)
            .AsSplitQuery();
    }
}
