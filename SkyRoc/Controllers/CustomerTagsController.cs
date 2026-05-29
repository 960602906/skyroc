using Application.DTOs.Customers;
using Application.interfaces;
using Application.QueryParameters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using Shared.Constants;

namespace SkyRoc.Controllers;

/// <summary>
///     客户标签管理控制器。
/// </summary>
[Route("api/[controller]")]
[Authorize]
public class CustomerTagsController(ICustomerTagService service)
    : BaseDataController<CustomerTagDto, CreateCustomerTagDto, UpdateCustomerTagDto, CustomerTagQueryParameters>(service)
{
    /// <summary>
    ///     获取客户标签树。
    /// </summary>
    [HttpGet("tree")]
    public async Task<IActionResult> GetTree()
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
