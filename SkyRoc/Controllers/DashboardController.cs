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
/// 首页驾驶舱控制器，提供不改变业务数据的销售、对账、取货状态和排行聚合查询。
/// </summary>
[ApiController]
[Route("api/dashboard")]
[Authorize]
[PermissionResource(PermissionCodes.Business.Reports.Resource)]
public class DashboardController(IReportService service) : ControllerBase
{
    /// <summary>查询统计周期内已签收订单的经营概览。</summary>
    /// <param name="parameters">统计周期与排行条数条件。</param>
    /// <returns>客户验收销售额、订单数和客户数。</returns>
    [HttpGet("brief")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<DashboardBriefDto>>> GetBrief(
        [FromQuery] DashboardQueryParameters parameters)
    {
        var result = await service.GetDashboardBriefAsync(parameters);
        return Ok(ApiResponse<DashboardBriefDto>.Ok(result));
    }

    /// <summary>查询按订单日期排列的已签收销售趋势。</summary>
    /// <param name="parameters">统计周期与排行条数条件。</param>
    /// <returns>自然日销售额、订单数和客户数列表。</returns>
    [HttpGet("sales-trend")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DashboardSalesTrendDto>>>> GetSalesTrend(
        [FromQuery] DashboardQueryParameters parameters)
    {
        var result = await service.GetDashboardSalesTrendAsync(parameters);
        return Ok(ApiResponse<IReadOnlyList<DashboardSalesTrendDto>>.Ok(result));
    }

    /// <summary>查询按客户验收销售额降序截取的客户销售排行。</summary>
    /// <param name="parameters">统计周期与排行条数条件。</param>
    /// <returns>客户销售排行列表。</returns>
    [HttpGet("customer-sales-rank")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DashboardCustomerSalesRankDto>>>> GetCustomerSalesRank(
        [FromQuery] DashboardQueryParameters parameters)
    {
        var result = await service.GetDashboardCustomerSalesRankAsync(parameters);
        return Ok(ApiResponse<IReadOnlyList<DashboardCustomerSalesRankDto>>.Ok(result));
    }

    /// <summary>查询按客户验收销售额降序截取的商品分类销售排行。</summary>
    /// <param name="parameters">统计周期与排行条数条件。</param>
    /// <returns>商品分类销售排行列表。</returns>
    [HttpGet("goods-type-sales-rank")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DashboardGoodsTypeSalesRankDto>>>> GetGoodsTypeSalesRank(
        [FromQuery] DashboardQueryParameters parameters)
    {
        var result = await service.GetDashboardGoodsTypeSalesRankAsync(parameters);
        return Ok(ApiResponse<IReadOnlyList<DashboardGoodsTypeSalesRankDto>>.Ok(result));
    }

    /// <summary>查询按客户账单业务日期汇总的应收、已结和待结金额。</summary>
    /// <param name="parameters">统计周期与排行条数条件。</param>
    /// <returns>客户账单对账汇总。</returns>
    [HttpGet("reconciliation")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<DashboardReconciliationDto>>> GetReconciliation(
        [FromQuery] DashboardQueryParameters parameters)
    {
        var result = await service.GetDashboardReconciliationAsync(parameters);
        return Ok(ApiResponse<DashboardReconciliationDto>.Ok(result));
    }

    /// <summary>查询按取货任务创建时间筛选并按当前状态汇总的取货任务数。</summary>
    /// <param name="parameters">统计周期与排行条数条件。</param>
    /// <returns>包含零计数状态的取货状态列表。</returns>
    [HttpGet("pickup-statuses")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DashboardPickupStatusDto>>>> GetPickupStatuses(
        [FromQuery] DashboardQueryParameters parameters)
    {
        var result = await service.GetDashboardPickupStatusesAsync(parameters);
        return Ok(ApiResponse<IReadOnlyList<DashboardPickupStatusDto>>.Ok(result));
    }
}
