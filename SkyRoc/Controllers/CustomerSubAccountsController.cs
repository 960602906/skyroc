using Application.DTOs.Customers;
using Application.interfaces;
using Application.QueryParameters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Constants;
using SkyRoc.Authorization;

namespace SkyRoc.Controllers;

/// <summary>
///     客户子账号管理控制器。
/// </summary>
[Route("api/[controller]")]
[Authorize]
[PermissionResource(PermissionCodes.Business.Customers.Resource)]
public class CustomerSubAccountsController(ICustomerSubAccountService service)
    : BaseDataController<CustomerSubAccountDto, CreateCustomerSubAccountDto, UpdateCustomerSubAccountDto, CustomerSubAccountQueryParameters>(service);
