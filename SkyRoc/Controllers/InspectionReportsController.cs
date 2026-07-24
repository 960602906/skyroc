using Application.DTOs.Traceability;
using Application.Interfaces;
using Application.QueryParameters.Traceability;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using Shared.Constants;
using SkyRoc.Authorization;

namespace SkyRoc.Controllers;

/// <summary>检测报告控制器，提供已审核采购入库商品选择、报告维护和历史报告查询。</summary>
[ApiController]
[Route("api/traceability/inspection-reports")]
[Authorize]
[PermissionResource(PermissionCodes.Business.Traceability.Resource)]
public class InspectionReportsController(ITraceabilityService service) : ControllerBase
{
    /// <summary>分页查询检测报告。</summary>
    /// <param name="parameters">仓库、供应商、结论、检测时间和关键字筛选条件。</param>
    /// <returns>检测报告分页结果。</returns>
    [HttpGet]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<PagedResult<InspectionReportDto>>>> GetPaged(
        [FromQuery] InspectionReportQueryParameters parameters)
    {
        return Ok(ApiResponse<PagedResult<InspectionReportDto>>.Ok(await service.GetInspectionReportsAsync(parameters)));
    }

    /// <summary>读取检测报告详情。</summary>
    /// <param name="id">检测报告主键。</param>
    /// <returns>包含送检商品和附件的检测报告。</returns>
    [HttpGet("{id:guid}")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<InspectionReportDto>>> GetById(Guid id)
    {
        return Ok(ApiResponse<InspectionReportDto>.Ok(await service.GetInspectionReportByIdAsync(id)));
    }

    /// <summary>根据检测报告编号读取检测报告详情。</summary>
    /// <param name="inspectionNo">检测报告编号。</param>
    /// <returns>包含送检商品和附件的检测报告。</returns>
    [HttpGet("by-no/{inspectionNo}")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<InspectionReportDto>>> GetByNo(string inspectionNo)
    {
        return Ok(ApiResponse<InspectionReportDto>.Ok(await service.GetInspectionReportByNoAsync(inspectionNo)));
    }

    /// <summary>分页查询可创建检测报告的已审核采购入库单。</summary>
    /// <param name="parameters">标准分页参数。</param>
    /// <returns>已审核采购入库单分页结果。</returns>
    [HttpGet("eligible-stock-ins")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<PagedResult<InspectionStockInOrderDto>>>> GetEligibleStockIns(
        [FromQuery] InspectionStockInOrderQueryParameters parameters)
    {
        return Ok(ApiResponse<PagedResult<InspectionStockInOrderDto>>.Ok(
            await service.GetEligibleStockInOrdersAsync(parameters)));
    }

    /// <summary>读取指定已审核采购入库单的商品明细，供客户端选择送检商品。</summary>
    /// <param name="stockInOrderId">采购入库单主键。</param>
    /// <returns>入库商品、单位、数量和批次快照列表。</returns>
    [HttpGet("eligible-stock-ins/{stockInOrderId:guid}/details")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<InspectionStockInDetailDto>>>> GetEligibleStockInDetails(Guid stockInOrderId)
    {
        return Ok(ApiResponse<IReadOnlyList<InspectionStockInDetailDto>>.Ok(
            await service.GetEligibleStockInDetailsAsync(stockInOrderId)));
    }

    /// <summary>创建检测报告并固化采购入库、商品、仓库和供应商快照。</summary>
    /// <param name="dto">来源采购入库、检测信息、送检商品和已上传附件元数据。</param>
    /// <returns>创建后的检测报告。</returns>
    [HttpPost]
    [ResourcePermission(PermissionActions.Create)]
    public async Task<ActionResult<ApiResponse<InspectionReportDto>>> Create([FromBody] SaveInspectionReportDto dto)
    {
        return Ok(ApiResponse<InspectionReportDto>.Ok(await service.CreateInspectionReportAsync(dto)));
    }

    /// <summary>更新未被溯源引用的检测报告；一旦生成二维码溯源，报告全文冻结不可修改。</summary>
    /// <param name="id">检测报告主键。</param>
    /// <param name="dto">更新后的检测信息、送检商品和附件元数据。</param>
    /// <returns>更新后的检测报告。</returns>
    [HttpPut("{id:guid}")]
    [ResourcePermission(PermissionActions.Update)]
    public async Task<ActionResult<ApiResponse<InspectionReportDto>>> Update(Guid id, [FromBody] SaveInspectionReportDto dto)
    {
        return Ok(ApiResponse<InspectionReportDto>.Ok(await service.UpdateInspectionReportAsync(id, dto)));
    }

    /// <summary>删除未被商品溯源引用的检测报告及其附件。</summary>
    /// <param name="id">检测报告主键。</param>
    /// <returns>删除成功标识。</returns>
    [HttpDelete("{id:guid}")]
    [ResourcePermission(PermissionActions.Delete)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        await service.DeleteInspectionReportAsync(id);
        return Ok(ApiResponse<bool>.Ok(true));
    }
}
