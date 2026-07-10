using Domain.Entities.AfterSales;
using Domain.Entities.Orders;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using Domain.Interfaces;
using Domain.ReadModels.Reports;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Infrastructure.Repositories;

/// <summary>
/// 报表仓储实现，通过 EF Core 只读投影汇总销售、售后、库存和采购业务数据。
/// </summary>
public class ReportRepository(ApplicationDbContext context) : IReportRepository
{
    // 已签收订单的验收口径：签收写回总会持久化 CustomerCheck*；缺失时按 0，
    // 不回退到下单数量/金额，避免把未验收行计入销售汇总。
    // 补货/换货/客户沟通不产生退款或减免，退款数量按 0 计入，金额本身已为 0。

    /// <inheritdoc />
    public async Task<PagedResult<SalesGoodsSummaryReadModel>> GetSalesGoodsSummaryAsync(
        SalesReportFilter filter,
        int current,
        int size)
    {
        var query = ApplySalesFilter(filter)
            .GroupBy(x => new
            {
                x.GoodsId,
                x.GoodsNameSnapshot,
                x.GoodsCodeSnapshot,
                GoodsTypeName = x.GoodsTypeNameSnapshot ?? "未分类",
                x.BaseUnitNameSnapshot
            })
            .Select(g => new SalesGoodsSummaryReadModel
            {
                GoodsId = g.Key.GoodsId,
                GoodsName = g.Key.GoodsNameSnapshot,
                GoodsCode = g.Key.GoodsCodeSnapshot,
                GoodsTypeName = g.Key.GoodsTypeName,
                BaseUnitName = g.Key.BaseUnitNameSnapshot,
                SaleBaseQuantity = g.Sum(x => x.CustomerCheckBaseQuantity ?? 0m),
                SaleAmount = g.Sum(x => x.CustomerCheckPrice ?? 0m),
                OrderCount = g.Select(x => x.SaleOrderId).Distinct().Count(),
                CustomerCount = g.Select(x => x.SaleOrder.CustomerId).Distinct().Count()
            })
            .OrderByDescending(x => x.SaleAmount)
            .ThenBy(x => x.GoodsName);

        return await ToPagedResultAsync(query, current, size);
    }

    /// <inheritdoc />
    public async Task<PagedResult<SalesCategorySummaryReadModel>> GetSalesCategorySummaryAsync(
        SalesReportFilter filter,
        int current,
        int size)
    {
        var query = ApplySalesFilter(filter)
            .GroupBy(x => x.GoodsTypeNameSnapshot ?? "未分类")
            .Select(g => new SalesCategorySummaryReadModel
            {
                GoodsTypeName = g.Key,
                SaleBaseQuantity = g.Sum(x => x.CustomerCheckBaseQuantity ?? 0m),
                SaleAmount = g.Sum(x => x.CustomerCheckPrice ?? 0m),
                OrderCount = g.Select(x => x.SaleOrderId).Distinct().Count(),
                CustomerCount = g.Select(x => x.SaleOrder.CustomerId).Distinct().Count()
            })
            .OrderByDescending(x => x.SaleAmount)
            .ThenBy(x => x.GoodsTypeName);

        return await ToPagedResultAsync(query, current, size);
    }

    /// <inheritdoc />
    public async Task<PagedResult<SalesCustomerSummaryReadModel>> GetSalesCustomerSummaryAsync(
        SalesReportFilter filter,
        int current,
        int size)
    {
        var query = ApplySalesFilter(filter)
            .GroupBy(x => new
            {
                x.SaleOrder.CustomerId,
                x.SaleOrder.CustomerNameSnapshot,
                x.SaleOrder.CustomerCodeSnapshot
            })
            .Select(g => new SalesCustomerSummaryReadModel
            {
                CustomerId = g.Key.CustomerId,
                CustomerName = g.Key.CustomerNameSnapshot,
                CustomerCode = g.Key.CustomerCodeSnapshot,
                SaleBaseQuantity = g.Sum(x => x.CustomerCheckBaseQuantity ?? 0m),
                SaleAmount = g.Sum(x => x.CustomerCheckPrice ?? 0m),
                OrderCount = g.Select(x => x.SaleOrderId).Distinct().Count(),
                GoodsCount = g.Select(x => x.GoodsId).Distinct().Count()
            })
            .OrderByDescending(x => x.SaleAmount)
            .ThenBy(x => x.CustomerName);

        return await ToPagedResultAsync(query, current, size);
    }

    /// <inheritdoc />
    public async Task<PagedResult<SalesAreaSummaryReadModel>> GetSalesAreaSummaryAsync(
        SalesReportFilter filter,
        int current,
        int size)
    {
        var query = ApplySalesFilter(filter)
            .GroupBy(x => x.SaleOrder.DeliveryAddressSnapshot ?? "未填写区域")
            .Select(g => new SalesAreaSummaryReadModel
            {
                AreaName = g.Key,
                SaleBaseQuantity = g.Sum(x => x.CustomerCheckBaseQuantity ?? 0m),
                SaleAmount = g.Sum(x => x.CustomerCheckPrice ?? 0m),
                OrderCount = g.Select(x => x.SaleOrderId).Distinct().Count(),
                CustomerCount = g.Select(x => x.SaleOrder.CustomerId).Distinct().Count()
            })
            .OrderByDescending(x => x.SaleAmount)
            .ThenBy(x => x.AreaName);

        return await ToPagedResultAsync(query, current, size);
    }

    /// <inheritdoc />
    public async Task<PagedResult<AfterSaleSummaryReadModel>> GetAfterSaleSummaryAsync(
        AfterSaleReportFilter filter,
        int current,
        int size)
    {
        var query = ApplyAfterSaleFilter(filter)
            .GroupBy(x => new
            {
                x.AfterSaleType,
                x.ReasonType,
                x.HandleType
            })
            .Select(g => new AfterSaleSummaryReadModel
            {
                AfterSaleType = g.Key.AfterSaleType,
                ReasonType = g.Key.ReasonType,
                HandleType = g.Key.HandleType,
                // 与售后写路径 RequiresFinancialAdjustment 对齐：补货/换货/客户沟通不计入退款减免数量。
                RefundBaseQuantity = g.Sum(x =>
                    x.HandleType == AfterSaleHandleType.Replenishment
                    || x.HandleType == AfterSaleHandleType.Exchange
                    || x.HandleType == AfterSaleHandleType.CustomerCommunication
                        ? 0m
                        : x.BaseRefundQuantity),
                RefundAmount = g.Sum(x => x.RefundAmount),
                AfterSaleCount = g.Select(x => x.AfterSaleId).Distinct().Count(),
                CustomerCount = g.Select(x => x.AfterSale.CustomerId).Distinct().Count()
            })
            .OrderByDescending(x => x.RefundAmount)
            .ThenBy(x => x.ReasonType)
            .ThenBy(x => x.HandleType);

        return await ToPagedResultAsync(query, current, size);
    }

    /// <inheritdoc />
    public async Task<PagedResult<DailyStockInOutSummaryReadModel>> GetDailyStockInOutSummaryAsync(
        StockReportFilter filter,
        int current,
        int size)
    {
        var inbound = ApplyStockInFilter(filter)
            .GroupBy(x => x.StockInOrder.InTime.Date)
            .Select(g => new DailyStockInOutSummaryReadModel
            {
                ReportDate = g.Key,
                InBaseQuantity = g.Sum(x => x.BaseQuantity),
                InAmount = g.Sum(x => x.TotalPrice),
                InOrderCount = g.Select(x => x.StockInOrderId).Distinct().Count(),
                OutBaseQuantity = 0m,
                OutAmount = 0m,
                OutOrderCount = 0
            });

        var outbound = ApplyStockOutFilter(filter)
            .GroupBy(x => x.StockOutOrder.OutTime.Date)
            .Select(g => new DailyStockInOutSummaryReadModel
            {
                ReportDate = g.Key,
                OutBaseQuantity = g.Sum(x => x.BaseQuantity),
                OutAmount = g.Sum(x => x.TotalPrice),
                OutOrderCount = g.Select(x => x.StockOutOrderId).Distinct().Count(),
                InBaseQuantity = 0m,
                InAmount = 0m,
                InOrderCount = 0
            });

        var query = inbound.Concat(outbound)
            .GroupBy(x => x.ReportDate)
            .Select(g => new DailyStockInOutSummaryReadModel
            {
                ReportDate = g.Key,
                InBaseQuantity = g.Sum(x => x.InBaseQuantity),
                InAmount = g.Sum(x => x.InAmount),
                OutBaseQuantity = g.Sum(x => x.OutBaseQuantity),
                OutAmount = g.Sum(x => x.OutAmount),
                InOrderCount = g.Sum(x => x.InOrderCount),
                OutOrderCount = g.Sum(x => x.OutOrderCount)
            })
            .OrderByDescending(x => x.ReportDate);

        return await ToPagedResultAsync(query, current, size);
    }

    /// <inheritdoc />
    public async Task<PagedResult<DailyGoodsStockInOutSummaryReadModel>> GetDailyGoodsStockInOutSummaryAsync(
        StockReportFilter filter,
        int current,
        int size)
    {
        var inbound = ApplyStockInFilter(filter)
            .GroupBy(x => new
            {
                ReportDate = x.StockInOrder.InTime.Date,
                x.GoodsId,
                x.GoodsNameSnapshot,
                x.GoodsCodeSnapshot,
                UnitName = x.StockBatch == null ? x.GoodsUnitNameSnapshot : x.StockBatch.BaseUnitNameSnapshot
            })
            .Select(g => new DailyGoodsStockInOutSummaryReadModel
            {
                ReportDate = g.Key.ReportDate,
                GoodsId = g.Key.GoodsId,
                GoodsName = g.Key.GoodsNameSnapshot,
                GoodsCode = g.Key.GoodsCodeSnapshot,
                BaseUnitName = g.Key.UnitName,
                InBaseQuantity = g.Sum(x => x.BaseQuantity),
                InAmount = g.Sum(x => x.TotalPrice),
                OutBaseQuantity = 0m,
                OutAmount = 0m
            });

        var outbound = ApplyStockOutFilter(filter)
            .GroupBy(x => new
            {
                ReportDate = x.StockOutOrder.OutTime.Date,
                x.GoodsId,
                x.GoodsNameSnapshot,
                x.GoodsCodeSnapshot,
                UnitName = x.StockBatch == null ? x.GoodsUnitNameSnapshot : x.StockBatch.BaseUnitNameSnapshot
            })
            .Select(g => new DailyGoodsStockInOutSummaryReadModel
            {
                ReportDate = g.Key.ReportDate,
                GoodsId = g.Key.GoodsId,
                GoodsName = g.Key.GoodsNameSnapshot,
                GoodsCode = g.Key.GoodsCodeSnapshot,
                BaseUnitName = g.Key.UnitName,
                OutBaseQuantity = g.Sum(x => x.BaseQuantity),
                OutAmount = g.Sum(x => x.TotalPrice),
                InBaseQuantity = 0m,
                InAmount = 0m
            });

        var query = inbound.Concat(outbound)
            .GroupBy(x => new { x.ReportDate, x.GoodsId })
            .Select(g => new DailyGoodsStockInOutSummaryReadModel
            {
                ReportDate = g.Key.ReportDate,
                GoodsId = g.Key.GoodsId,
                GoodsName = g.First().GoodsName,
                GoodsCode = g.First().GoodsCode,
                BaseUnitName = g.First().BaseUnitName,
                InBaseQuantity = g.Sum(x => x.InBaseQuantity),
                InAmount = g.Sum(x => x.InAmount),
                OutBaseQuantity = g.Sum(x => x.OutBaseQuantity),
                OutAmount = g.Sum(x => x.OutAmount)
            })
            .OrderByDescending(x => x.ReportDate)
            .ThenBy(x => x.GoodsName);

        return await ToPagedResultAsync(query, current, size);
    }

    /// <inheritdoc />
    public async Task<PagedResult<PurchaseInOutGoodsSummaryReadModel>> GetPurchaseInOutGoodsSummaryAsync(
        PurchaseInOutReportFilter filter,
        int current,
        int size)
    {
        var inbound = ApplyPurchaseInFilter(filter)
            .GroupBy(x => new
            {
                x.GoodsId,
                x.GoodsNameSnapshot,
                x.GoodsCodeSnapshot,
                UnitName = x.StockBatch == null ? x.GoodsUnitNameSnapshot : x.StockBatch.BaseUnitNameSnapshot
            })
            .Select(g => new PurchaseInOutGoodsSummaryReadModel
            {
                GoodsId = g.Key.GoodsId,
                GoodsName = g.Key.GoodsNameSnapshot,
                GoodsCode = g.Key.GoodsCodeSnapshot,
                BaseUnitName = g.Key.UnitName,
                InBaseQuantity = g.Sum(x => x.BaseQuantity),
                InAmount = g.Sum(x => x.TotalPrice),
                OutBaseQuantity = 0m,
                OutAmount = 0m
            });

        var outbound = ApplyPurchaseOutFilter(filter)
            .GroupBy(x => new
            {
                x.GoodsId,
                x.GoodsNameSnapshot,
                x.GoodsCodeSnapshot,
                UnitName = x.StockBatch == null ? x.GoodsUnitNameSnapshot : x.StockBatch.BaseUnitNameSnapshot
            })
            .Select(g => new PurchaseInOutGoodsSummaryReadModel
            {
                GoodsId = g.Key.GoodsId,
                GoodsName = g.Key.GoodsNameSnapshot,
                GoodsCode = g.Key.GoodsCodeSnapshot,
                BaseUnitName = g.Key.UnitName,
                OutBaseQuantity = g.Sum(x => x.BaseQuantity),
                OutAmount = g.Sum(x => x.TotalPrice),
                InBaseQuantity = 0m,
                InAmount = 0m
            });

        var query = inbound.Concat(outbound)
            .GroupBy(x => x.GoodsId)
            .Select(g => new PurchaseInOutGoodsSummaryReadModel
            {
                GoodsId = g.Key,
                GoodsName = g.First().GoodsName,
                GoodsCode = g.First().GoodsCode,
                BaseUnitName = g.First().BaseUnitName,
                InBaseQuantity = g.Sum(x => x.InBaseQuantity),
                InAmount = g.Sum(x => x.InAmount),
                OutBaseQuantity = g.Sum(x => x.OutBaseQuantity),
                OutAmount = g.Sum(x => x.OutAmount)
            })
            .OrderByDescending(x => x.InAmount - x.OutAmount)
            .ThenBy(x => x.GoodsName);

        return await ToPagedResultAsync(query, current, size);
    }

    /// <inheritdoc />
    public async Task<PagedResult<PurchaseInOutSupplierSummaryReadModel>> GetPurchaseInOutSupplierSummaryAsync(
        PurchaseInOutReportFilter filter,
        int current,
        int size)
    {
        var inbound = ApplyPurchaseInFilter(filter)
            .GroupBy(x => new { x.StockInOrder.SupplierId, x.StockInOrder.SupplierNameSnapshot })
            .Select(g => new PurchaseInOutSupplierSummaryReadModel
            {
                SupplierId = g.Key.SupplierId,
                SupplierName = g.Key.SupplierNameSnapshot ?? "未指定供应商",
                InBaseQuantity = g.Sum(x => x.BaseQuantity),
                InAmount = g.Sum(x => x.TotalPrice),
                OutBaseQuantity = 0m,
                OutAmount = 0m
            });

        var outbound = ApplyPurchaseOutFilter(filter)
            .GroupBy(x => new { x.StockOutOrder.SupplierId, x.StockOutOrder.SupplierNameSnapshot })
            .Select(g => new PurchaseInOutSupplierSummaryReadModel
            {
                SupplierId = g.Key.SupplierId,
                SupplierName = g.Key.SupplierNameSnapshot ?? "未指定供应商",
                OutBaseQuantity = g.Sum(x => x.BaseQuantity),
                OutAmount = g.Sum(x => x.TotalPrice),
                InBaseQuantity = 0m,
                InAmount = 0m
            });

        var query = inbound.Concat(outbound)
            .GroupBy(x => x.SupplierId)
            .Select(g => new PurchaseInOutSupplierSummaryReadModel
            {
                SupplierId = g.Key,
                SupplierName = g.First().SupplierName,
                InBaseQuantity = g.Sum(x => x.InBaseQuantity),
                InAmount = g.Sum(x => x.InAmount),
                OutBaseQuantity = g.Sum(x => x.OutBaseQuantity),
                OutAmount = g.Sum(x => x.OutAmount)
            })
            .OrderByDescending(x => x.InAmount - x.OutAmount)
            .ThenBy(x => x.SupplierName);

        return await ToPagedResultAsync(query, current, size);
    }

    /// <inheritdoc />
    public async Task<PagedResult<PurchaseInOutPurchaserSummaryReadModel>> GetPurchaseInOutPurchaserSummaryAsync(
        PurchaseInOutReportFilter filter,
        int current,
        int size)
    {
        var inbound = ApplyPurchaseInFilter(filter)
            .GroupBy(x => new { x.StockInOrder.PurchaserId, x.StockInOrder.PurchaserNameSnapshot })
            .Select(g => new PurchaseInOutPurchaserSummaryReadModel
            {
                PurchaserId = g.Key.PurchaserId,
                PurchaserName = g.Key.PurchaserNameSnapshot ?? "未指定采购员",
                InBaseQuantity = g.Sum(x => x.BaseQuantity),
                InAmount = g.Sum(x => x.TotalPrice),
                OutBaseQuantity = 0m,
                OutAmount = 0m
            });

        var outbound = GetPurchaseOutByPurchaser(filter);

        var query = inbound.Concat(outbound)
            .GroupBy(x => x.PurchaserId)
            .Select(g => new PurchaseInOutPurchaserSummaryReadModel
            {
                PurchaserId = g.Key,
                PurchaserName = g.First().PurchaserName,
                InBaseQuantity = g.Sum(x => x.InBaseQuantity),
                InAmount = g.Sum(x => x.InAmount),
                OutBaseQuantity = g.Sum(x => x.OutBaseQuantity),
                OutAmount = g.Sum(x => x.OutAmount)
            })
            .OrderByDescending(x => x.InAmount - x.OutAmount)
            .ThenBy(x => x.PurchaserName);

        return await ToPagedResultAsync(query, current, size);
    }

    private IQueryable<SaleOrderDetail> ApplySalesFilter(SalesReportFilter filter)
    {
        var query = context.SaleOrderDetails
            .AsNoTracking()
            .Where(x => x.SaleOrder.OrderStatus == SaleOrderStatus.Signed);

        if (filter.DateStart.HasValue)
        {
            query = query.Where(x => x.SaleOrder.OrderDate >= filter.DateStart.Value);
        }

        if (filter.DateEnd.HasValue)
        {
            query = query.Where(x => x.SaleOrder.OrderDate <= filter.DateEnd.Value);
        }

        if (filter.CustomerId.HasValue)
        {
            query = query.Where(x => x.SaleOrder.CustomerId == filter.CustomerId.Value);
        }

        if (filter.CustomerTagIds.Count > 0)
        {
            query = query.Where(x => x.SaleOrder.Customer.TagRelations.Any(
                relation => filter.CustomerTagIds.Contains(relation.CustomerTagId)));
        }

        if (filter.GoodsTypeIds.Count > 0)
        {
            // 明细仅有分类名称快照，按所选分类当前名称匹配历史快照，与分组展示口径一致。
            query = query.Where(x => context.GoodsTypes.Any(
                type => filter.GoodsTypeIds.Contains(type.Id)
                        && type.Name == x.GoodsTypeNameSnapshot));
        }

        if (filter.GoodsIds.Count > 0)
        {
            query = query.Where(x => filter.GoodsIds.Contains(x.GoodsId));
        }

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            query = query.Where(x => x.SaleOrder.OrderNo.Contains(filter.Keyword)
                                     || x.SaleOrder.CustomerNameSnapshot.Contains(filter.Keyword)
                                     || x.SaleOrder.CustomerCodeSnapshot.Contains(filter.Keyword)
                                     || x.GoodsNameSnapshot.Contains(filter.Keyword)
                                     || x.GoodsCodeSnapshot.Contains(filter.Keyword));
        }

        if (!string.IsNullOrWhiteSpace(filter.AreaKeyword))
        {
            query = query.Where(x => x.SaleOrder.DeliveryAddressSnapshot != null
                                     && x.SaleOrder.DeliveryAddressSnapshot.Contains(filter.AreaKeyword));
        }

        return query;
    }

    private IQueryable<StockInDetail> ApplyStockInFilter(StockReportFilter filter)
    {
        var query = context.StockInDetails
            .AsNoTracking()
            .Where(x => x.StockInOrder.BusinessStatus == StockDocumentStatus.Audited);

        if (filter.DateStart.HasValue)
        {
            query = query.Where(x => x.StockInOrder.InTime >= filter.DateStart.Value);
        }

        if (filter.DateEnd.HasValue)
        {
            query = query.Where(x => x.StockInOrder.InTime <= filter.DateEnd.Value);
        }

        if (filter.WareId.HasValue)
        {
            query = query.Where(x => x.StockInOrder.WareId == filter.WareId.Value);
        }

        if (filter.GoodsIds.Count > 0)
        {
            query = query.Where(x => filter.GoodsIds.Contains(x.GoodsId));
        }

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            query = query.Where(x => x.StockInOrder.WareNameSnapshot.Contains(filter.Keyword)
                                     || x.GoodsNameSnapshot.Contains(filter.Keyword)
                                     || x.GoodsCodeSnapshot.Contains(filter.Keyword));
        }

        return query;
    }

    private IQueryable<StockOutDetail> ApplyStockOutFilter(StockReportFilter filter)
    {
        var query = context.StockOutDetails
            .AsNoTracking()
            .Where(x => x.StockOutOrder.BusinessStatus == StockDocumentStatus.Audited);

        if (filter.DateStart.HasValue)
        {
            query = query.Where(x => x.StockOutOrder.OutTime >= filter.DateStart.Value);
        }

        if (filter.DateEnd.HasValue)
        {
            query = query.Where(x => x.StockOutOrder.OutTime <= filter.DateEnd.Value);
        }

        if (filter.WareId.HasValue)
        {
            query = query.Where(x => x.StockOutOrder.WareId == filter.WareId.Value);
        }

        if (filter.GoodsIds.Count > 0)
        {
            query = query.Where(x => filter.GoodsIds.Contains(x.GoodsId));
        }

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            query = query.Where(x => x.StockOutOrder.WareNameSnapshot.Contains(filter.Keyword)
                                     || x.GoodsNameSnapshot.Contains(filter.Keyword)
                                     || x.GoodsCodeSnapshot.Contains(filter.Keyword));
        }

        return query;
    }

    private IQueryable<StockInDetail> ApplyPurchaseInFilter(PurchaseInOutReportFilter filter)
    {
        var query = context.StockInDetails
            .AsNoTracking()
            .Where(x => x.StockInOrder.BusinessStatus == StockDocumentStatus.Audited
                        && x.StockInOrder.OrderType == StockInOrderType.Purchase);

        if (filter.DateStart.HasValue)
        {
            query = query.Where(x => x.StockInOrder.InTime >= filter.DateStart.Value);
        }

        if (filter.DateEnd.HasValue)
        {
            query = query.Where(x => x.StockInOrder.InTime <= filter.DateEnd.Value);
        }

        if (filter.WareId.HasValue)
        {
            query = query.Where(x => x.StockInOrder.WareId == filter.WareId.Value);
        }

        if (filter.SupplierId.HasValue)
        {
            query = query.Where(x => x.StockInOrder.SupplierId == filter.SupplierId.Value);
        }

        if (filter.PurchaserId.HasValue)
        {
            query = query.Where(x => x.StockInOrder.PurchaserId == filter.PurchaserId.Value);
        }

        if (filter.PurchasePattern.HasValue)
        {
            query = query.Where(x => x.StockInOrder.PurchasePattern == filter.PurchasePattern.Value);
        }

        if (filter.GoodsIds.Count > 0)
        {
            query = query.Where(x => filter.GoodsIds.Contains(x.GoodsId));
        }

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            query = query.Where(x => x.StockInOrder.InNo.Contains(filter.Keyword)
                                     || (x.StockInOrder.SupplierNameSnapshot != null
                                         && x.StockInOrder.SupplierNameSnapshot.Contains(filter.Keyword))
                                     || (x.StockInOrder.PurchaserNameSnapshot != null
                                         && x.StockInOrder.PurchaserNameSnapshot.Contains(filter.Keyword))
                                     || x.GoodsNameSnapshot.Contains(filter.Keyword)
                                     || x.GoodsCodeSnapshot.Contains(filter.Keyword));
        }

        return query;
    }

    private IQueryable<StockOutDetail> ApplyPurchaseOutFilter(PurchaseInOutReportFilter filter)
    {
        var query = context.StockOutDetails
            .AsNoTracking()
            .Where(x => x.StockOutOrder.BusinessStatus == StockDocumentStatus.Audited
                        && x.StockOutOrder.OrderType == StockOutOrderType.PurchaseReturn);

        if (filter.DateStart.HasValue)
        {
            query = query.Where(x => x.StockOutOrder.OutTime >= filter.DateStart.Value);
        }

        if (filter.DateEnd.HasValue)
        {
            query = query.Where(x => x.StockOutOrder.OutTime <= filter.DateEnd.Value);
        }

        if (filter.WareId.HasValue)
        {
            query = query.Where(x => x.StockOutOrder.WareId == filter.WareId.Value);
        }

        if (filter.SupplierId.HasValue)
        {
            query = query.Where(x => x.StockOutOrder.SupplierId == filter.SupplierId.Value);
        }

        if (filter.PurchaserId.HasValue)
        {
            query = query.Where(x => x.StockBatchId.HasValue
                                     && context.StockInDetails
                                         .Where(source => source.StockBatchId == x.StockBatchId
                                                          && source.StockInOrder.OrderType == StockInOrderType.Purchase
                                                          && source.StockInOrder.BusinessStatus == StockDocumentStatus.Audited)
                                         .OrderBy(source => source.StockInOrder.InTime)
                                         .Select(source => source.StockInOrder.PurchaserId)
                                         .FirstOrDefault() == filter.PurchaserId.Value);
        }

        if (filter.PurchasePattern.HasValue)
        {
            query = query.Where(x => x.StockBatchId.HasValue
                                     && context.StockInDetails
                                         .Where(source => source.StockBatchId == x.StockBatchId
                                                          && source.StockInOrder.OrderType == StockInOrderType.Purchase
                                                          && source.StockInOrder.BusinessStatus == StockDocumentStatus.Audited)
                                         .OrderBy(source => source.StockInOrder.InTime)
                                         .Select(source => (PurchasePattern?)source.StockInOrder.PurchasePattern)
                                         .FirstOrDefault() == filter.PurchasePattern.Value);
        }

        if (filter.GoodsIds.Count > 0)
        {
            query = query.Where(x => filter.GoodsIds.Contains(x.GoodsId));
        }

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            query = query.Where(x => x.StockOutOrder.OutNo.Contains(filter.Keyword)
                                     || (x.StockOutOrder.SupplierNameSnapshot != null
                                         && x.StockOutOrder.SupplierNameSnapshot.Contains(filter.Keyword))
                                     || x.GoodsNameSnapshot.Contains(filter.Keyword)
                                     || x.GoodsCodeSnapshot.Contains(filter.Keyword));
        }

        return query;
    }

    private IQueryable<PurchaseInOutPurchaserSummaryReadModel> GetPurchaseOutByPurchaser(
        PurchaseInOutReportFilter filter)
    {
        return ApplyPurchaseOutFilter(filter)
            .Select(x => new
            {
                Detail = x,
                Source = context.StockInDetails
                    .Where(source => source.StockBatchId == x.StockBatchId
                                     && x.StockBatchId.HasValue
                                     && source.StockInOrder.OrderType == StockInOrderType.Purchase
                                     && source.StockInOrder.BusinessStatus == StockDocumentStatus.Audited)
                    .OrderBy(source => source.StockInOrder.InTime)
                    .Select(source => new
                    {
                        source.StockInOrder.PurchaserId,
                        source.StockInOrder.PurchaserNameSnapshot
                    })
                    .FirstOrDefault()
            })
            .Where(x => x.Source != null)
            .GroupBy(x => new { x.Source!.PurchaserId, x.Source.PurchaserNameSnapshot })
            .Select(g => new PurchaseInOutPurchaserSummaryReadModel
            {
                PurchaserId = g.Key.PurchaserId,
                PurchaserName = g.Key.PurchaserNameSnapshot ?? "未指定采购员",
                OutBaseQuantity = g.Sum(x => x.Detail.BaseQuantity),
                OutAmount = g.Sum(x => x.Detail.TotalPrice),
                InBaseQuantity = 0m,
                InAmount = 0m
            });
    }

    private IQueryable<AfterSaleGoods> ApplyAfterSaleFilter(AfterSaleReportFilter filter)
    {
        var query = context.AfterSaleGoods
            .AsNoTracking()
            .Where(x => x.AfterSale.AfterStatus == AfterSaleStatus.Completed);

        if (filter.DateStart.HasValue)
        {
            query = query.Where(x => x.AfterSale.CreateTime.HasValue
                                     && x.AfterSale.CreateTime.Value >= filter.DateStart.Value);
        }

        if (filter.DateEnd.HasValue)
        {
            query = query.Where(x => x.AfterSale.CreateTime.HasValue
                                     && x.AfterSale.CreateTime.Value <= filter.DateEnd.Value);
        }

        if (filter.CustomerId.HasValue)
        {
            query = query.Where(x => x.AfterSale.CustomerId == filter.CustomerId.Value);
        }

        if (filter.GoodsIds.Count > 0)
        {
            query = query.Where(x => filter.GoodsIds.Contains(x.GoodsId));
        }

        if (filter.ReasonType.HasValue)
        {
            query = query.Where(x => x.ReasonType == filter.ReasonType.Value);
        }

        if (filter.AfterSaleType.HasValue)
        {
            query = query.Where(x => x.AfterSaleType == filter.AfterSaleType.Value);
        }

        if (filter.HandleType.HasValue)
        {
            query = query.Where(x => x.HandleType == filter.HandleType.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            query = query.Where(x => x.AfterSale.AfterSaleNo.Contains(filter.Keyword)
                                     || x.AfterSale.CustomerNameSnapshot.Contains(filter.Keyword)
                                     || x.GoodsNameSnapshot.Contains(filter.Keyword)
                                     || x.GoodsCodeSnapshot.Contains(filter.Keyword));
        }

        return query;
    }

    private static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        IQueryable<T> query,
        int current,
        int size)
    {
        var total = await query.CountAsync();
        var records = await query
            .Skip((current - 1) * size)
            .Take(size)
            .ToListAsync();

        return new PagedResult<T>
        {
            Current = current,
            Size = size,
            Total = total,
            Records = records
        };
    }

    private static PagedResult<T> ToPagedResult<T>(
        IReadOnlyList<T> rows,
        int current,
        int size)
    {
        return new PagedResult<T>
        {
            Current = current,
            Size = size,
            Total = rows.Count,
            Records = rows
                .Skip((current - 1) * size)
                .Take(size)
                .ToList()
        };
    }
}
