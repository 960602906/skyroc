using Application.DTOs.User;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;

namespace SkyRoc.Controllers;

/// <summary>
///     当前用户个人中心
/// </summary>
[ApiController]
[Route("api/system/user/profile")]
[Authorize]
public class ProfileController(IUserService userService) : ControllerBase
{
    /// <summary>
    ///     获取当前用户个人资料
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<ProfileDto>>> Get()
    {
        var profile = await userService.GetCurrentProfileAsync();
        return Ok(ApiResponse<ProfileDto>.Ok(profile));
    }

    /// <summary>
    ///     更新当前用户个人资料
    /// </summary>
    [HttpPut]
    public async Task<ActionResult<ApiResponse<string>>> Update([FromBody] UpdateProfileDto request)
    {
        await userService.UpdateCurrentProfileAsync(request);
        return Ok(ApiResponse<string>.Ok("个人资料更新成功"));
    }

    /// <summary>
    ///     修改当前用户密码
    /// </summary>
    [HttpPut("updatePwd")]
    public async Task<ActionResult<ApiResponse<string>>> ChangePassword([FromBody] ChangePasswordDto request)
    {
        await userService.ChangeCurrentPasswordAsync(request);
        return Ok(ApiResponse<string>.Ok("密码修改成功"));
    }
}
