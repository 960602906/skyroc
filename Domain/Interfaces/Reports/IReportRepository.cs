using Domain.ReadModels.Reports;
using Shared.Constants;

namespace Domain.Interfaces;

/// <summary>
/// 报表查询仓储接口，提供销售与售后只读聚合投影。
/// </summary>
public interface IReportRepository
{
    /// <summary>
    /// 按商品汇总已签收销售订单的验收数量和金额。
    /// </summary>
    /// <param name="filter">销售报表筛选条件。</param>
    /// <param name="current">页码，从 1 开始。</param>
    /// <param name="size">每页条数。</param>
    /// <returns>商品维度销售汇总分页结果。</returns>
    Task<PagedResult<SalesGoodsSummaryReadModel>> GetSalesGoodsSummaryAsync(
        SalesReportFilter filter,
        int current,
        int size);

    /// <summary>
    /// 按商品分类汇总已签收销售订单的验收数量和金额。
    /// </summary>
    /// <param name="filter">销售报表筛选条件。</param>
    /// <param name="current">页码，从 1 开始。</param>
    /// <param name="size">每页条数。</param>
    /// <returns>分类维度销售汇总分页结果。</returns>
    Task<PagedResult<SalesCategorySummaryReadModel>> GetSalesCategorySummaryAsync(
        SalesReportFilter filter,
        int current,
        int size);

    /// <summary>
    /// 按客户汇总已签收销售订单的验收数量和金额。
    /// </summary>
    /// <param name="filter">销售报表筛选条件。</param>
    /// <param name="current">页码，从 1 开始。</param>
    /// <param name="size">每页条数。</param>
    /// <returns>客户维度销售汇总分页结果。</returns>
    Task<PagedResult<SalesCustomerSummaryReadModel>> GetSalesCustomerSummaryAsync(
        SalesReportFilter filter,
        int current,
        int size);

    /// <summary>
    /// 按订单配送地址快照汇总已签收销售订单的验收数量和金额。
    /// </summary>
    /// <param name="filter">销售报表筛选条件。</param>
    /// <param name="current">页码，从 1 开始。</param>
    /// <param name="size">每页条数。</param>
    /// <returns>区域维度销售汇总分页结果。</returns>
    Task<PagedResult<SalesAreaSummaryReadModel>> GetSalesAreaSummaryAsync(
        SalesReportFilter filter,
        int current,
        int size);

    /// <summary>
    /// 按售后申请类型、原因和处理方式汇总已完成售后商品的退款/减免数量与金额；
    /// 补货、换货、客户沟通不计入退款减免数量。
    /// </summary>
    /// <param name="filter">售后报表筛选条件。</param>
    /// <param name="current">页码，从 1 开始。</param>
    /// <param name="size">每页条数。</param>
    /// <returns>售后汇总分页结果。</returns>
    Task<PagedResult<AfterSaleSummaryReadModel>> GetAfterSaleSummaryAsync(
        AfterSaleReportFilter filter,
        int current,
        int size);
}
