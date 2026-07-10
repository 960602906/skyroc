using Application.DTOs.Reports;
using Application.QueryParameters.Reports;
using Shared.Constants;

namespace Application.interfaces;

/// <summary>
/// 销售、售后、库存和采购报表应用服务，提供只读汇总查询且不改变业务状态。
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
}
