using Application.DTOs.Pricing;
using Application.Interfaces;
using Application.QueryParameters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Constants;
using SkyRoc.Authorization;

namespace SkyRoc.Controllers;

/// <summary>
///     报价商品管理控制器。
/// </summary>
[Route("api/quotation-goods")]
[Authorize]
[PermissionResource(PermissionCodes.Business.Pricing.Resource)]
public class QuotationGoodsController(IQuotationGoodsService service)
    : BaseDataController<QuotationGoodsDto, CreateQuotationGoodsDto, UpdateQuotationGoodsDto, QuotationGoodsQueryParameters>(service);
