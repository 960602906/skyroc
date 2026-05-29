using Application.DTOs.Purchases;
using Application.interfaces;
using Application.QueryParameters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SkyRoc.Controllers;

/// <summary>
///     采购员管理控制器。
/// </summary>
[Route("api/[controller]")]
[Authorize]
public class PurchasersController(IPurchaserService service)
    : BaseDataController<PurchaserDto, CreatePurchaserDto, UpdatePurchaserDto, PurchaserQueryParameters>(service);
