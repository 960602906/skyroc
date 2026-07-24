using Application.DTOs.Finance;
using Application.Interfaces;
using Application.QueryParameters.Finance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using Shared.Constants;
using SkyRoc.Authorization;

namespace SkyRoc.Controllers;

/// <summary>
/// 客户结款控制器，提供待结账单查询、结款凭证创建、详情查询和作废操作。
/// </summary>
[ApiController]
[Route("api/customer-settlements")]
[Authorize]
[PermissionResource(PermissionCodes.Business.Finance.Resource)]
public class CustomerSettlementsController(ICustomerSettlementService service) : ControllerBase
{
    /// <summary>分页查询客户账单，可筛选仍有未结余额的待结账单。</summary>
    /// <param name="parameters">账单日期、客户、状态和关键字筛选条件。</param>
    /// <returns>客户账单分页结果。</returns>
    [HttpGet("bills")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<PagedResult<CustomerBillDto>>>> GetBills(
        [FromQuery] CustomerBillQueryParameters parameters)
    {
        var result = await service.GetBillsAsync(parameters);
        return Ok(ApiResponse<PagedResult<CustomerBillDto>>.Ok(result));
    }

    /// <summary>分页查询客户结款凭证。</summary>
    /// <param name="parameters">制单日期、结款日期、客户、状态和关键字筛选条件。</param>
    /// <returns>客户结款凭证分页结果。</returns>
    [HttpGet]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<PagedResult<CustomerSettlementDto>>>> GetPaged(
        [FromQuery] CustomerSettlementQueryParameters parameters)
    {
        var result = await service.GetPagedAsync(parameters);
        return Ok(ApiResponse<PagedResult<CustomerSettlementDto>>.Ok(result));
    }

    /// <summary>查询客户结款凭证明细。</summary>
    /// <param name="id">客户结款凭证主键。</param>
    /// <returns>包含账单核销明细的客户结款凭证。</returns>
    [HttpGet("{id:guid}")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<CustomerSettlementDto>>> GetById(Guid id)
    {
        var result = await service.GetByIdAsync(id);
        return Ok(ApiResponse<CustomerSettlementDto>.Ok(result));
    }

    /// <summary>根据结款凭证编号查询客户结款凭证明细。</summary>
    /// <param name="settlementNo">结款凭证编号。</param>
    /// <returns>包含账单核销明细的客户结款凭证。</returns>
    [HttpGet("by-no/{settlementNo}")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<CustomerSettlementDto>>> GetBySettlementNo(string settlementNo)
    {
        var result = await service.GetBySettlementNoAsync(settlementNo);
        return Ok(ApiResponse<CustomerSettlementDto>.Ok(result));
    }

    /// <summary>创建客户结款凭证并回写客户账单余额。</summary>
    /// <param name="dto">结款日期、流水号、备注和账单核销明细。</param>
    /// <returns>创建后的客户结款凭证。</returns>
    [HttpPost]
    [ResourcePermission(PermissionActions.Create)]
    public async Task<ActionResult<ApiResponse<CustomerSettlementDto>>> Create(
        [FromBody] CreateCustomerSettlementDto dto)
    {
        var result = await service.CreateAsync(dto);
        return Ok(ApiResponse<CustomerSettlementDto>.Ok(result));
    }

    /// <summary>作废客户结款凭证并回滚已核销账单金额。</summary>
    /// <param name="id">客户结款凭证主键。</param>
    /// <param name="dto">作废原因。</param>
    /// <returns>已作废的客户结款凭证。</returns>
    [HttpDelete("{id:guid}/void")]
    [ResourcePermission(PermissionActions.Delete)]
    public async Task<ActionResult<ApiResponse<CustomerSettlementDto>>> Void(
        Guid id,
        [FromBody] VoidCustomerSettlementDto dto)
    {
        var result = await service.VoidAsync(id, dto);
        return Ok(ApiResponse<CustomerSettlementDto>.Ok(result));
    }
}
