using Application.DTOs.Role;
using Application.interfaces;
using Application.QueryParameters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using Shared.Constants;

namespace SkyRoc.Controllers;

/// <summary>
///     角色管理控制器
/// </summary>
[ApiController]
[Route("api/roles")]
[Authorize]
public class RolesController(IRoleService roleService) : ControllerBase
{
    /// <summary>
    ///     分页查询角色
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns></returns>
    [HttpGet("list")]
    [Authorize(Policy = PermissionCodes.System.Roles.Read)]
    public async Task<ActionResult<ApiResponse<PagedResult<RoleDto>>>> GetPagedMenus([FromQuery] RoleQueryParameters parameters)
    {
        var result = await roleService.GetPagedMenusAsync(parameters);
        return Ok(ApiResponse<PagedResult<RoleDto>>.Ok(result));
    }

    /// <summary>
    ///     查询所有角色
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Authorize(Policy = PermissionCodes.System.Roles.Read)]
    public async Task<ActionResult<ApiResponse<IEnumerable<RoleDto>>>> GetAllRoles()
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
    [Authorize(Policy = PermissionCodes.System.Roles.Read)]
    public async Task<ActionResult<ApiResponse<RoleDto>>> GetById(Guid id)
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
    [Authorize(Policy = PermissionCodes.System.Roles.Create)]
    public async Task<ActionResult<ApiResponse<RoleDto>>> Create([FromBody] CreateRoleDto dto)
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
    [Authorize(Policy = PermissionCodes.System.Roles.Update)]
    public async Task<ActionResult<ApiResponse<string>>> Update([FromBody] UpdateRoleDto dto)
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
    [Authorize(Policy = PermissionCodes.System.Roles.Delete)]
    public async Task<ActionResult<ApiResponse<string>>> Delete(Guid id)
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
    [Authorize(Policy = PermissionCodes.System.Roles.Delete)]
    public async Task<ActionResult<ApiResponse<string>>> BatchDeleteRoles(List<Guid> roleIds)
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
    [Authorize(Policy = PermissionCodes.System.Roles.AssignMenus)]
    public async Task<ActionResult<ApiResponse<string>>> AssignMenus([FromBody] AssignMenusDto dto)
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
    [Authorize(Policy = PermissionCodes.System.Roles.AssignMenus)]
    public async Task<ActionResult<ApiResponse<string>>> UnAssignMenus([FromBody] AssignMenusDto dto)
    {
        await roleService.RemoveMenusFromRoleAsync(dto.RoleId, dto.MenuIds);
        return Ok(ApiResponse<string>.Ok("Role unassigned successfully"));
    }
}
