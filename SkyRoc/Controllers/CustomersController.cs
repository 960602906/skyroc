using Application.DTOs.Customers;
using Application.interfaces;
using Application.QueryParameters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SkyRoc.Controllers;

/// <summary>
///     客户管理控制器。
/// </summary>
[Route("api/[controller]")]
[Authorize]
public class CustomersController(ICustomerService service)
    : BaseDataController<CustomerDto, CreateCustomerDto, UpdateCustomerDto, CustomerQueryParameters>(service);
