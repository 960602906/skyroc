using Application.DTOs.Storage;
using Application.QueryParameters;
using Shared.Constants;

namespace Application.Interfaces;

/// <summary>
/// 库存查询应用服务接口，提供库存总览、批次余额和只追加台账的只读查询。
/// </summary>
public interface IStockQueryService
{
    /// <summary>
    /// 按仓库和商品分页汇总当前数量、可用数量、占用量及货值。
    /// </summary>
    /// <param name="parameters">库存总览分页和筛选参数。</param>
    /// <returns>仓库商品粒度的库存总览分页结果。</returns>
    Task<PagedResult<StockOverviewDto>> GetOverviewAsync(StockOverviewQueryParameters parameters);

    /// <summary>
    /// 分页查询库存批次的数量、成本、生产日期和到期日期。
    /// </summary>
    /// <param name="parameters">库存批次分页和筛选参数。</param>
    /// <returns>批次库存分页结果。</returns>
    Task<PagedResult<StockBatchDto>> GetBatchesAsync(StockBatchQueryParameters parameters);

    /// <summary>
    /// 分页查询库存增减流水，包含审核和反审核记录以保持完整追溯链。
    /// </summary>
    /// <param name="parameters">库存台账分页和筛选参数。</param>
    /// <returns>按发生时间倒序排列的库存台账分页结果。</returns>
    Task<PagedResult<StockLedgerDto>> GetLedgersAsync(StockLedgerQueryParameters parameters);
}
