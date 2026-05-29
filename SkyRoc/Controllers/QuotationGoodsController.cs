using Application.DTOs.Pricing;
using Application.interfaces;
using Application.QueryParameters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SkyRoc.Controllers;

/// <summary>
///     报价商品管理控制器。
/// </summary>
[Route("api/[controller]")]
[Authorize]
public class QuotationGoodsController(IQuotationGoodsService service)
    : BaseDataController<QuotationGoodsDto, CreateQuotationGoodsDto, UpdateQuotationGoodsDto, QuotationGoodsQueryParameters>(service);
