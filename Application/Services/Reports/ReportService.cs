using Application.DTOs.Reports;
using Application.interfaces;
using Application.QueryParameters.Reports;
using Domain.Interfaces;
using Domain.ReadModels.Reports;
using Shared.Constants;

namespace Application.Services;

/// <summary>
/// 销售、售后、库存、采购报表与首页驾驶舱应用服务，负责筛选条件归一化和响应精度收口。
/// </summary>
public class ReportService(IReportRepository repository) : IReportService
{
    /// <inheritdoc />
    public async Task<PagedResult<SalesGoodsSummaryDto>> GetSalesGoodsSummaryAsync(
        SalesReportQueryParameters parameters)
    {
        var result = await repository.GetSalesGoodsSummaryAsync(ToFilter(parameters), parameters.Current, parameters.Size);
        return MapPage(result, x => new SalesGoodsSummaryDto
        {
            GoodsId = x.GoodsId,
            GoodsName = x.GoodsName,
            GoodsCode = x.GoodsCode,
            GoodsTypeName = x.GoodsTypeName,
            BaseUnitName = x.BaseUnitName,
            SaleBaseQuantity = NumericPrecision.RoundQuantity(x.SaleBaseQuantity),
            SaleAmount = NumericPrecision.RoundMoney(x.SaleAmount),
            OrderCount = x.OrderCount,
            CustomerCount = x.CustomerCount
        });
    }

    /// <inheritdoc />
    public async Task<PagedResult<SalesCategorySummaryDto>> GetSalesCategorySummaryAsync(
        SalesReportQueryParameters parameters)
    {
        var result = await repository.GetSalesCategorySummaryAsync(ToFilter(parameters), parameters.Current, parameters.Size);
        return MapPage(result, x => new SalesCategorySummaryDto
        {
            GoodsTypeName = x.GoodsTypeName,
            SaleBaseQuantity = NumericPrecision.RoundQuantity(x.SaleBaseQuantity),
            SaleAmount = NumericPrecision.RoundMoney(x.SaleAmount),
            OrderCount = x.OrderCount,
            CustomerCount = x.CustomerCount
        });
    }

    /// <inheritdoc />
    public async Task<PagedResult<SalesCustomerSummaryDto>> GetSalesCustomerSummaryAsync(
        SalesReportQueryParameters parameters)
    {
        var result = await repository.GetSalesCustomerSummaryAsync(ToFilter(parameters), parameters.Current, parameters.Size);
        return MapPage(result, x => new SalesCustomerSummaryDto
        {
            CustomerId = x.CustomerId,
            CustomerName = x.CustomerName,
            CustomerCode = x.CustomerCode,
            SaleBaseQuantity = NumericPrecision.RoundQuantity(x.SaleBaseQuantity),
            SaleAmount = NumericPrecision.RoundMoney(x.SaleAmount),
            OrderCount = x.OrderCount,
            GoodsCount = x.GoodsCount
        });
    }

    /// <inheritdoc />
    public async Task<PagedResult<SalesAreaSummaryDto>> GetSalesAreaSummaryAsync(
        SalesReportQueryParameters parameters)
    {
        var result = await repository.GetSalesAreaSummaryAsync(ToFilter(parameters), parameters.Current, parameters.Size);
        return MapPage(result, x => new SalesAreaSummaryDto
        {
            AreaName = x.AreaName,
            SaleBaseQuantity = NumericPrecision.RoundQuantity(x.SaleBaseQuantity),
            SaleAmount = NumericPrecision.RoundMoney(x.SaleAmount),
            OrderCount = x.OrderCount,
            CustomerCount = x.CustomerCount
        });
    }

    /// <inheritdoc />
    public async Task<PagedResult<AfterSaleSummaryDto>> GetAfterSaleSummaryAsync(
        AfterSaleReportQueryParameters parameters)
    {
        var result = await repository.GetAfterSaleSummaryAsync(ToFilter(parameters), parameters.Current, parameters.Size);
        return MapPage(result, x => new AfterSaleSummaryDto
        {
            AfterSaleType = x.AfterSaleType,
            ReasonType = x.ReasonType,
            HandleType = x.HandleType,
            RefundBaseQuantity = NumericPrecision.RoundQuantity(x.RefundBaseQuantity),
            RefundAmount = NumericPrecision.RoundMoney(x.RefundAmount),
            AfterSaleCount = x.AfterSaleCount,
            CustomerCount = x.CustomerCount
        });
    }

    /// <inheritdoc />
    public async Task<PagedResult<DailyStockInOutSummaryDto>> GetDailyStockInOutSummaryAsync(
        StockReportQueryParameters parameters)
    {
        var result = await repository.GetDailyStockInOutSummaryAsync(
            ToFilter(parameters),
            parameters.Current,
            parameters.Size);
        return MapPage(result, x => new DailyStockInOutSummaryDto
        {
            ReportDate = DateOnly.FromDateTime(x.ReportDate),
            InBaseQuantity = NumericPrecision.RoundQuantity(x.InBaseQuantity),
            InAmount = NumericPrecision.RoundMoney(x.InAmount),
            OutBaseQuantity = NumericPrecision.RoundQuantity(x.OutBaseQuantity),
            OutAmount = NumericPrecision.RoundMoney(x.OutAmount),
            NetAmount = NumericPrecision.RoundMoney(x.InAmount - x.OutAmount),
            InOrderCount = x.InOrderCount,
            OutOrderCount = x.OutOrderCount
        });
    }

    /// <inheritdoc />
    public async Task<PagedResult<DailyGoodsStockInOutSummaryDto>> GetDailyGoodsStockInOutSummaryAsync(
        StockReportQueryParameters parameters)
    {
        var result = await repository.GetDailyGoodsStockInOutSummaryAsync(
            ToFilter(parameters),
            parameters.Current,
            parameters.Size);
        return MapPage(result, x => new DailyGoodsStockInOutSummaryDto
        {
            ReportDate = DateOnly.FromDateTime(x.ReportDate),
            GoodsId = x.GoodsId,
            GoodsName = x.GoodsName,
            GoodsCode = x.GoodsCode,
            BaseUnitName = x.BaseUnitName,
            InBaseQuantity = NumericPrecision.RoundQuantity(x.InBaseQuantity),
            InAmount = NumericPrecision.RoundMoney(x.InAmount),
            OutBaseQuantity = NumericPrecision.RoundQuantity(x.OutBaseQuantity),
            OutAmount = NumericPrecision.RoundMoney(x.OutAmount),
            NetAmount = NumericPrecision.RoundMoney(x.InAmount - x.OutAmount)
        });
    }

    /// <inheritdoc />
    public async Task<PagedResult<PurchaseInOutGoodsSummaryDto>> GetPurchaseInOutGoodsSummaryAsync(
        PurchaseInOutReportQueryParameters parameters)
    {
        var result = await repository.GetPurchaseInOutGoodsSummaryAsync(
            ToFilter(parameters),
            parameters.Current,
            parameters.Size);
        return MapPage(result, x => new PurchaseInOutGoodsSummaryDto
        {
            GoodsId = x.GoodsId,
            GoodsName = x.GoodsName,
            GoodsCode = x.GoodsCode,
            BaseUnitName = x.BaseUnitName,
            InBaseQuantity = NumericPrecision.RoundQuantity(x.InBaseQuantity),
            InAmount = NumericPrecision.RoundMoney(x.InAmount),
            OutBaseQuantity = NumericPrecision.RoundQuantity(x.OutBaseQuantity),
            OutAmount = NumericPrecision.RoundMoney(x.OutAmount),
            NetAmount = NumericPrecision.RoundMoney(x.InAmount - x.OutAmount)
        });
    }

    /// <inheritdoc />
    public async Task<PagedResult<PurchaseInOutSupplierSummaryDto>> GetPurchaseInOutSupplierSummaryAsync(
        PurchaseInOutReportQueryParameters parameters)
    {
        var result = await repository.GetPurchaseInOutSupplierSummaryAsync(
            ToFilter(parameters),
            parameters.Current,
            parameters.Size);
        return MapPage(result, x => new PurchaseInOutSupplierSummaryDto
        {
            SupplierId = x.SupplierId,
            SupplierName = x.SupplierName,
            InBaseQuantity = NumericPrecision.RoundQuantity(x.InBaseQuantity),
            InAmount = NumericPrecision.RoundMoney(x.InAmount),
            OutBaseQuantity = NumericPrecision.RoundQuantity(x.OutBaseQuantity),
            OutAmount = NumericPrecision.RoundMoney(x.OutAmount),
            NetAmount = NumericPrecision.RoundMoney(x.InAmount - x.OutAmount)
        });
    }

    /// <inheritdoc />
    public async Task<PagedResult<PurchaseInOutPurchaserSummaryDto>> GetPurchaseInOutPurchaserSummaryAsync(
        PurchaseInOutReportQueryParameters parameters)
    {
        var result = await repository.GetPurchaseInOutPurchaserSummaryAsync(
            ToFilter(parameters),
            parameters.Current,
            parameters.Size);
        return MapPage(result, x => new PurchaseInOutPurchaserSummaryDto
        {
            PurchaserId = x.PurchaserId,
            PurchaserName = x.PurchaserName,
            InBaseQuantity = NumericPrecision.RoundQuantity(x.InBaseQuantity),
            InAmount = NumericPrecision.RoundMoney(x.InAmount),
            OutBaseQuantity = NumericPrecision.RoundQuantity(x.OutBaseQuantity),
            OutAmount = NumericPrecision.RoundMoney(x.OutAmount),
            NetAmount = NumericPrecision.RoundMoney(x.InAmount - x.OutAmount)
        });
    }

    /// <inheritdoc />
    public async Task<DashboardBriefDto> GetDashboardBriefAsync(DashboardQueryParameters parameters)
    {
        var result = await repository.GetDashboardBriefAsync(ToFilter(parameters));
        return new DashboardBriefDto
        {
            SaleAmount = NumericPrecision.RoundMoney(result.SaleAmount),
            OrderCount = result.OrderCount,
            CustomerCount = result.CustomerCount
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DashboardSalesTrendDto>> GetDashboardSalesTrendAsync(
        DashboardQueryParameters parameters)
    {
        var result = await repository.GetDashboardSalesTrendAsync(ToFilter(parameters));
        return result.Select(x => new DashboardSalesTrendDto
        {
            ReportDate = DateOnly.FromDateTime(x.ReportDate),
            SaleAmount = NumericPrecision.RoundMoney(x.SaleAmount),
            OrderCount = x.OrderCount,
            CustomerCount = x.CustomerCount
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DashboardCustomerSalesRankDto>> GetDashboardCustomerSalesRankAsync(
        DashboardQueryParameters parameters)
    {
        var result = await repository.GetDashboardCustomerSalesRankAsync(ToFilter(parameters));
        return result.Select(x => new DashboardCustomerSalesRankDto
        {
            CustomerId = x.CustomerId,
            CustomerName = x.CustomerName,
            SaleAmount = NumericPrecision.RoundMoney(x.SaleAmount),
            OrderCount = x.OrderCount
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DashboardGoodsTypeSalesRankDto>> GetDashboardGoodsTypeSalesRankAsync(
        DashboardQueryParameters parameters)
    {
        var result = await repository.GetDashboardGoodsTypeSalesRankAsync(ToFilter(parameters));
        return result.Select(x => new DashboardGoodsTypeSalesRankDto
        {
            GoodsTypeName = x.GoodsTypeName,
            SaleAmount = NumericPrecision.RoundMoney(x.SaleAmount),
            OrderCount = x.OrderCount
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<DashboardReconciliationDto> GetDashboardReconciliationAsync(
        DashboardQueryParameters parameters)
    {
        var result = await repository.GetDashboardReconciliationAsync(ToFilter(parameters));
        return new DashboardReconciliationDto
        {
            ReceivableAmount = NumericPrecision.RoundMoney(result.ReceivableAmount),
            SettledAmount = NumericPrecision.RoundMoney(result.SettledAmount),
            PendingAmount = NumericPrecision.RoundMoney(result.PendingAmount),
            BillCount = result.BillCount
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DashboardPickupStatusDto>> GetDashboardPickupStatusesAsync(
        DashboardQueryParameters parameters)
    {
        var rows = await repository.GetDashboardPickupStatusesAsync(ToFilter(parameters));
        var counts = rows.ToDictionary(x => x.PickupStatus, x => x.TaskCount);
        return Enum.GetValues<Domain.Entities.AfterSales.PickupTaskStatus>()
            .OrderBy(x => (int)x)
            .Select(status => new DashboardPickupStatusDto
            {
                PickupStatus = status,
                TaskCount = counts.GetValueOrDefault(status)
            })
            .ToList();
    }

    private static SalesReportFilter ToFilter(SalesReportQueryParameters parameters)
    {
        return new SalesReportFilter
        {
            DateStart = parameters.DateStart,
            DateEnd = parameters.DateEnd,
            CustomerId = parameters.CustomerId,
            CustomerTagIds = NormalizeIds(parameters.CustomerTagIds),
            GoodsTypeIds = NormalizeIds(parameters.GoodsTypeIds),
            GoodsIds = NormalizeIds(parameters.GoodsIds),
            Keyword = NormalizeText(parameters.Keyword),
            AreaKeyword = NormalizeText(parameters.AreaKeyword)
        };
    }

    private static AfterSaleReportFilter ToFilter(AfterSaleReportQueryParameters parameters)
    {
        return new AfterSaleReportFilter
        {
            DateStart = parameters.DateStart,
            DateEnd = parameters.DateEnd,
            CustomerId = parameters.CustomerId,
            GoodsIds = NormalizeIds(parameters.GoodsIds),
            ReasonType = parameters.ReasonType,
            AfterSaleType = parameters.AfterSaleType,
            HandleType = parameters.HandleType,
            Keyword = NormalizeText(parameters.Keyword)
        };
    }

    private static StockReportFilter ToFilter(StockReportQueryParameters parameters)
    {
        return new StockReportFilter
        {
            DateStart = parameters.DateStart,
            DateEnd = parameters.DateEnd,
            WareId = parameters.WareId,
            GoodsIds = NormalizeIds(parameters.GoodsIds),
            Keyword = NormalizeText(parameters.Keyword)
        };
    }

    private static PurchaseInOutReportFilter ToFilter(PurchaseInOutReportQueryParameters parameters)
    {
        return new PurchaseInOutReportFilter
        {
            DateStart = parameters.DateStart,
            DateEnd = parameters.DateEnd,
            WareId = parameters.WareId,
            SupplierId = parameters.SupplierId,
            PurchaserId = parameters.PurchaserId,
            PurchasePattern = parameters.PurchasePattern,
            GoodsIds = NormalizeIds(parameters.GoodsIds),
            Keyword = NormalizeText(parameters.Keyword)
        };
    }

    private static DashboardFilter ToFilter(DashboardQueryParameters parameters)
    {
        return new DashboardFilter
        {
            DateStart = parameters.DateStart,
            DateEnd = parameters.DateEnd,
            RankSize = Math.Clamp(parameters.RankSize, 1, 100)
        };
    }

    private static IReadOnlyCollection<Guid> NormalizeIds(IEnumerable<Guid>? ids)
    {
        return ids?.Where(id => id != Guid.Empty).Distinct().ToArray() ?? [];
    }

    private static string? NormalizeText(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static PagedResult<TDestination> MapPage<TSource, TDestination>(
        PagedResult<TSource> source,
        Func<TSource, TDestination> mapper)
    {
        return new PagedResult<TDestination>
        {
            Current = source.Current,
            Size = source.Size,
            Total = source.Total,
            Records = source.Records?.Select(mapper).ToList() ?? []
        };
    }
}
