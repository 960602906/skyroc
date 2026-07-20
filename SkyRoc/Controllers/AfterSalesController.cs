using Application.DTOs.AfterSales;
using Application.Interfaces;
using Application.QueryParameters.AfterSales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using Shared.Constants;
using SkyRoc.Authorization;

namespace SkyRoc.Controllers;

/// <summary>
/// 售后单管理控制器，提供草稿维护、审核和处理完成操作。
/// </summary>
[ApiController]
[Route("api/after-sales")]
[Authorize]
[PermissionResource(PermissionCodes.Business.AfterSales.Resource)]
public class AfterSalesController(IAfterSaleService service) : ControllerBase
{
    /// <summary>分页查询售后单。</summary>
    /// <param name="parameters">时间、单号、客户、状态和处理类型筛选条件。</param>
    /// <returns>符合条件的售后单轻量分页结果。</returns>
    [HttpGet]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<PagedResult<AfterSaleListItemDto>>>> GetPaged(
        [FromQuery] AfterSaleQueryParameters parameters)
    {
        var result = await service.GetPagedAsync(parameters);
        return Ok(ApiResponse<PagedResult<AfterSaleListItemDto>>.Ok(result));
    }

    /// <summary>查询售后单商品明细和审核轨迹。</summary>
    /// <param name="id">售后单主键。</param>
    /// <returns>售后单完整详情。</returns>
    [HttpGet("{id:guid}")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<AfterSaleDto>>> GetById(Guid id)
    {
        var result = await service.GetByIdAsync(id);
        return Ok(ApiResponse<AfterSaleDto>.Ok(result));
    }

    /// <summary>创建待提交售后单并固化来源业务快照。</summary>
    /// <param name="dto">来源订单或客户、联系信息及商品申请行。</param>
    /// <returns>新建的待提交售后单。</returns>
    [HttpPost]
    [ResourcePermission(PermissionActions.Create)]
    public async Task<ActionResult<ApiResponse<AfterSaleDto>>> Create([FromBody] CreateAfterSaleDto dto)
    {
        var result = await service.CreateAsync(dto);
        return Ok(ApiResponse<AfterSaleDto>.Ok(result));
    }

    /// <summary>更新待提交售后单并原子替换全部商品申请行。</summary>
    /// <param name="dto">售后单主键、联系信息及替换后的完整商品行。</param>
    /// <returns>更新后的售后单。</returns>
    [HttpPut]
    [ResourcePermission(PermissionActions.Update)]
    public async Task<ActionResult<ApiResponse<AfterSaleDto>>> Update([FromBody] UpdateAfterSaleDto dto)
    {
        var result = await service.UpdateAsync(dto);
        return Ok(ApiResponse<AfterSaleDto>.Ok(result));
    }

    /// <summary>删除从未提交过的售后草稿。</summary>
    /// <param name="id">售后单主键。</param>
    /// <returns>删除成功时返回 <c>true</c>。</returns>
    [HttpDelete("{id:guid}")]
    [ResourcePermission(PermissionActions.Delete)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        var result = await service.DeleteAsync(id);
        return Ok(ApiResponse<bool>.Ok(result));
    }

    /// <summary>首次提交售后草稿进入待审核。</summary>
    /// <param name="id">售后单主键。</param>
    /// <param name="dto">可选提交说明。</param>
    /// <returns>进入待审核状态的售后单。</returns>
    [HttpPost("{id:guid}/submit")]
    [Authorize(Policy = PermissionCodes.Business.AfterSales.Audit)]
    public async Task<ActionResult<ApiResponse<AfterSaleDto>>> Submit(Guid id, [FromBody] AfterSaleActionDto? dto)
    {
        var result = await service.SubmitAsync(id, dto?.Remark);
        return Ok(ApiResponse<AfterSaleDto>.Ok(result));
    }

    /// <summary>审核通过售后并进入实物或退款处理；退货退款商品会幂等生成取货任务。</summary>
    /// <param name="id">售后单主键。</param>
    /// <param name="dto">可选审核意见。</param>
    /// <returns>审核通过后的售后单。</returns>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = PermissionCodes.Business.AfterSales.Audit)]
    public async Task<ActionResult<ApiResponse<AfterSaleDto>>> Approve(Guid id, [FromBody] AfterSaleActionDto? dto)
    {
        var result = await service.ApproveAsync(id, dto?.Remark);
        return Ok(ApiResponse<AfterSaleDto>.Ok(result));
    }

    /// <summary>驳回待审核售后到可修改草稿。</summary>
    /// <param name="id">售后单主键。</param>
    /// <param name="dto">包含必填驳回原因的操作请求。</param>
    /// <returns>已驳回到待提交状态的售后单。</returns>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = PermissionCodes.Business.AfterSales.Audit)]
    public async Task<ActionResult<ApiResponse<AfterSaleDto>>> Reject(Guid id, [FromBody] AfterSaleActionDto dto)
    {
        var result = await service.RejectAsync(id, dto.Remark);
        return Ok(ApiResponse<AfterSaleDto>.Ok(result));
    }

    /// <summary>将已驳回并修正的售后单重新提交审核。</summary>
    /// <param name="id">售后单主键。</param>
    /// <param name="dto">可选重提说明。</param>
    /// <returns>重新进入待审核状态的售后单。</returns>
    [HttpPost("{id:guid}/resubmit")]
    [Authorize(Policy = PermissionCodes.Business.AfterSales.Audit)]
    public async Task<ActionResult<ApiResponse<AfterSaleDto>>> Resubmit(Guid id, [FromBody] AfterSaleActionDto? dto)
    {
        var result = await service.ResubmitAsync(id, dto?.Remark);
        return Ok(ApiResponse<AfterSaleDto>.Ok(result));
    }

    /// <summary>撤销尚未产生下游取货任务的审核结论。</summary>
    /// <param name="id">售后单主键。</param>
    /// <param name="dto">包含必填反审核说明的操作请求。</param>
    /// <returns>返回待审核状态的售后单。</returns>
    [HttpPost("{id:guid}/reverse")]
    [Authorize(Policy = PermissionCodes.Business.AfterSales.Audit)]
    public async Task<ActionResult<ApiResponse<AfterSaleDto>>> Reverse(Guid id, [FromBody] AfterSaleActionDto dto)
    {
        var result = await service.ReverseAsync(id, dto.Remark);
        return Ok(ApiResponse<AfterSaleDto>.Ok(result));
    }

    /// <summary>完成待退货、补货、换货或待退款处理；退货退款必须已完成并审核退货入库。</summary>
    /// <param name="id">售后单主键。</param>
    /// <returns>全部实物及库存处理完成后进入已完成状态的售后单。</returns>
    [HttpPost("{id:guid}/complete")]
    [ResourcePermission(PermissionActions.Update)]
    public async Task<ActionResult<ApiResponse<AfterSaleDto>>> Complete(Guid id)
    {
        var result = await service.CompleteAsync(id);
        return Ok(ApiResponse<AfterSaleDto>.Ok(result));
    }
}
