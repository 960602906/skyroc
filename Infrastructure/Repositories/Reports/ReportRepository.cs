using Domain.Entities.AfterSales;
using Domain.Entities.Orders;
using Domain.Interfaces;
using Domain.ReadModels.Reports;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Infrastructure.Repositories;

/// <summary>
/// 报表仓储实现，通过 EF Core 只读投影汇总销售订单和售后商品。
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
}
