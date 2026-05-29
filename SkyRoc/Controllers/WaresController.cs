using Application.DTOs.Storage;
using Application.interfaces;
using Application.QueryParameters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SkyRoc.Controllers;

/// <summary>
///     仓库管理控制器。
/// </summary>
[Route("api/[controller]")]
[Authorize]
public class WaresController(IWareService service)
    : BaseDataController<WareDto, CreateWareDto, UpdateWareDto, WareQueryParameters>(service);
