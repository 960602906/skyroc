using Application.DTOs.Reports;
using Application.interfaces;
using Application.QueryParameters.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using Shared.Constants;
using SkyRoc.Authorization;

namespace SkyRoc.Controllers;

/// <summary>
/// 报表控制器，提供销售商品、分类、客户、区域和售后汇总只读查询。
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
}
