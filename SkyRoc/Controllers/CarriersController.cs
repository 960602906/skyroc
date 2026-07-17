using Application.DTOs.Delivery;
using Application.Interfaces;
using Application.QueryParameters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Constants;
using SkyRoc.Authorization;

namespace SkyRoc.Controllers;

/// <summary>
///     承运商管理控制器。
/// </summary>
[Route("api/carriers")]
[Authorize]
[PermissionResource(PermissionCodes.Business.Delivery.Resource)]
public class CarriersController(ICarrierService service)
    : BaseDataController<CarrierDto, CreateCarrierDto, UpdateCarrierDto, CarrierQueryParameters>(service);
