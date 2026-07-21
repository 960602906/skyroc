using Application.DTOs.Storage;
using Application.Interfaces;
using Application.QueryParameters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Constants;
using SkyRoc.Authorization;

namespace SkyRoc.Controllers;

/// <summary>
///     仓库管理控制器。
/// </summary>
[Route("api/wares")]
[Authorize]
[PermissionResource(PermissionCodes.Business.Storage.Resource)]
public class WaresController(IWareService service)
    : NamedCodeDataController<WareDto, CreateWareDto, UpdateWareDto, WareQueryParameters>(service);
