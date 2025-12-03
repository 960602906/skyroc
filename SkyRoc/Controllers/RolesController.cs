using Application.DTOs.Role;
using Application.interfaces;
using Application.QueryParameters;
using Common.Constants;
using Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SkyRoc.Controllers;

/// <summary>
///     角色管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RolesController(IRoleService roleService) : ControllerBase
{
    /// <summary>
    ///     分页查询角色
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns></returns>
    [HttpGet("list")]
    public async Task<IActionResult> GetPagedMenus([FromQuery] RoleQueryParameters parameters)
    {
        var result = await roleService.GetPagedMenusAsync(parameters);
        return Ok(ApiResponse<PagedResult<RoleDto>>.Ok(result));
    }

    /// <summary>
    ///     查询所有角色
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<IActionResult> GetAllRoles()
    {
        var roles = await roleService.GetAllRolesAsync();
        return Ok(ApiResponse<IEnumerable<RoleDto>>.Ok(roles));
    }

    /// <summary>
    ///     根据id获取角色
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var roles = await roleService.GetRoleByIdAsync(id);
        return Ok(ApiResponse<RoleDto>.Ok(roles));
    }

    /// <summary>
    ///     创建角色
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoleDto dto)
    {
        // ✅ 方法 1：获取当前用户 ID
        // var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // ✅ 方法 2：获取用户名
        // var userName = User.FindFirst(ClaimTypes.Name)?.Value;
        var role = await roleService.CreateRoleAsync(dto);
        return Ok(ApiResponse<RoleDto>.Ok(role));
    }

    /// <summary>
    ///     更新角色
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateRoleDto dto)
    {
        await roleService.UpdateRoleAsync(dto.Id, dto);
        return Ok(ApiResponse<string>.Ok("Role updated successfully"));
    }

    /// <summary>
    ///     删除角色
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await roleService.DeleteRoleAsync(id);
        return Ok(ApiResponse<string>.Ok("Role deleted successfully"));
    }

    /// <summary>
    ///     批量删除角色
    /// </summary>
    /// <param name="roleIds"></param>
    /// <returns></returns>
    [HttpDelete("batchDelete")]
    public async Task<IActionResult> BatchDeleteRoles(List<Guid> roleIds)
    {
        await roleService.DeleteAllRolesAsync(roleIds);
        return Ok(ApiResponse<string>.Ok("Roles deleted successfully"));
    }


    /// <summary>
    ///     为角色分配菜单
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPost("assignMenus")]
    public async Task<IActionResult> AssignMenus([FromBody] AssignMenusDto dto)
    {
        await roleService.AssignMenusToRoleAsync(dto.RoleId, dto.MenuIds);
        return Ok(ApiResponse<string>.Ok("Role assigned successfully"));
    }

    /// <summary>
    ///     为角色删除菜单
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPost("unassignRoles")]
    public async Task<IActionResult> UnAssignMenus([FromBody] AssignMenusDto dto)
    {
        await roleService.RemoveMenusFromRoleAsync(dto.RoleId, dto.MenuIds);
        return Ok(ApiResponse<string>.Ok("Role unassigned successfully"));
    }
}