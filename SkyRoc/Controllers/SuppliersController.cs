using Application.DTOs.Purchases;
using Application.interfaces;
using Application.QueryParameters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SkyRoc.Controllers;

/// <summary>
///     供应商管理控制器。
/// </summary>
[Route("api/[controller]")]
[Authorize]
public class SuppliersController(ISupplierService service)
    : BaseDataController<SupplierDto, CreateSupplierDto, UpdateSupplierDto, SupplierQueryParameters>(service);
