using Application.DTOs;
using Application.interfaces;
using Application.QueryParameters;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using Shared.Constants;
using SkyRoc.Authorization;

namespace SkyRoc.Controllers;

/// <summary>
///     基础资料控制器基类。
/// </summary>
[ApiController]
public abstract class BaseDataController<TDto, TCreateDto, TUpdateDto, TQuery>(
    IBaseDataService<TDto, TCreateDto, TUpdateDto, TQuery> service) : ControllerBase
    where TDto : class
    where TUpdateDto : IHasId
    where TQuery : PagedQueryParameters
{
    /// <summary>
    ///     分页查询。
    /// </summary>
    [HttpGet("list")]
    [ResourcePermission(PermissionActions.Read)]
    public virtual async Task<IActionResult> GetPaged([FromQuery] TQuery parameters)
    {
        var result = await service.GetPagedAsync(parameters);
        return Ok(ApiResponse<PagedResult<TDto>>.Ok(result));
    }

    /// <summary>
    ///     查询全部。
    /// </summary>
    [HttpGet]
    [ResourcePermission(PermissionActions.Read)]
    public virtual async Task<IActionResult> GetAll()
    {
        var result = await service.GetAllAsync();
        return Ok(ApiResponse<List<TDto>>.Ok(result));
    }

    /// <summary>
    ///     根据 ID 查询。
    /// </summary>
    [HttpGet("{id:guid}")]
    [ResourcePermission(PermissionActions.Read)]
    public virtual async Task<IActionResult> GetById(Guid id)
    {
        var result = await service.GetByIdAsync(id);
        return Ok(ApiResponse<TDto>.Ok(result));
    }

    /// <summary>
    ///     创建。
    /// </summary>
    [HttpPost]
    [ResourcePermission(PermissionActions.Create)]
    public virtual async Task<IActionResult> Create([FromBody] TCreateDto dto)
    {
        var result = await service.CreateAsync(dto);
        return Ok(ApiResponse<TDto>.Ok(result));
    }

    /// <summary>
    ///     更新。
    /// </summary>
    [HttpPut]
    [ResourcePermission(PermissionActions.Update)]
    public virtual async Task<IActionResult> Update([FromBody] TUpdateDto dto)
    {
        var result = await service.UpdateAsync(dto.Id, dto);
        return Ok(ApiResponse<TDto>.Ok(result));
    }

    /// <summary>
    ///     删除。
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ResourcePermission(PermissionActions.Delete)]
    public virtual async Task<IActionResult> Delete(Guid id)
    {
        var result = await service.DeleteAsync(id);
        return Ok(ApiResponse<bool>.Ok(result));
    }

    /// <summary>
    ///     批量删除。
    /// </summary>
    [HttpDelete("batchDelete")]
    [ResourcePermission(PermissionActions.Delete)]
    public virtual async Task<IActionResult> BatchDelete([FromBody] List<Guid> ids)
    {
        var result = await service.BatchDeleteAsync(ids);
        return Ok(ApiResponse<bool>.Ok(result));
    }

    /// <summary>
    ///     启用或禁用。
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [ResourcePermission(PermissionActions.Update)]
    public virtual async Task<IActionResult> ToggleStatus(Guid id, [FromQuery] Status status)
    {
        var result = await service.ToggleStatusAsync(id, status);
        return Ok(ApiResponse<TDto>.Ok(result));
    }
}
