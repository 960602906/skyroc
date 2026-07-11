using Application.DTOs.Printing;
using Application.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using Shared.Constants;
using SkyRoc.Authorization;

namespace SkyRoc.Controllers;

/// <summary>
/// 打印模板管理控制器，提供设计 JSON、字段定义和启用状态的受保护维护接口。
/// </summary>
[ApiController]
[Route("api/print-templates")]
[Authorize]
[PermissionResource(PermissionCodes.System.PrintTemplates.Resource)]
public class PrintTemplatesController(IPrintService service) : ControllerBase
{
    /// <summary>分页查询打印模板。</summary>
    /// <param name="pageNumber">从 1 开始的页码。</param>
    /// <param name="pageSize">每页模板数量，最大 100。</param>
    /// <returns>模板分页数据。</returns>
    [HttpGet]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<PagedResult<PrintTemplateDto>>>> GetPaged(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await service.GetTemplatesAsync(pageNumber, pageSize);
        return Ok(ApiResponse<PagedResult<PrintTemplateDto>>.Ok(result));
    }

    /// <summary>按稳定模板编码查询模板和字段定义。</summary>
    /// <param name="templateCode">打印模板稳定业务编码。</param>
    /// <returns>模板设计 JSON 和字段集合。</returns>
    [HttpGet("by-code/{templateCode}")]
    [ResourcePermission(PermissionActions.Read)]
    public async Task<ActionResult<ApiResponse<PrintTemplateDto>>> GetByCode(string templateCode)
    {
        var result = await service.GetTemplateByCodeAsync(templateCode);
        return Ok(ApiResponse<PrintTemplateDto>.Ok(result));
    }

    /// <summary>新增打印模板和完整字段定义。</summary>
    /// <param name="dto">模板编码、业务类型、设计 JSON、字段集合和启用状态。</param>
    /// <returns>创建后的模板完整配置。</returns>
    [HttpPost]
    [ResourcePermission(PermissionActions.Create)]
    public async Task<ActionResult<ApiResponse<PrintTemplateDto>>> Create([FromBody] CreatePrintTemplateDto dto)
    {
        var result = await service.CreateTemplateAsync(dto);
        return Ok(ApiResponse<PrintTemplateDto>.Ok(result));
    }

    /// <summary>完整更新打印模板，传入字段集合将替换原字段定义。</summary>
    /// <param name="dto">含模板主键的完整替换请求。</param>
    /// <returns>更新后的模板完整配置。</returns>
    [HttpPut]
    [ResourcePermission(PermissionActions.Update)]
    public async Task<ActionResult<ApiResponse<PrintTemplateDto>>> Update([FromBody] UpdatePrintTemplateDto dto)
    {
        var result = await service.UpdateTemplateAsync(dto);
        return Ok(ApiResponse<PrintTemplateDto>.Ok(result));
    }

    /// <summary>删除打印模板及其字段定义。</summary>
    /// <param name="id">待删除的模板主键。</param>
    /// <returns>删除成功标记。</returns>
    [HttpDelete("{id:guid}")]
    [ResourcePermission(PermissionActions.Delete)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        var result = await service.DeleteTemplateAsync(id);
        return Ok(ApiResponse<bool>.Ok(result));
    }
}
