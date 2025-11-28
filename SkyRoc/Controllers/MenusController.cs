using Application.DTOs.Menu;
using Application.interfaces;
using Application.QueryParameters;
using Common.Constants;
using Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

/// <summary>
///     菜单管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MenusController(
    IMenuService menuService,
    ICurrentUserService currentUserService
) : ControllerBase
{
    /// <summary>
    ///     分页查询菜单
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns></returns>
    [HttpGet("list")]
    public async Task<IActionResult> GetPagedMenus([FromQuery] MenuQueryParameters parameters)
    {
        var result = await menuService.GetPagedMenusAsync(parameters);
        return Ok(ApiResponse<PagedResult<MenuDto>>.Ok(result));
    }

    /// <summary>
    ///     查询所有菜单
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<IActionResult> GetAllMenus()
    {
        var menus = await menuService.GetAllMenusAsync();
        return Ok(ApiResponse<List<MenuDto>>.Ok(menus));
    }
    
    /// <summary>
    ///   查询菜单树
    /// </summary>
    /// <returns></returns>
    [HttpGet("tree")]
    public async Task<IActionResult> GetAllMenusTreeAsync()
    {
        var menus = await menuService.GetAllMenusTreeAsync();
        return Ok(ApiResponse<List<MenuTreeDto>>.Ok(menus));
    }
    
    /// <summary>
    ///     根据id 获取菜单
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
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
    public async Task<IActionResult> Create([FromBody] CreateMenuDto dto)
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
    public async Task<IActionResult> Update([FromBody] UpdateMenuDto dto)
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
    public async Task<IActionResult> Delete(Guid id)
    {
        await menuService.DeleteMenuAsync(id);
        return Ok(ApiResponse<string>.Ok("Menu deleted successfully"));
    }
}