using Application.DTOs.Orders;
using Application.interfaces;
using Application.QueryParameters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using Shared.Constants;

namespace SkyRoc.Controllers;

/// <summary>
/// 销售订单管理控制器。
/// </summary>
[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController(ISaleOrderService service) : ControllerBase
{
    /// <summary>
    /// 分页查询销售订单。
    /// </summary>
    [HttpGet("list")]
    public async Task<IActionResult> GetPaged([FromQuery] SaleOrderQueryParameters parameters)
    {
        var result = await service.GetPagedAsync(parameters);
        return Ok(ApiResponse<PagedResult<SaleOrderDto>>.Ok(result));
    }

    /// <summary>
    /// 查询销售订单详情。
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await service.GetByIdAsync(id);
        return Ok(ApiResponse<SaleOrderDto>.Ok(result));
    }

    /// <summary>
    /// 创建销售订单。
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSaleOrderDto dto)
    {
        var result = await service.CreateAsync(dto);
        return Ok(ApiResponse<SaleOrderDto>.Ok(result));
    }

    /// <summary>
    /// 编辑销售订单及其商品明细。
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateSaleOrderDto dto)
    {
        var result = await service.UpdateAsync(dto);
        return Ok(ApiResponse<SaleOrderDto>.Ok(result));
    }

    /// <summary>
    /// 删除销售订单。
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await service.DeleteAsync(id);
        return Ok(ApiResponse<bool>.Ok(result));
    }

    /// <summary>
    /// 审核通过待审核订单。
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] SaleOrderAuditDto? dto)
    {
        var result = await service.ApproveAsync(id, dto?.Remark);
        return Ok(ApiResponse<SaleOrderDto>.Ok(result));
    }

    /// <summary>
    /// 驳回待审核订单。
    /// </summary>
    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] SaleOrderAuditDto? dto)
    {
        var result = await service.RejectAsync(id, dto?.Remark);
        return Ok(ApiResponse<SaleOrderDto>.Ok(result));
    }

    /// <summary>
    /// 重新提交已驳回订单。
    /// </summary>
    [HttpPost("{id:guid}/resubmit")]
    public async Task<IActionResult> Resubmit(Guid id, [FromBody] SaleOrderAuditDto? dto)
    {
        var result = await service.ResubmitAsync(id, dto?.Remark);
        return Ok(ApiResponse<SaleOrderDto>.Ok(result));
    }
}
