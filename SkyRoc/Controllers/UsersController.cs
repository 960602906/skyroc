using Application.DTOs.User;
using Application.interfaces;
using Application.QueryParameters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using Shared.Constants;

namespace SkyRoc.Controllers;

/// <summary>
///     用户管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController(IUserService userService) : ControllerBase
{
    /// <summary>
    ///     分页查询用户
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns></returns>
    [HttpGet("list")]
    public async Task<IActionResult> GetPagedMenus([FromQuery] UserQueryParameters parameters)
    {
        var result = await userService.GetPagedMenusAsync(parameters);
        return Ok(ApiResponse<PagedResult<UserDto>>.Ok(result));
    }

    /// <summary>
    ///     获取所有用户
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await userService.GetAllUsersAsync();
        return Ok(ApiResponse<IEnumerable<UserDto>>.Ok(users));
    }

    /// <summary>
    ///     根据id获取用户
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await userService.GetUserByIdAsync(id);
        return Ok(ApiResponse<UserDto>.Ok(user));
    }

    /// <summary>
    ///     创建用户
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Create([FromBody] CreateUserDto request)
    {
        var user = await userService.CreateUserAsync(request);
        return Ok(ApiResponse<UserDto>.Ok(user));
    }

    /// <summary>
    ///     更新用户
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateUserDto request)
    {
        await userService.UpdateUserAsync(request.Id, request);
        return Ok(ApiResponse<string>.Ok("User updated successfully"));
    }

    /// <summary>
    ///     删除用户
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await userService.DeleteUserAsync(id);
        return Ok(ApiResponse<string>.Ok("User deleted successfully"));
    }

    /// <summary>
    ///     批量删除用户
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    [HttpDelete("batchDelete")]
    public async Task<IActionResult> BatchDelete(List<Guid> ids)
    {
        await userService.DeleteUsersAsync(ids);
        return Ok(ApiResponse<string>.Ok("User deleted successfully"));
    }

    /// <summary>
    ///     为用户分配角色
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("assignRoles")]
    public async Task<IActionResult> AssignRoles([FromBody] AssignRolesDto request)
    {
        await userService.AssignRolesToUserAsync(request.UserId, request.RoleIds);
        return Ok(ApiResponse<string>.Ok("Roles assigned successfully"));
    }

    /// <summary>
    ///     为用户移除角色
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpDelete("unassignRoles")]
    public async Task<IActionResult> UnassignRoles([FromBody] AssignRolesDto request)
    {
        await userService.RemoveRolesFromUserAsync(request.UserId, request.RoleIds);
        return Ok(ApiResponse<string>.Ok("Roles unassigned successfully"));
    }
}