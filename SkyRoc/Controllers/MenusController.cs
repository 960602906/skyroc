using Application.DTOs.Menu;
using Application.Interfaces;
using Application.QueryParameters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using Shared.Constants;

namespace SkyRoc.Controllers;

/// <summary>
///     菜单管理控制器
/// </summary>
[ApiController]
[Route("api/menus")]
[Authorize]
public class MenusController(
    IMenuService menuService
) : ControllerBase
{
    /// <summary>
    ///     分页查询菜单
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns></returns>
    [HttpGet("list")]
    [Authorize(Policy = PermissionCodes.System.Menus.Read)]
    public async Task<ActionResult<ApiResponse<PagedResult<MenuDto>>>> GetPagedMenus([FromQuery] MenuQueryParameters parameters)
    {
        var result = await menuService.GetPagedMenusAsync(parameters);
        return Ok(ApiResponse<PagedResult<MenuDto>>.Ok(result));
    }

    /// <summary>
    ///     查询所有菜单
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Authorize(Policy = PermissionCodes.System.Menus.Read)]
    public async Task<ActionResult<ApiResponse<List<MenuDto>>>> GetAllMenus()
    {
        var menus = await menuService.GetAllMenusAsync();
        return Ok(ApiResponse<List<MenuDto>>.Ok(menus));
    }

    /// <summary>
    ///     查询菜单树
    /// </summary>
    /// <returns></returns>
    [HttpGet("tree")]
    [Authorize(Policy = PermissionCodes.System.Menus.Read)]
    public async Task<ActionResult<ApiResponse<PagedResult<MenuTreeDto>>>> GetAllMenusTreeAsync()
    {
        var menus = await menuService.GetAllMenusTreeAsync();
        return Ok(ApiResponse<PagedResult<MenuTreeDto>>.Ok(new PagedResult<MenuTreeDto>
        {
            Current = 1,
            Size = menus.Count,
            Total = menus.Count,
            Records = menus
        }));
    }

    /// <summary>
    ///     根据id 获取菜单
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = PermissionCodes.System.Menus.Read)]
    public async Task<ActionResult<ApiResponse<MenuDto>>> GetById(Guid id)
    {
        var menu = await menuService.GetMenuByIdAsync(id);
        return Ok(ApiResponse<MenuDto>.Ok(menu));
    }

    /// <summary>
    ///     创建菜单
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPost]
    [Authorize(Policy = PermissionCodes.System.Menus.Create)]
    public async Task<ActionResult<ApiResponse<MenuDto>>> Create([FromBody] CreateMenuDto dto)
    {
        var menu = await menuService.CreateMenuAsync(dto);
        return Ok(ApiResponse<MenuDto>.Ok(menu));
    }

    /// <summary>
    ///     更新菜单
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPut]
    [Authorize(Policy = PermissionCodes.System.Menus.Update)]
    public async Task<ActionResult<ApiResponse<string>>> Update([FromBody] UpdateMenuDto dto)
    {
        await menuService.UpdateMenuAsync(dto.Id, dto);
        return Ok(ApiResponse<string>.Ok("Menu updated successfully"));
    }

    /// <summary>
    ///     删除菜单
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = PermissionCodes.System.Menus.Delete)]
    public async Task<ActionResult<ApiResponse<string>>> Delete(Guid id)
    {
        await menuService.DeleteMenuAsync(id);
        return Ok(ApiResponse<string>.Ok("Menu deleted successfully"));
    }

    /// <summary>
    ///     批量删除菜单
    /// </summary>
    /// <param name="menuIds"></param>
    /// <returns></returns>
    [HttpDelete("batchDelete")]
    [Authorize(Policy = PermissionCodes.System.Menus.Delete)]
    public async Task<ActionResult<ApiResponse<string>>> BatchDelete([FromBody] List<Guid> menuIds)
    {
        await menuService.DeleteMenusAsync(menuIds);
        return Ok(ApiResponse<string>.Ok("Menus deleted successfully"));
    }
}
