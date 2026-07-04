using Application.DTOs.Delivery;
using Application.interfaces;
using Application.QueryParameters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Constants;
using SkyRoc.Authorization;

namespace SkyRoc.Controllers;

/// <summary>
///     司机管理控制器。
/// </summary>
[Route("api/[controller]")]
[Authorize]
[PermissionResource(PermissionCodes.Business.Delivery.Resource)]
public class DriversController(IDriverService service)
    : BaseDataController<DriverDto, CreateDriverDto, UpdateDriverDto, DriverQueryParameters>(service);
