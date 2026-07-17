using Application.DTOs.Storage;
using Application.Interfaces;
using Application.QueryParameters;
using Domain.Entities.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using Shared.Constants;
using SkyRoc.Authorization;

namespace SkyRoc.Controllers;

/// <summary>
/// 其他出库控制器，提供手工库存扣减单据的查询、维护及审核、反审核接口。
/// </summary>
[ApiController]
[Route("api/stock-out/other")]
[Authorize]
[PermissionResource(PermissionCodes.Business.Storage.Resource)]
public class OtherStockOutController(IStockOutService service) : ControllerBase
{
    private const StockOutOrderType OrderType = StockOutOrderType.Other;

    /// <summary>
    /// 分页查询其他出库单及商品批次明细。需要库存读取权限。
    /// </summary>
    /// <param name="parameters">其他出库分页和筛选参数。</param>
    /// <returns>其他出库单分页结果。</returns>
    [HttpGet("list")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<PagedResult<StockOutOrderDto>>>> GetPaged([FromQuery] StockOutOrderQueryParameters parameters)
    {
        var result = await service.GetPagedAsync(OrderType, parameters);
        return Ok(ApiResponse<PagedResult<StockOutOrderDto>>.Ok(result));
    }

    /// <summary>
    /// 查询其他出库单详情。需要库存读取权限。
    /// </summary>
    /// <param name="id">其他出库单主键。</param>
    /// <returns>其他出库单完整详情。</returns>
    [HttpGet("{id:guid}")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<StockOutOrderDto>>> GetById(Guid id)
    {
        var result = await service.GetByIdAsync(OrderType, id);
        return Ok(ApiResponse<StockOutOrderDto>.Ok(result));
    }

    /// <summary>
    /// 创建其他出库草稿及商品批次明细。需要库存创建权限。
    /// </summary>
    /// <param name="dto">其他出库创建请求。</param>
    /// <returns>创建后的其他出库单详情。</returns>
    [HttpPost]
    [ResourcePermission(PermissionActions.Create)]
    public async Task<ActionResult<ApiResponse<StockOutOrderDto>>> Create([FromBody] CreateOtherStockOutDto dto)
    {
        var result = await service.CreateOtherAsync(dto);
        return Ok(ApiResponse<StockOutOrderDto>.Ok(result));
    }

    /// <summary>
    /// 编辑其他出库草稿及其全部商品批次明细。需要库存更新权限。
    /// </summary>
    /// <param name="dto">其他出库整单替换请求。</param>
    /// <returns>更新后的其他出库单详情。</returns>
    [HttpPut]
    [ResourcePermission(PermissionActions.Update)]
    public async Task<ActionResult<ApiResponse<StockOutOrderDto>>> Update([FromBody] UpdateOtherStockOutDto dto)
    {
        var result = await service.UpdateOtherAsync(dto);
        return Ok(ApiResponse<StockOutOrderDto>.Ok(result));
    }

    /// <summary>
    /// 删除其他出库草稿。需要库存删除权限。
    /// </summary>
    /// <param name="id">其他出库单主键。</param>
    /// <returns>删除成功标记。</returns>
    [HttpDelete("{id:guid}")]
    [ResourcePermission(PermissionActions.Delete)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        var result = await service.DeleteAsync(OrderType, id);
        return Ok(ApiResponse<bool>.Ok(result));
    }

    /// <summary>
    /// 审核其他出库单，校验可用库存后扣减批次并写入库存流水。需要库存更新权限。
    /// </summary>
    /// <param name="id">其他出库单主键。</param>
    /// <param name="dto">审核说明。</param>
    /// <returns>审核后的其他出库单详情。</returns>
    [HttpPost("{id:guid}/audit")]
    [ResourcePermission(PermissionActions.Update)]
    public async Task<ActionResult<ApiResponse<StockOutOrderDto>>> Audit(Guid id, [FromBody] StockOutAuditDto? dto)
    {
        var result = await service.AuditAsync(OrderType, id, dto?.Remark);
        return Ok(ApiResponse<StockOutOrderDto>.Ok(result));
    }

    /// <summary>
    /// 反审核其他出库单，恢复库存批次并写入反向流水。需要库存更新权限。
    /// </summary>
    /// <param name="id">其他出库单主键。</param>
    /// <param name="dto">反审核原因说明。</param>
    /// <returns>反审核后的其他出库单详情。</returns>
    [HttpPost("{id:guid}/reverse-audit")]
    [ResourcePermission(PermissionActions.Update)]
    public async Task<ActionResult<ApiResponse<StockOutOrderDto>>> ReverseAudit(Guid id, [FromBody] StockOutAuditDto? dto)
    {
        var result = await service.ReverseAuditAsync(OrderType, id, dto?.Remark);
        return Ok(ApiResponse<StockOutOrderDto>.Ok(result));
    }
}
