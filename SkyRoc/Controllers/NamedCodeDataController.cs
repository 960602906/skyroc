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
    ///     按名称或编码限量搜索选择项。
    /// </summary>
    /// <param name="parameters">关键词和返回数量；空关键词默认返回前 20 条。</param>
    /// <returns>轻量选择项和是否仍有更多匹配项。</returns>
    [HttpGet("options/search")]
    [ResourcePermission(PermissionActions.Read)]
    public virtual async Task<ActionResult<ApiResponse<SelectionOptionSearchResultDto>>> SearchOptions(
        [FromQuery] SelectionOptionSearchQueryParameters parameters)
    {
        var result = await service.SearchSelectionOptionsAsync(parameters);
        return Ok(ApiResponse<SelectionOptionSearchResultDto>.Ok(result));
    }

    /// <summary>
    ///     按主键集合解析已选项显示文本。
    /// </summary>
    /// <param name="parameters">已选业务主键集合，单次最多 100 个。</param>
    /// <returns>存在的轻量选择项。</returns>
    [HttpGet("options/resolve")]
    [ResourcePermission(PermissionActions.Read)]
    public virtual async Task<ActionResult<ApiResponse<List<SelectionOptionDto>>>> ResolveOptions(
        [FromQuery] SelectionOptionResolveQueryParameters parameters)
    {
        var result = await service.ResolveSelectionOptionsAsync(parameters.Ids);
        return Ok(ApiResponse<List<SelectionOptionDto>>.Ok(result));
    }

    /// <summary>
    ///     获取有明确业务边界的轻量选择项。
    /// </summary>
    /// <returns>不超过 500 条的轻量选择项；越界时返回业务校验失败。</returns>
    [HttpGet("options/bounded")]
    [ResourcePermission(PermissionActions.Read)]
    public virtual async Task<ActionResult<ApiResponse<List<SelectionOptionDto>>>> GetBoundedOptions()
    {
        var result = await service.GetBoundedSelectionOptionsAsync();
        return Ok(ApiResponse<List<SelectionOptionDto>>.Ok(result));
    }
}
