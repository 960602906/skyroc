using Application.DTOs.Printing;
using Application.interfaces;
using Domain.Entities.Printing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using Shared.Constants;
using SkyRoc.Authorization;

namespace SkyRoc.Controllers;

/// <summary>
/// 业务打印数据控制器，读取订单、采购、出入库和结算快照，并显式确认正式打印状态。
/// </summary>
[ApiController]
[Route("api/print-data")]
[Authorize]
[PermissionResource(PermissionCodes.Business.Printing.Resource)]
public class PrintDataController(IPrintService service) : ControllerBase
{
    /// <summary>读取指定业务单据的打印数据；预览读取不会修改来源单据状态。</summary>
    /// <param name="businessType">业务单据类型：1 销售订单、2 采购单、3 入库单、4 出库单、5 客户结款、6 供应商结算。</param>
    /// <param name="ids">来源单据主键集合，单次最多 100 个且不能重复。</param>
    /// <returns>与请求主键顺序一致的打印数据集合。</returns>
    [HttpGet("{businessType:int}")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PrintDocumentDto>>>> GetData(
        PrintBusinessType businessType,
        [FromQuery] List<Guid> ids)
    {
        var result = await service.GetDataAsync(businessType, ids);
        return Ok(ApiResponse<IReadOnlyList<PrintDocumentDto>>.Ok(result));
    }

    /// <summary>确认订单、入库单或出库单已完成正式打印；不适用于采购单和结算单。</summary>
    /// <param name="businessType">支持状态维护的业务类型：1 销售订单、3 入库单、4 出库单。</param>
    /// <param name="dto">实际完成打印的来源单据主键集合。</param>
    /// <returns>确认成功标记。</returns>
    [HttpPost("{businessType:int}/confirm")]
    [ResourcePermission(PermissionActions.Update)]
    public async Task<ActionResult<ApiResponse<bool>>> ConfirmPrinted(
        PrintBusinessType businessType,
        [FromBody] ConfirmPrintDto dto)
    {
        await service.ConfirmPrintedAsync(businessType, dto.Ids);
        return Ok(ApiResponse<bool>.Ok(true));
    }
}
