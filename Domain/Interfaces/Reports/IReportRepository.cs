using Domain.ReadModels.Reports;
using Shared.Constants;

namespace Domain.Interfaces;

/// <summary>
/// 报表查询仓储接口，提供销售、售后、库存和采购只读聚合投影。
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

    /// <summary>
    /// 按自然日汇总已审核库存入库与出库数量、金额和单据数。
    /// </summary>
    /// <param name="filter">库存报表筛选条件。</param>
    /// <param name="current">页码，从 1 开始。</param>
    /// <param name="size">每页条数。</param>
    /// <returns>日库存出入库汇总分页结果。</returns>
    Task<PagedResult<DailyStockInOutSummaryReadModel>> GetDailyStockInOutSummaryAsync(
        StockReportFilter filter,
        int current,
        int size);

    /// <summary>
    /// 按自然日和商品汇总已审核库存入库与出库数量和金额。
    /// </summary>
    /// <param name="filter">库存报表筛选条件。</param>
    /// <param name="current">页码，从 1 开始。</param>
    /// <param name="size">每页条数。</param>
    /// <returns>日商品库存出入库汇总分页结果。</returns>
    Task<PagedResult<DailyGoodsStockInOutSummaryReadModel>> GetDailyGoodsStockInOutSummaryAsync(
        StockReportFilter filter,
        int current,
        int size);

    /// <summary>
    /// 按商品汇总采购入库与采购退货出库数量和金额。
    /// </summary>
    /// <param name="filter">采购出入库报表筛选条件。</param>
    /// <param name="current">页码，从 1 开始。</param>
    /// <param name="size">每页条数。</param>
    /// <returns>商品维度采购出入库汇总分页结果。</returns>
    Task<PagedResult<PurchaseInOutGoodsSummaryReadModel>> GetPurchaseInOutGoodsSummaryAsync(
        PurchaseInOutReportFilter filter,
        int current,
        int size);

    /// <summary>
    /// 按供应商汇总采购入库与采购退货出库数量和金额。
    /// </summary>
    /// <param name="filter">采购出入库报表筛选条件。</param>
    /// <param name="current">页码，从 1 开始。</param>
    /// <param name="size">每页条数。</param>
    /// <returns>供应商维度采购出入库汇总分页结果。</returns>
    Task<PagedResult<PurchaseInOutSupplierSummaryReadModel>> GetPurchaseInOutSupplierSummaryAsync(
        PurchaseInOutReportFilter filter,
        int current,
        int size);

    /// <summary>
    /// 按采购员汇总采购入库与采购退货出库数量和金额。
    /// </summary>
    /// <param name="filter">采购出入库报表筛选条件。</param>
    /// <param name="current">页码，从 1 开始。</param>
    /// <param name="size">每页条数。</param>
    /// <returns>采购员维度采购出入库汇总分页结果。</returns>
    Task<PagedResult<PurchaseInOutPurchaserSummaryReadModel>> GetPurchaseInOutPurchaserSummaryAsync(
        PurchaseInOutReportFilter filter,
        int current,
        int size);

    /// <summary>
    /// 汇总首页经营概览，统计周期内仅包含已签收订单的客户验收金额。
    /// </summary>
    /// <param name="filter">驾驶舱统计周期与排行条数。</param>
    /// <returns>销售额、订单数和客户数聚合结果。</returns>
    Task<DashboardBriefReadModel> GetDashboardBriefAsync(DashboardFilter filter);

    /// <summary>
    /// 按订单日期汇总首页销售趋势，统计周期内仅包含已签收订单的客户验收金额。
    /// </summary>
    /// <param name="filter">驾驶舱统计周期与排行条数。</param>
    /// <returns>按自然日升序排列的销售趋势结果。</returns>
    Task<IReadOnlyList<DashboardSalesTrendReadModel>> GetDashboardSalesTrendAsync(DashboardFilter filter);

    /// <summary>
    /// 按客户汇总首页销售排行，按客户验收销售金额降序截取指定条数。
    /// </summary>
    /// <param name="filter">驾驶舱统计周期与排行条数。</param>
    /// <returns>客户销售排行结果。</returns>
    Task<IReadOnlyList<DashboardCustomerSalesRankReadModel>> GetDashboardCustomerSalesRankAsync(DashboardFilter filter);

    /// <summary>
    /// 按商品分类快照汇总首页销售排行，按客户验收销售金额降序截取指定条数。
    /// </summary>
    /// <param name="filter">驾驶舱统计周期与排行条数。</param>
    /// <returns>商品分类销售排行结果。</returns>
    Task<IReadOnlyList<DashboardGoodsTypeSalesRankReadModel>> GetDashboardGoodsTypeSalesRankAsync(DashboardFilter filter);

    /// <summary>
    /// 按客户账单业务日期汇总首页对账数据，待结金额按单账单余额下限为零计算。
    /// </summary>
    /// <param name="filter">驾驶舱统计周期与排行条数。</param>
    /// <returns>应收、已结、待结金额和账单数聚合结果。</returns>
    Task<DashboardReconciliationReadModel> GetDashboardReconciliationAsync(DashboardFilter filter);

    /// <summary>
    /// 按取货任务创建时间和当前状态汇总首页取货状态。
    /// </summary>
    /// <param name="filter">驾驶舱统计周期与排行条数。</param>
    /// <returns>实际存在状态的取货任务数量。</returns>
    Task<IReadOnlyList<DashboardPickupStatusReadModel>> GetDashboardPickupStatusesAsync(DashboardFilter filter);
}
