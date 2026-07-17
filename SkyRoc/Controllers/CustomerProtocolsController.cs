using Application.DTOs.Pricing;
using Application.interfaces;
using Application.QueryParameters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Constants;
using SkyRoc.Authorization;

namespace SkyRoc.Controllers;

/// <summary>
///     客户协议价管理控制器。
/// </summary>
[Route("api/customer-protocols")]
[Authorize]
[PermissionResource(PermissionCodes.Business.Pricing.Resource)]
public class CustomerProtocolsController(ICustomerProtocolService service)
    : NamedCodeDataController<CustomerProtocolDto, CreateCustomerProtocolDto, UpdateCustomerProtocolDto, CustomerProtocolQueryParameters>(service);
