using Application.DTOs.Pricing;
using Application.interfaces;
using Application.QueryParameters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SkyRoc.Controllers;

/// <summary>
///     客户协议价管理控制器。
/// </summary>
[Route("api/[controller]")]
[Authorize]
public class CustomerProtocolsController(ICustomerProtocolService service)
    : BaseDataController<CustomerProtocolDto, CreateCustomerProtocolDto, UpdateCustomerProtocolDto, CustomerProtocolQueryParameters>(service);
