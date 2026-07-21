using Application.DTOs.Purchases;
using Application.Interfaces;
using Application.QueryParameters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Constants;
using SkyRoc.Authorization;

namespace SkyRoc.Controllers;

/// <summary>
///     供应商管理控制器。
/// </summary>
[Route("api/suppliers")]
[Authorize]
[PermissionResource(PermissionCodes.Business.Purchases.Resource)]
public class SuppliersController(ISupplierService service)
    : NamedCodeDataController<SupplierDto, CreateSupplierDto, UpdateSupplierDto, SupplierQueryParameters>(service);
