using Application.DTOs.Customers;
using Application.Interfaces;
using Application.QueryParameters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Constants;
using SkyRoc.Authorization;

namespace SkyRoc.Controllers;

/// <summary>
///     客户管理控制器。
/// </summary>
[Route("api/customers")]
[Authorize]
[PermissionResource(PermissionCodes.Business.Customers.Resource)]
public class CustomersController(ICustomerService service)
    : NamedCodeDataController<CustomerDto, CreateCustomerDto, UpdateCustomerDto, CustomerQueryParameters>(service);
