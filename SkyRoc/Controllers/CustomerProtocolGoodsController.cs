using Application.DTOs.Pricing;
using Application.Interfaces;
using Application.QueryParameters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Constants;
using SkyRoc.Authorization;

namespace SkyRoc.Controllers;

/// <summary>
///     客户协议价商品管理控制器。
/// </summary>
[Route("api/customer-protocol-goods")]
[Authorize]
[PermissionResource(PermissionCodes.Business.Pricing.Resource)]
public class CustomerProtocolGoodsController(ICustomerProtocolGoodsService service)
    : BaseDataController<CustomerProtocolGoodsDto, CreateCustomerProtocolGoodsDto, UpdateCustomerProtocolGoodsDto, CustomerProtocolGoodsQueryParameters>(service);
