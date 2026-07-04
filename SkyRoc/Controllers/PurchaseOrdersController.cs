using Application.DTOs.Purchases;
using Application.interfaces;
using Application.QueryParameters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using Shared.Constants;
using SkyRoc.Authorization;

namespace SkyRoc.Controllers;

/// <summary>
/// 采购单管理控制器，提供查询、维护、计划生成以及完成或取消状态流转接口。
/// </summary>
[ApiController]
[Route("api/purchase-orders")]
[Authorize]
[PermissionResource(PermissionCodes.Business.Purchases.Resource)]
public class PurchaseOrdersController(IPurchaseOrderService service) : ControllerBase
{
    /// <summary>
    /// 分页查询采购单及商品明细。需要采购读取权限。
    /// </summary>
    /// <param name="parameters">采购单分页和筛选参数。</param>
    /// <returns>采购单分页结果。</returns>
    [HttpGet("list")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<PagedResult<PurchaseOrderDto>>>> GetPaged(
        [FromQuery] PurchaseOrderQueryParameters parameters)
    {
        var result = await service.GetPagedAsync(parameters);
        return Ok(ApiResponse<PagedResult<PurchaseOrderDto>>.Ok(result));
    }

    /// <summary>
    /// 查询采购单详情及其采购计划来源。需要采购读取权限。
    /// </summary>
    /// <param name="id">采购单主键。</param>
    /// <returns>采购单完整详情。</returns>
    [HttpGet("{id:guid}")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> GetById(Guid id)
    {
        var result = await service.GetByIdAsync(id);
        return Ok(ApiResponse<PurchaseOrderDto>.Ok(result));
    }

    /// <summary>
    /// 手工创建采购单草稿和商品明细。需要采购创建权限。
    /// </summary>
    /// <param name="dto">采购单创建请求。</param>
    /// <returns>创建后的采购单详情。</returns>
    [HttpPost]
    [ResourcePermission(PermissionActions.Create)]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> Create([FromBody] CreatePurchaseOrderDto dto)
    {
        var result = await service.CreateAsync(dto);
        return Ok(ApiResponse<PurchaseOrderDto>.Ok(result));
    }

    /// <summary>
    /// 编辑采购单草稿及其全部商品明细。需要采购更新权限。
    /// </summary>
    /// <param name="dto">采购单完整替换请求。</param>
    /// <returns>更新后的采购单详情。</returns>
    [HttpPut]
    [ResourcePermission(PermissionActions.Update)]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> Update([FromBody] UpdatePurchaseOrderDto dto)
    {
        var result = await service.UpdateAsync(dto);
        return Ok(ApiResponse<PurchaseOrderDto>.Ok(result));
    }

    /// <summary>
    /// 删除采购单草稿并释放采购计划数量占用。需要采购删除权限。
    /// </summary>
    /// <param name="id">采购单主键。</param>
    /// <returns>删除成功标记。</returns>
    [HttpDelete("{id:guid}")]
    [ResourcePermission(PermissionActions.Delete)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        var result = await service.DeleteAsync(id);
        return Ok(ApiResponse<bool>.Ok(result));
    }

    /// <summary>
    /// 从采购计划全部剩余数量批量生成采购单草稿。需要采购创建权限。
    /// </summary>
    /// <param name="dto">来源采购计划、预计到货时间和备注。</param>
    /// <returns>按采购模式、供应商和采购员分组生成的采购单集合。</returns>
    [HttpPost("generate-from-plans")]
    [ResourcePermission(PermissionActions.Create)]
    public async Task<ActionResult<ApiResponse<List<PurchaseOrderDto>>>> GenerateFromPlans(
        [FromBody] GeneratePurchaseOrdersFromPlansDto dto)
    {
        var result = await service.GenerateFromPlansAsync(dto);
        return Ok(ApiResponse<List<PurchaseOrderDto>>.Ok(result));
    }

    /// <summary>
    /// 完成采购单草稿，使其可供后续采购入库引用。需要采购更新权限。
    /// </summary>
    /// <param name="id">采购单主键。</param>
    /// <returns>完成后的采购单详情。</returns>
    [HttpPost("{id:guid}/complete")]
    [ResourcePermission(PermissionActions.Update)]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> Complete(Guid id)
    {
        var result = await service.CompleteAsync(id);
        return Ok(ApiResponse<PurchaseOrderDto>.Ok(result));
    }

    /// <summary>
    /// 取消采购单草稿并释放采购计划数量占用。需要采购更新权限。
    /// </summary>
    /// <param name="id">采购单主键。</param>
    /// <returns>取消后的采购单详情。</returns>
    [HttpPost("{id:guid}/cancel")]
    [ResourcePermission(PermissionActions.Update)]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> Cancel(Guid id)
    {
        var result = await service.CancelAsync(id);
        return Ok(ApiResponse<PurchaseOrderDto>.Ok(result));
    }
}
