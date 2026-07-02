using Application.DTOs.Purchases;
using Application.interfaces;
using Application.QueryParameters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Constants;
using SkyRoc.Authorization;

namespace SkyRoc.Controllers;

/// <summary>
///     采购规则管理控制器。
/// </summary>
[Route("api/[controller]")]
[Authorize]
[PermissionResource(PermissionCodes.Business.Purchases.Resource)]
public class PurchaseRulesController(IPurchaseRuleService service)
    : BaseDataController<PurchaseRuleDto, CreatePurchaseRuleDto, UpdatePurchaseRuleDto, PurchaseRuleQueryParameters>(service);
