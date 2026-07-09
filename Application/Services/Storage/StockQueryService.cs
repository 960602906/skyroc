using Application.DTOs.Storage;
using Application.Extensions;
using Application.interfaces;
using Application.QueryParameters;
using AutoMapper;
using Domain.Entities.Storage;
using Domain.Interfaces;
using Domain.ReadModels.Storage;
using Shared.Constants;

namespace Application.Services;

/// <summary>
/// 库存只读查询服务，协调数据库侧筛选、聚合、分页和标准 DTO 映射。
/// </summary>
public class StockQueryService(
    IStockBatchRepository stockBatchRepository,
    IStockLedgerRepository stockLedgerRepository,
    IMapper mapper) : IStockQueryService
{
    /// <inheritdoc />
    public async Task<PagedResult<StockOverviewDto>> GetOverviewAsync(StockOverviewQueryParameters parameters)
    {
        var result = await stockBatchRepository.GetOverviewPagedAsync(
            parameters.ToCriteria(),
            parameters.Current,
            parameters.Size);
        return mapper.ToPagedResult<StockOverviewReadModel, StockOverviewDto>(
            (result.Items, result.Total),
            parameters);
    }

    /// <inheritdoc />
    public async Task<PagedResult<StockBatchDto>> GetBatchesAsync(StockBatchQueryParameters parameters)
    {
        var result = await stockBatchRepository.GetQueryPagedAsync(
            parameters.ToCriteria(),
            parameters.Current,
            parameters.Size);
        return mapper.ToPagedResult<StockBatchReadModel, StockBatchDto>(
            (result.Items, result.Total),
            parameters);
    }

    /// <inheritdoc />
    public async Task<PagedResult<StockLedgerDto>> GetLedgersAsync(StockLedgerQueryParameters parameters)
    {
        var result = await stockLedgerRepository.GetQueryPagedAsync(
            parameters.ToCriteria(),
            parameters.Current,
            parameters.Size);
        return mapper.ToPagedResult<StockLedger, StockLedgerDto>(
            (result.Items, result.Total),
            parameters);
    }
}
