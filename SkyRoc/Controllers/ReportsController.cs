using Application.DTOs.Reports;
using Application.Interfaces;
using Application.QueryParameters.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using Shared.Constants;
using SkyRoc.Authorization;

namespace SkyRoc.Controllers;

/// <summary>
/// 报表控制器，提供销售、售后、库存和采购多维汇总只读查询。
/// </summary>
[ApiController]
[Route("api/reports")]
[Authorize]
[PermissionResource(PermissionCodes.Business.Reports.Resource)]
public class ReportsController(IReportService service) : ControllerBase
{
    /// <summary>按商品汇总已签收销售订单的验收数量和金额。</summary>
    /// <param name="parameters">日期、客户、标签、商品、分类和区域筛选条件。</param>
    /// <returns>商品维度销售汇总分页结果。</returns>
    [HttpGet("sales/goods")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<PagedResult<SalesGoodsSummaryDto>>>> GetSalesGoodsSummary(
        [FromQuery] SalesReportQueryParameters parameters)
    {
        var result = await service.GetSalesGoodsSummaryAsync(parameters);
        return Ok(ApiResponse<PagedResult<SalesGoodsSummaryDto>>.Ok(result));
    }

    /// <summary>按商品分类汇总已签收销售订单的验收数量和金额。</summary>
    /// <param name="parameters">日期、客户、标签、商品、分类和区域筛选条件。</param>
    /// <returns>分类维度销售汇总分页结果。</returns>
    [HttpGet("sales/categories")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<PagedResult<SalesCategorySummaryDto>>>> GetSalesCategorySummary(
        [FromQuery] SalesReportQueryParameters parameters)
    {
        var result = await service.GetSalesCategorySummaryAsync(parameters);
        return Ok(ApiResponse<PagedResult<SalesCategorySummaryDto>>.Ok(result));
    }

    /// <summary>按客户汇总已签收销售订单的验收数量和金额。</summary>
    /// <param name="parameters">日期、客户、标签、商品、分类和区域筛选条件。</param>
    /// <returns>客户维度销售汇总分页结果。</returns>
    [HttpGet("sales/customers")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<PagedResult<SalesCustomerSummaryDto>>>> GetSalesCustomerSummary(
        [FromQuery] SalesReportQueryParameters parameters)
    {
        var result = await service.GetSalesCustomerSummaryAsync(parameters);
        return Ok(ApiResponse<PagedResult<SalesCustomerSummaryDto>>.Ok(result));
    }

    /// <summary>按订单配送地址快照汇总已签收销售订单的验收数量和金额。</summary>
    /// <param name="parameters">日期、客户、标签、商品、分类和区域筛选条件。</param>
    /// <returns>区域维度销售汇总分页结果。</returns>
    [HttpGet("sales/areas")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<PagedResult<SalesAreaSummaryDto>>>> GetSalesAreaSummary(
        [FromQuery] SalesReportQueryParameters parameters)
    {
        var result = await service.GetSalesAreaSummaryAsync(parameters);
        return Ok(ApiResponse<PagedResult<SalesAreaSummaryDto>>.Ok(result));
    }

    /// <summary>按售后申请类型、原因和处理方式汇总已完成售后的退款/减免；补货、换货、客户沟通数量计 0。</summary>
    /// <param name="parameters">日期、客户、商品、原因、类型和处理方式筛选条件。</param>
    /// <returns>售后汇总分页结果。</returns>
    [HttpGet("after-sales")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<PagedResult<AfterSaleSummaryDto>>>> GetAfterSaleSummary(
        [FromQuery] AfterSaleReportQueryParameters parameters)
    {
        var result = await service.GetAfterSaleSummaryAsync(parameters);
        return Ok(ApiResponse<PagedResult<AfterSaleSummaryDto>>.Ok(result));
    }

    /// <summary>按自然日汇总已审核库存入库与出库的数量、金额和单据数。</summary>
    /// <param name="parameters">日期、仓库、商品和关键字筛选条件。</param>
    /// <returns>日库存出入库汇总分页结果。</returns>
    [HttpGet("stock/daily")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<PagedResult<DailyStockInOutSummaryDto>>>> GetDailyStockInOutSummary(
        [FromQuery] StockReportQueryParameters parameters)
    {
        var result = await service.GetDailyStockInOutSummaryAsync(parameters);
        return Ok(ApiResponse<PagedResult<DailyStockInOutSummaryDto>>.Ok(result));
    }

    /// <summary>按自然日和商品汇总已审核库存入库与出库的数量和金额。</summary>
    /// <param name="parameters">日期、仓库、商品和关键字筛选条件。</param>
    /// <returns>日商品库存出入库汇总分页结果。</returns>
    [HttpGet("stock/daily-goods")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<PagedResult<DailyGoodsStockInOutSummaryDto>>>> GetDailyGoodsStockInOutSummary(
        [FromQuery] StockReportQueryParameters parameters)
    {
        var result = await service.GetDailyGoodsStockInOutSummaryAsync(parameters);
        return Ok(ApiResponse<PagedResult<DailyGoodsStockInOutSummaryDto>>.Ok(result));
    }

    /// <summary>按商品汇总采购入库与采购退货出库的数量和金额。</summary>
    /// <param name="parameters">日期、仓库、供应商、采购员、采购模式、商品和关键字筛选条件。</param>
    /// <returns>商品维度采购出入库汇总分页结果。</returns>
    [HttpGet("purchase-in-out/goods")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<PagedResult<PurchaseInOutGoodsSummaryDto>>>> GetPurchaseInOutGoodsSummary(
        [FromQuery] PurchaseInOutReportQueryParameters parameters)
    {
        var result = await service.GetPurchaseInOutGoodsSummaryAsync(parameters);
        return Ok(ApiResponse<PagedResult<PurchaseInOutGoodsSummaryDto>>.Ok(result));
    }

    /// <summary>按供应商汇总采购入库与采购退货出库的数量和金额。</summary>
    /// <param name="parameters">日期、仓库、供应商、采购员、采购模式、商品和关键字筛选条件。</param>
    /// <returns>供应商维度采购出入库汇总分页结果。</returns>
    [HttpGet("purchase-in-out/suppliers")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<PagedResult<PurchaseInOutSupplierSummaryDto>>>> GetPurchaseInOutSupplierSummary(
        [FromQuery] PurchaseInOutReportQueryParameters parameters)
    {
        var result = await service.GetPurchaseInOutSupplierSummaryAsync(parameters);
        return Ok(ApiResponse<PagedResult<PurchaseInOutSupplierSummaryDto>>.Ok(result));
    }

    /// <summary>按采购员汇总采购入库与采购退货出库的数量和金额；退货出库通过批次追溯原采购入库采购员。</summary>
    /// <param name="parameters">日期、仓库、供应商、采购员、采购模式、商品和关键字筛选条件。</param>
    /// <returns>采购员维度采购出入库汇总分页结果。</returns>
    [HttpGet("purchase-in-out/purchasers")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<PagedResult<PurchaseInOutPurchaserSummaryDto>>>> GetPurchaseInOutPurchaserSummary(
        [FromQuery] PurchaseInOutReportQueryParameters parameters)
    {
        var result = await service.GetPurchaseInOutPurchaserSummaryAsync(parameters);
        return Ok(ApiResponse<PagedResult<PurchaseInOutPurchaserSummaryDto>>.Ok(result));
    }
}
