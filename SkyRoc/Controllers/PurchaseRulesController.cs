using Application.DTOs.Purchases;
using Application.Interfaces;
using Application.QueryParameters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Constants;
using SkyRoc.Authorization;

namespace SkyRoc.Controllers;

/// <summary>
///     采购规则管理控制器。
/// </summary>
[Route("api/purchase-rules")]
[Authorize]
[PermissionResource(PermissionCodes.Business.Purchases.Resource)]
public class PurchaseRulesController(IPurchaseRuleService service)
    : NamedCodeDataController<PurchaseRuleDto, CreatePurchaseRuleDto, UpdatePurchaseRuleDto, PurchaseRuleQueryParameters>(service);
