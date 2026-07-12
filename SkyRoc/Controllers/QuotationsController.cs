using Application.DTOs.Pricing;
using Application.interfaces;
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
    : BaseDataController<QuotationDto, CreateQuotationDto, UpdateQuotationDto, QuotationQueryParameters>(service)
{
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
