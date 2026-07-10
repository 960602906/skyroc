using Domain.Entities.Storage;
using Domain.Entities.Orders;
using Domain.Entities.Traceability;
using Domain.Interfaces;
using Domain.ReadModels.Traceability;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// 商品溯源记录仓储实现，以数据库投影读取销售出库批次、采购入库来源和检测报告关系。
/// </summary>
public class TraceRecordRepository(ApplicationDbContext context)
    : Repository<TraceRecord>(context), ITraceRecordRepository
{
    /// <inheritdoc />
    public override Task<TraceRecord?> GetByIdAsync(Guid id) => GetDetailByIdAsync(id);

    /// <inheritdoc />
    public Task<TraceRecord?> GetDetailByIdAsync(Guid id)
        => BuildDetailQuery().FirstOrDefaultAsync(x => x.Id == id);

    /// <inheritdoc />
    public Task<TraceRecord?> GetDetailByTraceNoAsync(string traceNo)
    {
        return BuildDetailQuery()
            .FirstOrDefaultAsync(x => x.TraceNo == traceNo.Trim());
    }

    private IQueryable<TraceRecord> BuildDetailQuery()
    {
        return DbSet.AsNoTracking()
            .Include(x => x.InspectionReport)
                .ThenInclude(x => x!.Goods)
            .Include(x => x.InspectionReport)
                .ThenInclude(x => x!.Attachments)
            .AsSplitQuery();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TraceRecord>> GetBySaleOrderIdAsync(Guid saleOrderId)
    {
        return await DbSet.AsNoTracking()
            .Where(x => x.SaleOrderId == saleOrderId)
            .OrderBy(x => x.SaleOrderDetailId)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<bool> LockSaleOrderAsync(Guid saleOrderId)
    {
        if (!Context.Database.IsNpgsql())
        {
            return await Context.Set<SaleOrder>().AnyAsync(x => x.Id == saleOrderId);
        }

        return await Context.Set<SaleOrder>().FromSqlInterpolated(
                $"SELECT * FROM sale_order WHERE id = {saleOrderId} FOR UPDATE")
            .AnyAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TraceGenerationSource>> GetGenerationSourcesAsync(Guid saleOrderId)
    {
        var stockOutOrders = await Context.Set<StockOutOrder>()
            .AsNoTracking()
            .Where(order => order.SaleOrderId == saleOrderId
                            && order.OrderType == StockOutOrderType.Sale
                            && order.BusinessStatus == StockDocumentStatus.Audited)
            .Include(order => order.SaleOrder)
            .Include(order => order.Details)
                .ThenInclude(detail => detail.SaleOrderDetail)
            .Include(order => order.Details)
                .ThenInclude(detail => detail.StockBatch)
            .ToListAsync();
        var outboundDetails = stockOutOrders.SelectMany(order => order.Details
                .Where(detail => detail.SaleOrderDetailId.HasValue && detail.StockBatchId.HasValue)
                .Select(detail => new { Order = order, Detail = detail }))
            .ToList();
        var batchIds = outboundDetails.Select(x => x.Detail.StockBatchId!.Value).Distinct().ToArray();
        if (batchIds.Length == 0) return [];

        var stockInDetailsByBatchId = (await Context.Set<StockInDetail>().AsNoTracking()
                .Where(detail => detail.StockBatchId.HasValue && batchIds.Contains(detail.StockBatchId.Value))
                .Include(detail => detail.StockInOrder)
                .ToListAsync())
            .Where(detail => detail.StockInOrder.OrderType == StockInOrderType.Purchase
                             && detail.StockInOrder.BusinessStatus == StockDocumentStatus.Audited)
            .GroupBy(detail => detail.StockBatchId!.Value)
            .ToDictionary(group => group.Key, group => group.ToList());
        var unambiguousStockInByBatchId = stockInDetailsByBatchId
            .Where(x => x.Value.Count == 1)
            .ToDictionary(x => x.Key, x => x.Value.Single());
        var stockInDetailIds = unambiguousStockInByBatchId.Values.Select(x => x.Id).ToArray();
        var reportByStockInDetailId = (await Context.Set<InspectionReportGoods>().AsNoTracking()
                .Where(goods => stockInDetailIds.Contains(goods.StockInDetailId))
                .Include(goods => goods.InspectionReport)
                .ToListAsync())
            .GroupBy(goods => goods.StockInDetailId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(goods => goods.InspectionReport.InspectTime).First().InspectionReportId);

        return outboundDetails
            .Where(x => x.Order.SaleOrder is not null)
            .Select(x =>
            {
                stockInDetailsByBatchId.TryGetValue(x.Detail.StockBatchId!.Value, out var sources);
                var stockIn = sources?.FirstOrDefault();
                var order = x.Order.SaleOrder!;
                Guid? inspectionReportId = null;
                if (stockIn is not null
                    && reportByStockInDetailId.TryGetValue(stockIn.Id, out var selectedReportId))
                {
                    inspectionReportId = selectedReportId;
                }
                return new TraceGenerationSource
                {
                    SaleOrderId = order.Id,
                    SaleOrderNo = order.OrderNo,
                    SaleOrderDetailId = x.Detail.SaleOrderDetailId!.Value,
                    CustomerId = order.CustomerId,
                    CustomerName = order.CustomerNameSnapshot,
                    GoodsId = x.Detail.GoodsId,
                    GoodsName = x.Detail.GoodsNameSnapshot,
                    GoodsCode = x.Detail.GoodsCodeSnapshot,
                    GoodsTypeName = x.Detail.SaleOrderDetail?.GoodsTypeNameSnapshot,
                    StockInDetailId = stockIn?.Id ?? Guid.Empty,
                    StockInSourceCount = sources?.Count ?? 0,
                    SupplierId = stockIn?.StockInOrder.SupplierId,
                    SupplierName = stockIn?.StockInOrder.SupplierNameSnapshot,
                    WareId = stockIn?.StockInOrder.WareId ?? Guid.Empty,
                    WareName = stockIn?.StockInOrder.WareNameSnapshot ?? string.Empty,
                    BatchNo = x.Detail.StockBatch!.BatchNo,
                    InspectionReportId = inspectionReportId
                };
            })
            .ToList();
    }

    /// <inheritdoc />
    public Task<bool> ExistsByInspectionReportIdAsync(Guid inspectionReportId)
    {
        return DbSet.AnyAsync(x => x.InspectionReportId == inspectionReportId);
    }
}
