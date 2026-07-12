using Application.DTOs.Purchases;
using Application.interfaces;
using Application.QueryParameters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Constants;
using SkyRoc.Authorization;

namespace SkyRoc.Controllers;

/// <summary>
///     采购员管理控制器。
/// </summary>
[Route("api/purchasers")]
[Authorize]
[PermissionResource(PermissionCodes.Business.Purchases.Resource)]
public class PurchasersController(IPurchaserService service)
    : BaseDataController<PurchaserDto, CreatePurchaserDto, UpdatePurchaserDto, PurchaserQueryParameters>(service);
