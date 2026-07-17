using Application.DTOs.Reports;
using Application.QueryParameters.Reports;
using Shared.Constants;

namespace Application.Interfaces;

/// <summary>
/// 销售、售后、库存、采购报表与首页驾驶舱应用服务，提供只读汇总查询且不改变业务状态。
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

    /// <summary>
    /// 查询日库存出入库汇总。
    /// </summary>
    /// <param name="parameters">库存报表筛选与分页条件。</param>
    /// <returns>按自然日汇总的库存出入库分页结果。</returns>
    Task<PagedResult<DailyStockInOutSummaryDto>> GetDailyStockInOutSummaryAsync(
        StockReportQueryParameters parameters);

    /// <summary>
    /// 查询日商品库存出入库汇总。
    /// </summary>
    /// <param name="parameters">库存报表筛选与分页条件。</param>
    /// <returns>按自然日和商品汇总的库存出入库分页结果。</returns>
    Task<PagedResult<DailyGoodsStockInOutSummaryDto>> GetDailyGoodsStockInOutSummaryAsync(
        StockReportQueryParameters parameters);

    /// <summary>
    /// 查询采购出入库商品汇总。
    /// </summary>
    /// <param name="parameters">采购出入库报表筛选与分页条件。</param>
    /// <returns>商品维度采购出入库分页结果。</returns>
    Task<PagedResult<PurchaseInOutGoodsSummaryDto>> GetPurchaseInOutGoodsSummaryAsync(
        PurchaseInOutReportQueryParameters parameters);

    /// <summary>
    /// 查询采购出入库供应商汇总。
    /// </summary>
    /// <param name="parameters">采购出入库报表筛选与分页条件。</param>
    /// <returns>供应商维度采购出入库分页结果。</returns>
    Task<PagedResult<PurchaseInOutSupplierSummaryDto>> GetPurchaseInOutSupplierSummaryAsync(
        PurchaseInOutReportQueryParameters parameters);

    /// <summary>
    /// 查询采购出入库采购员汇总。
    /// </summary>
    /// <param name="parameters">采购出入库报表筛选与分页条件。</param>
    /// <returns>采购员维度采购出入库分页结果。</returns>
    Task<PagedResult<PurchaseInOutPurchaserSummaryDto>> GetPurchaseInOutPurchaserSummaryAsync(
        PurchaseInOutReportQueryParameters parameters);

    /// <summary>
    /// 查询首页经营概览。
    /// </summary>
    /// <param name="parameters">驾驶舱统计周期与排行条数条件。</param>
    /// <returns>已签收销售订单的销售额、订单数和客户数。</returns>
    Task<DashboardBriefDto> GetDashboardBriefAsync(DashboardQueryParameters parameters);

    /// <summary>
    /// 查询首页销售趋势。
    /// </summary>
    /// <param name="parameters">驾驶舱统计周期与排行条数条件。</param>
    /// <returns>按订单日期排列的已签收销售趋势。</returns>
    Task<IReadOnlyList<DashboardSalesTrendDto>> GetDashboardSalesTrendAsync(DashboardQueryParameters parameters);

    /// <summary>
    /// 查询首页客户销售排行。
    /// </summary>
    /// <param name="parameters">驾驶舱统计周期与排行条数条件。</param>
    /// <returns>按客户验收销售金额降序排列的客户列表。</returns>
    Task<IReadOnlyList<DashboardCustomerSalesRankDto>> GetDashboardCustomerSalesRankAsync(
        DashboardQueryParameters parameters);

    /// <summary>
    /// 查询首页商品分类销售排行。
    /// </summary>
    /// <param name="parameters">驾驶舱统计周期与排行条数条件。</param>
    /// <returns>按客户验收销售金额降序排列的商品分类列表。</returns>
    Task<IReadOnlyList<DashboardGoodsTypeSalesRankDto>> GetDashboardGoodsTypeSalesRankAsync(
        DashboardQueryParameters parameters);

    /// <summary>
    /// 查询首页客户对账汇总。
    /// </summary>
    /// <param name="parameters">驾驶舱统计周期与排行条数条件。</param>
    /// <returns>按账单业务日期汇总的应收、已结和待结金额。</returns>
    Task<DashboardReconciliationDto> GetDashboardReconciliationAsync(DashboardQueryParameters parameters);

    /// <summary>
    /// 查询首页取货状态统计。
    /// </summary>
    /// <param name="parameters">驾驶舱统计周期与排行条数条件。</param>
    /// <returns>按取货任务当前状态统计的数量，缺失状态以零返回。</returns>
    Task<IReadOnlyList<DashboardPickupStatusDto>> GetDashboardPickupStatusesAsync(DashboardQueryParameters parameters);
}
