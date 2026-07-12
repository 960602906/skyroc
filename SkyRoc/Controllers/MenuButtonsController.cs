using Application.DTOs.MenuButton;
using Application.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using Shared.Constants;

namespace SkyRoc.Controllers;

/// <summary>
///     菜单按钮管理控制器
/// </summary>
[ApiController]
[Route("api/menu-buttons")]
[Authorize]
public class MenuButtonsController(IMenuButtonService menuButtonService) : ControllerBase
{
    /// <summary>
    ///     根据 id 获取菜单按钮
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = PermissionCodes.System.MenuButtons.Read)]
    public async Task<ActionResult<ApiResponse<MenuButtonDto>>> GetById(Guid id)
    {
        var menuButton = await menuButtonService.GetMenuButtonAsync(id);
        return Ok(ApiResponse<MenuButtonDto>.Ok(menuButton));
    }

    /// <summary>
    ///     创建菜单按钮
    /// </summary>
    [HttpPost]
    [Authorize(Policy = PermissionCodes.System.MenuButtons.Create)]
    public async Task<ActionResult<ApiResponse<MenuButtonDto>>> Create([FromBody] CreateMenuButtonDto dto)
    {
        var menuButton = await menuButtonService.CreateMenuButtonAsync(dto);
        return Ok(ApiResponse<MenuButtonDto>.Ok(menuButton));
    }

    /// <summary>
    ///     批量创建菜单按钮
    /// </summary>
    [HttpPost("batch")]
    [Authorize(Policy = PermissionCodes.System.MenuButtons.Create)]
    public async Task<ActionResult<ApiResponse<List<MenuButtonDto>>>> BatchCreate([FromQuery] Guid menuId, [FromBody] List<CreateMenuButtonDto> dtos)
    {
        var menuButtons = await menuButtonService.CreateMenuButtonsAsync(menuId, dtos);
        return Ok(ApiResponse<List<MenuButtonDto>>.Ok(menuButtons));
    }

    /// <summary>
    ///     更新菜单按钮
    /// </summary>
    [HttpPut]
    [Authorize(Policy = PermissionCodes.System.MenuButtons.Update)]
    public async Task<ActionResult<ApiResponse<MenuButtonDto>>> Update([FromQuery] Guid menuId, [FromBody] UpdateMenuButtonDto dto)
    {
        var menuButton = await menuButtonService.UpdateMenuButtonAsync(menuId, dto);
        return Ok(ApiResponse<MenuButtonDto>.Ok(menuButton, "Menu button updated successfully"));
    }

    /// <summary>
    ///     批量替换菜单按钮
    /// </summary>
    [HttpPut("replace")]
    [Authorize(Policy = PermissionCodes.System.MenuButtons.Update)]
    public async Task<ActionResult<ApiResponse<List<MenuButtonDto>>>> Replace([FromQuery] Guid menuId, [FromBody] List<CreateMenuButtonDto> dtos)
    {
        var menuButtons = await menuButtonService.ReplaceMenuButtonsAsync(menuId, dtos);
        return Ok(ApiResponse<List<MenuButtonDto>>.Ok(menuButtons));
    }

    /// <summary>
    ///     删除菜单按钮
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = PermissionCodes.System.MenuButtons.Delete)]
    public async Task<ActionResult<ApiResponse<string>>> Delete(Guid id)
    {
        await menuButtonService.DeleteMenuButtonAsync(id);
        return Ok(ApiResponse<string>.Ok("Menu button deleted successfully"));
    }
}
