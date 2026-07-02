using Application.DTOs.Department;
using Application.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using Shared.Constants;

namespace SkyRoc.Controllers;

/// <summary>
///     部门管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DepartmentsController(IDepartmentService departmentService) : ControllerBase
{
    /// <summary>
    ///     获取部门树
    /// </summary>
    [HttpGet("tree")]
    [Authorize(Policy = PermissionCodes.System.Departments.Read)]
    public async Task<IActionResult> GetTree()
    {
        var departments = await departmentService.GetDepartmentTreeAsync();
        return Ok(ApiResponse<PagedResult<DepartmentTreeDto>>.Ok(new PagedResult<DepartmentTreeDto>
        {
            Current = 1,
            Size = departments.Count,
            Total = departments.Count,
            Records = departments
        }));
    }

    /// <summary>
    ///     根据 id 获取部门
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = PermissionCodes.System.Departments.Read)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var department = await departmentService.GetByIdAsync(id);
        return Ok(ApiResponse<DepartmentDto>.Ok(department));
    }

    /// <summary>
    ///     创建部门
    /// </summary>
    [HttpPost]
    [Authorize(Policy = PermissionCodes.System.Departments.Create)]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentDto dto)
    {
        var department = await departmentService.CreateAsync(dto);
        return Ok(ApiResponse<DepartmentDto>.Ok(department));
    }

    /// <summary>
    ///     更新部门
    /// </summary>
    [HttpPut]
    [Authorize(Policy = PermissionCodes.System.Departments.Update)]
    public async Task<IActionResult> Update([FromBody] UpdateDepartmentDto dto)
    {
        var department = await departmentService.UpdateAsync(dto.Id, dto);
        return Ok(ApiResponse<DepartmentDto>.Ok(department, "Department updated successfully"));
    }

    /// <summary>
    ///     删除部门
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = PermissionCodes.System.Departments.Delete)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await departmentService.DeleteAsync(id);
        return Ok(ApiResponse<bool>.Ok(deleted));
    }

    /// <summary>
    ///     批量删除部门
    /// </summary>
    [HttpDelete("batchDelete")]
    [Authorize(Policy = PermissionCodes.System.Departments.Delete)]
    public async Task<IActionResult> BatchDelete([FromBody] List<Guid> ids)
    {
        var deleted = await departmentService.BatchDeleteAsync(ids);
        return Ok(ApiResponse<bool>.Ok(deleted));
    }

    /// <summary>
    ///     切换部门状态
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = PermissionCodes.System.Departments.Update)]
    public async Task<IActionResult> ToggleStatus(Guid id, [FromQuery] Status status)
    {
        var department = await departmentService.ToggleStatusAsync(id, status);
        return Ok(ApiResponse<DepartmentDto>.Ok(department));
    }

    /// <summary>
    ///     获取部门下的用户列表
    /// </summary>
    [HttpGet("{id:guid}/users")]
    [Authorize(Policy = PermissionCodes.System.Departments.Read)]
    public async Task<IActionResult> GetUsers(Guid id)
    {
        var users = await departmentService.GetUsersAsync(id);
        return Ok(ApiResponse<object>.Ok(new
        {
            departmentId = id,
            total = users.Count,
            records = users
        }));
    }
}
