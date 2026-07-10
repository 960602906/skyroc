using Application.DTOs.Reports;
using Application.QueryParameters.Reports;
using Shared.Constants;

namespace Application.interfaces;

/// <summary>
/// 销售与售后报表应用服务，提供只读汇总查询且不改变业务状态。
/// </summary>
public interface IReportService
{
    /// <summary>
    /// 查询商品维度销售汇总。
    /// </summary>
    /// <param name="parameters">销售报表筛选与分页条件。</param>
    /// <returns>商品维度销售汇总分页结果。</returns>
    Task<PagedResult<SalesGoodsSummaryDto>> GetSalesGoodsSummaryAsync(SalesReportQueryParameters parameters);

    /// <summary>
    /// 查询商品分类维度销售汇总。
    /// </summary>
    /// <param name="parameters">销售报表筛选与分页条件。</param>
    /// <returns>分类维度销售汇总分页结果。</returns>
    Task<PagedResult<SalesCategorySummaryDto>> GetSalesCategorySummaryAsync(SalesReportQueryParameters parameters);

    /// <summary>
    /// 查询客户维度销售汇总。
    /// </summary>
    /// <param name="parameters">销售报表筛选与分页条件。</param>
    /// <returns>客户维度销售汇总分页结果。</returns>
    Task<PagedResult<SalesCustomerSummaryDto>> GetSalesCustomerSummaryAsync(SalesReportQueryParameters parameters);

    /// <summary>
    /// 查询区域维度销售汇总。
    /// </summary>
    /// <param name="parameters">销售报表筛选与分页条件。</param>
    /// <returns>区域维度销售汇总分页结果。</returns>
    Task<PagedResult<SalesAreaSummaryDto>> GetSalesAreaSummaryAsync(SalesReportQueryParameters parameters);

    /// <summary>
    /// 查询售后汇总。
    /// </summary>
    /// <param name="parameters">售后报表筛选与分页条件。</param>
    /// <returns>售后汇总分页结果。</returns>
    Task<PagedResult<AfterSaleSummaryDto>> GetAfterSaleSummaryAsync(AfterSaleReportQueryParameters parameters);
}
