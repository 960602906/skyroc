using Application.DTOs;
using Application.Interfaces;
using Application.QueryParameters;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using Shared.Constants;
using SkyRoc.Authorization;

namespace SkyRoc.Controllers;

/// <summary>
///     带名称和编码的基础资料控制器基类，在通用基础资料端点之上额外提供轻量下拉选项查询。
/// </summary>
public abstract class NamedCodeDataController<TDto, TCreateDto, TUpdateDto, TQuery>(
    INamedCodeBaseDataService<TDto, TCreateDto, TUpdateDto, TQuery> service)
    : BaseDataController<TDto, TCreateDto, TUpdateDto, TQuery>(service)
    where TDto : class
    where TUpdateDto : IHasId
    where TQuery : PagedQueryParameters
{
    /// <summary>
    ///     查询全部下拉选项。
    /// </summary>
    /// <returns>仅包含主键、名称和编码的下拉选项集合，不含明细字段。</returns>
    [HttpGet("options")]
    [ResourcePermission(PermissionActions.Read)]
    public virtual async Task<ActionResult<ApiResponse<List<NamedCodeOptionDto>>>> GetOptions()
    {
        var result = await service.GetOptionsAsync();
        return Ok(ApiResponse<List<NamedCodeOptionDto>>.Ok(result));
    }
}
