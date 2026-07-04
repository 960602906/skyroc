using Application.DTOs.Storage;
using Application.interfaces;
using Application.QueryParameters;
using Domain.Entities.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using Shared.Constants;
using SkyRoc.Authorization;

namespace SkyRoc.Controllers;

/// <summary>
/// 采购入库控制器，提供采购到货入库的查询、维护及审核、反审核接口。
/// </summary>
[ApiController]
[Route("api/stock-in/purchase")]
[Authorize]
[PermissionResource(PermissionCodes.Business.Storage.Resource)]
public class PurchaseStockInController(IStockInService service) : ControllerBase
{
    private const StockInOrderType OrderType = StockInOrderType.Purchase;

    /// <summary>
    /// 分页查询采购入库单及商品明细。需要库存读取权限。
    /// </summary>
    /// <param name="parameters">采购入库分页和筛选参数。</param>
    /// <returns>采购入库单分页结果。</returns>
    [HttpGet("list")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<PagedResult<StockInOrderDto>>>> GetPaged([FromQuery] StockInOrderQueryParameters parameters)
    {
        var result = await service.GetPagedAsync(OrderType, parameters);
        return Ok(ApiResponse<PagedResult<StockInOrderDto>>.Ok(result));
    }

    /// <summary>
    /// 查询采购入库单详情。需要库存读取权限。
    /// </summary>
    /// <param name="id">采购入库单主键。</param>
    /// <returns>采购入库单完整详情。</returns>
    [HttpGet("{id:guid}")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<StockInOrderDto>>> GetById(Guid id)
    {
        var result = await service.GetByIdAsync(OrderType, id);
        return Ok(ApiResponse<StockInOrderDto>.Ok(result));
    }

    /// <summary>
    /// 创建采购入库草稿及商品明细。需要库存创建权限。
    /// </summary>
    /// <param name="dto">采购入库创建请求。</param>
    /// <returns>创建后的采购入库单详情。</returns>
    [HttpPost]
    [ResourcePermission(PermissionActions.Create)]
    public async Task<ActionResult<ApiResponse<StockInOrderDto>>> Create([FromBody] CreatePurchaseStockInDto dto)
    {
        var result = await service.CreatePurchaseAsync(dto);
        return Ok(ApiResponse<StockInOrderDto>.Ok(result));
    }

    /// <summary>
    /// 编辑采购入库草稿及其全部商品明细。需要库存更新权限。
    /// </summary>
    /// <param name="dto">采购入库整单替换请求。</param>
    /// <returns>更新后的采购入库单详情。</returns>
    [HttpPut]
    [ResourcePermission(PermissionActions.Update)]
    public async Task<ActionResult<ApiResponse<StockInOrderDto>>> Update([FromBody] UpdatePurchaseStockInDto dto)
    {
        var result = await service.UpdatePurchaseAsync(dto);
        return Ok(ApiResponse<StockInOrderDto>.Ok(result));
    }

    /// <summary>
    /// 删除采购入库草稿。需要库存删除权限。
    /// </summary>
    /// <param name="id">采购入库单主键。</param>
    /// <returns>删除成功标记。</returns>
    [HttpDelete("{id:guid}")]
    [ResourcePermission(PermissionActions.Delete)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        var result = await service.DeleteAsync(OrderType, id);
        return Ok(ApiResponse<bool>.Ok(result));
    }

    /// <summary>
    /// 审核采购入库单，增加库存批次并写入库存流水。需要库存更新权限。
    /// </summary>
    /// <param name="id">采购入库单主键。</param>
    /// <param name="dto">审核说明。</param>
    /// <returns>审核后的采购入库单详情。</returns>
    [HttpPost("{id:guid}/audit")]
    [ResourcePermission(PermissionActions.Update)]
    public async Task<ActionResult<ApiResponse<StockInOrderDto>>> Audit(Guid id, [FromBody] StockInAuditDto? dto)
    {
        var result = await service.AuditAsync(OrderType, id, dto?.Remark);
        return Ok(ApiResponse<StockInOrderDto>.Ok(result));
    }

    /// <summary>
    /// 反审核采购入库单，回滚库存批次并写入反向流水。需要库存更新权限。
    /// </summary>
    /// <param name="id">采购入库单主键。</param>
    /// <param name="dto">反审核原因说明。</param>
    /// <returns>反审核后的采购入库单详情。</returns>
    [HttpPost("{id:guid}/reverse-audit")]
    [ResourcePermission(PermissionActions.Update)]
    public async Task<ActionResult<ApiResponse<StockInOrderDto>>> ReverseAudit(Guid id, [FromBody] StockInAuditDto? dto)
    {
        var result = await service.ReverseAuditAsync(OrderType, id, dto?.Remark);
        return Ok(ApiResponse<StockInOrderDto>.Ok(result));
    }
}
