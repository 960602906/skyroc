using Application.DTOs;
using Application.DTOs.Orders;
using Application.Interfaces;
using Application.QueryParameters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using Shared.Constants;
using SkyRoc.Authorization;

namespace SkyRoc.Controllers;

/// <summary>
/// 销售订单管理控制器。
/// </summary>
[ApiController]
[Route("api/orders")]
[Authorize]
[PermissionResource(PermissionCodes.Business.Orders.Resource)]
public class OrdersController(ISaleOrderService service) : ControllerBase
{
    /// <summary>
    /// 分页查询销售订单。
    /// </summary>
    [HttpGet("list")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<PagedResult<SaleOrderDto>>>> GetPaged(
        [FromQuery] SaleOrderQueryParameters parameters)
    {
        var result = await service.GetPagedAsync(parameters);
        return Ok(ApiResponse<PagedResult<SaleOrderDto>>.Ok(result));
    }

    /// <summary>
    /// 按订单号限量搜索销售订单选择项。
    /// </summary>
    /// <param name="parameters">订单号关键词和返回数量；空关键词默认返回最近 20 条。</param>
    /// <returns>轻量订单选择项和是否仍有更多匹配项。</returns>
    [HttpGet("options/search")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<SelectionOptionSearchResultDto>>> SearchOptions(
        [FromQuery] SelectionOptionSearchQueryParameters parameters)
    {
        var result = await service.SearchSelectionOptionsAsync(parameters);
        return Ok(ApiResponse<SelectionOptionSearchResultDto>.Ok(result));
    }

    /// <summary>
    /// 按主键集合解析已选销售订单的订单号和客户名称。
    /// </summary>
    /// <param name="parameters">销售订单主键集合，单次最多 100 个。</param>
    /// <returns>存在的轻量销售订单选择项。</returns>
    [HttpGet("options/resolve")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<List<SelectionOptionDto>>>> ResolveOptions(
        [FromQuery] SelectionOptionResolveQueryParameters parameters)
    {
        var result = await service.ResolveSelectionOptionsAsync(parameters.Ids);
        return Ok(ApiResponse<List<SelectionOptionDto>>.Ok(result));
    }

    /// <summary>
    /// 查询销售订单详情。
    /// </summary>
    [HttpGet("{id:guid}")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<SaleOrderDto>>> GetById(Guid id)
    {
        var result = await service.GetByIdAsync(id);
        return Ok(ApiResponse<SaleOrderDto>.Ok(result));
    }

    /// <summary>
    /// 创建销售订单。
    /// </summary>
    [HttpPost]
    [ResourcePermission(PermissionActions.Create)]
    public async Task<ActionResult<ApiResponse<SaleOrderDto>>> Create([FromBody] CreateSaleOrderDto dto)
    {
        var result = await service.CreateAsync(dto);
        return Ok(ApiResponse<SaleOrderDto>.Ok(result));
    }

    /// <summary>
    /// 编辑销售订单及其商品明细。
    /// </summary>
    [HttpPut]
    [ResourcePermission(PermissionActions.Update)]
    public async Task<ActionResult<ApiResponse<SaleOrderDto>>> Update([FromBody] UpdateSaleOrderDto dto)
    {
        var result = await service.UpdateAsync(dto);
        return Ok(ApiResponse<SaleOrderDto>.Ok(result));
    }

    /// <summary>
    /// 删除销售订单。
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ResourcePermission(PermissionActions.Delete)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        var result = await service.DeleteAsync(id);
        return Ok(ApiResponse<bool>.Ok(result));
    }

    /// <summary>
    /// 审核通过待审核订单。
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = PermissionCodes.Business.Orders.Audit)]
    public async Task<ActionResult<ApiResponse<SaleOrderDto>>> Approve(Guid id, [FromBody] SaleOrderAuditDto? dto)
    {
        var result = await service.ApproveAsync(id, dto?.Remark);
        return Ok(ApiResponse<SaleOrderDto>.Ok(result));
    }

    /// <summary>
    /// 驳回待审核订单。
    /// </summary>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = PermissionCodes.Business.Orders.Audit)]
    public async Task<ActionResult<ApiResponse<SaleOrderDto>>> Reject(Guid id, [FromBody] SaleOrderAuditDto? dto)
    {
        var result = await service.RejectAsync(id, dto?.Remark);
        return Ok(ApiResponse<SaleOrderDto>.Ok(result));
    }

    /// <summary>
    /// 重新提交已驳回订单。
    /// </summary>
    [HttpPost("{id:guid}/resubmit")]
    [Authorize(Policy = PermissionCodes.Business.Orders.Audit)]
    public async Task<ActionResult<ApiResponse<SaleOrderDto>>> Resubmit(Guid id, [FromBody] SaleOrderAuditDto? dto)
    {
        var result = await service.ResubmitAsync(id, dto?.Remark);
        return Ok(ApiResponse<SaleOrderDto>.Ok(result));
    }
}
