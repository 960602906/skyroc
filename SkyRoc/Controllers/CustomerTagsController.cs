using Application.DTOs.Customers;
using Application.Interfaces;
using Application.QueryParameters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using Shared.Constants;
using SkyRoc.Authorization;

namespace SkyRoc.Controllers;

/// <summary>
///     客户标签管理控制器。
/// </summary>
[Route("api/customer-tags")]
[Authorize]
[PermissionResource(PermissionCodes.Business.Customers.Resource)]
public class CustomerTagsController(ICustomerTagService service)
    : BaseDataController<CustomerTagDto, CreateCustomerTagDto, UpdateCustomerTagDto, CustomerTagQueryParameters>(service)
{
    /// <summary>
    ///     获取客户标签树。
    /// </summary>
    [HttpGet("tree")]
    [Authorize(Policy = PermissionCodes.Business.Customers.Read)]
    public async Task<ActionResult<ApiResponse<PagedResult<CustomerTagDto>>>> GetTree()
    {
        var result = await service.GetTreeAsync();
        return Ok(ApiResponse<PagedResult<CustomerTagDto>>.Ok(new PagedResult<CustomerTagDto>
        {
            Current = 1,
            Size = result.Count,
            Total = result.Count,
            Records = result
        }));
    }
}
