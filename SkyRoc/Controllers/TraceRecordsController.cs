using Application.DTOs.Traceability;
using Application.interfaces;
using Application.QueryParameters.Traceability;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using Shared.Constants;
using SkyRoc.Authorization;

namespace SkyRoc.Controllers;

/// <summary>商品溯源控制器，生成销售订单商品溯源记录、查询后台追溯列表，并提供二维码白名单详情。</summary>
[ApiController]
[Route("api/traceability/traces")]
[Authorize]
[PermissionResource(PermissionCodes.Business.Traceability.Resource)]
public class TraceRecordsController(ITraceabilityService service) : ControllerBase
{
    /// <summary>分页查询商品溯源记录。</summary>
    /// <param name="parameters">订单、仓库、供应商、商品分类、商品与关键字筛选条件。</param>
    /// <returns>商品溯源记录分页结果。</returns>
    [HttpGet]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<PagedResult<TraceRecordDto>>>> GetPaged(
        [FromQuery] TraceRecordQueryParameters parameters)
    {
        return Ok(ApiResponse<PagedResult<TraceRecordDto>>.Ok(await service.GetTraceRecordsAsync(parameters)));
    }

    /// <summary>为销售订单中已审核销售出库的商品生成缺失溯源记录。</summary>
    /// <param name="saleOrderId">销售订单主键。</param>
    /// <returns>该订单全部已生成的商品溯源记录。</returns>
    [HttpPost("sale-orders/{saleOrderId:guid}/generate")]
    [ResourcePermission(PermissionActions.Create)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TraceRecordDto>>>> Generate(Guid saleOrderId)
    {
        return Ok(ApiResponse<IReadOnlyList<TraceRecordDto>>.Ok(await service.GenerateSaleOrderTracesAsync(saleOrderId)));
    }

    /// <summary>读取二维码公开详情，仅返回已固化的商品、批次、供应商、仓库和检测报告快照。</summary>
    /// <param name="traceNo">二维码承载的溯源业务编号。</param>
    /// <returns>无需登录的二维码溯源详情。</returns>
    [AllowAnonymous]
    [HttpGet("qr/{traceNo}")]
    public async Task<ActionResult<ApiResponse<TraceQrCodeDto>>> GetQrCode(string traceNo)
    {
        return Ok(ApiResponse<TraceQrCodeDto>.Ok(await service.GetTraceQrCodeAsync(traceNo)));
    }
}
