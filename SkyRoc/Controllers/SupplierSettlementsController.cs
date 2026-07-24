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
/// 供应商结算控制器，提供待结单据查询、结算单创建、详情查询和作废操作。
/// </summary>
[ApiController]
[Route("api/supplier-settlements")]
[Authorize]
[PermissionResource(PermissionCodes.Business.Finance.Resource)]
public class SupplierSettlementsController(ISupplierSettlementService service) : ControllerBase
{
    /// <summary>分页查询供应商待结单据，可筛选仍有未结余额的待处理单据。</summary>
    /// <param name="parameters">单据日期、供应商、来源类型、状态和关键字筛选条件。</param>
    /// <returns>供应商待结单据分页结果。</returns>
    [HttpGet("bills")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<PagedResult<SupplierBillDto>>>> GetBills(
        [FromQuery] SupplierBillQueryParameters parameters)
    {
        var result = await service.GetBillsAsync(parameters);
        return Ok(ApiResponse<PagedResult<SupplierBillDto>>.Ok(result));
    }

    /// <summary>分页查询供应商结算单。</summary>
    /// <param name="parameters">制单日期、结款日期、供应商、状态和关键字筛选条件。</param>
    /// <returns>供应商结算单分页结果。</returns>
    [HttpGet]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<PagedResult<SupplierSettlementDto>>>> GetPaged(
        [FromQuery] SupplierSettlementQueryParameters parameters)
    {
        var result = await service.GetPagedAsync(parameters);
        return Ok(ApiResponse<PagedResult<SupplierSettlementDto>>.Ok(result));
    }

    /// <summary>查询供应商结算单明细。</summary>
    /// <param name="id">供应商结算单主键。</param>
    /// <returns>包含待结单据核销明细的供应商结算单。</returns>
    [HttpGet("{id:guid}")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<SupplierSettlementDto>>> GetById(Guid id)
    {
        var result = await service.GetByIdAsync(id);
        return Ok(ApiResponse<SupplierSettlementDto>.Ok(result));
    }

    /// <summary>根据结算单编号查询供应商结算单明细。</summary>
    /// <param name="settlementNo">结算单编号。</param>
    /// <returns>包含待结单据核销明细的供应商结算单。</returns>
    [HttpGet("by-no/{settlementNo}")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<SupplierSettlementDto>>> GetBySettlementNo(string settlementNo)
    {
        var result = await service.GetBySettlementNoAsync(settlementNo);
        return Ok(ApiResponse<SupplierSettlementDto>.Ok(result));
    }

    /// <summary>创建供应商结算单并回写待结单据余额。</summary>
    /// <param name="dto">结款日期、流水号、备注和待结单据核销明细。</param>
    /// <returns>创建后的供应商结算单。</returns>
    [HttpPost]
    [ResourcePermission(PermissionActions.Create)]
    public async Task<ActionResult<ApiResponse<SupplierSettlementDto>>> Create(
        [FromBody] CreateSupplierSettlementDto dto)
    {
        var result = await service.CreateAsync(dto);
        return Ok(ApiResponse<SupplierSettlementDto>.Ok(result));
    }

    /// <summary>作废供应商结算单并回滚已核销待结单据金额。</summary>
    /// <param name="id">供应商结算单主键。</param>
    /// <param name="dto">作废原因。</param>
    /// <returns>已作废的供应商结算单。</returns>
    [HttpDelete("{id:guid}/void")]
    [ResourcePermission(PermissionActions.Delete)]
    public async Task<ActionResult<ApiResponse<SupplierSettlementDto>>> Void(
        Guid id,
        [FromBody] VoidSupplierSettlementDto dto)
    {
        var result = await service.VoidAsync(id, dto);
        return Ok(ApiResponse<SupplierSettlementDto>.Ok(result));
    }
}
