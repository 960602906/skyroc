using Application.DTOs;
using Application.DTOs.Pricing;
using Application.Interfaces;
using Application.QueryParameters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using Shared.Constants;
using SkyRoc.Authorization;

namespace SkyRoc.Controllers;

/// <summary>
///     报价单管理控制器。
/// </summary>
[Route("api/quotations")]
[Authorize]
[PermissionResource(PermissionCodes.Business.Pricing.Resource)]
public class QuotationsController(IQuotationService service)
    : NamedCodeDataController<QuotationDto, CreateQuotationDto, UpdateQuotationDto, QuotationQueryParameters>(service)
{
    /// <summary>
    ///     查询旧版全量轻量选项，仅为现有调用方保留一个过渡发布周期。
    /// </summary>
    /// <returns>全部报价单主键、名称和编码。</returns>
    [HttpGet("options")]
    [Authorize(Policy = PermissionCodes.Business.Pricing.Read)]
    [Obsolete("请使用 options/search、options/resolve 或 options/bounded。")]
    public async Task<ActionResult<ApiResponse<List<NamedCodeOptionDto>>>> GetLegacyOptions()
    {
        var result = await service.GetOptionsAsync();
        return Ok(ApiResponse<List<NamedCodeOptionDto>>.Ok(result));
    }

    /// <summary>
    ///     审核或反审核报价单。
    /// </summary>
    [HttpPatch("{id:guid}/audit")]
    [Authorize(Policy = PermissionCodes.Business.Pricing.Audit)]
    public async Task<ActionResult<ApiResponse<QuotationDto>>> ToggleAudit(Guid id, [FromQuery] bool isAudited)
    {
        var result = await service.ToggleAuditAsync(id, isAudited);
        return Ok(ApiResponse<QuotationDto>.Ok(result));
    }
}
