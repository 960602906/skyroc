using Application.DTOs.Reports;
using Application.interfaces;
using Application.QueryParameters.Reports;
using Domain.Interfaces;
using Domain.ReadModels.Reports;
using Shared.Constants;

namespace Application.Services;

/// <summary>
/// 销售与售后报表应用服务，负责筛选条件归一化和响应精度收口。
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
