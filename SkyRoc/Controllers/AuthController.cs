using Application.DTOs.Auth;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;

namespace SkyRoc.Controllers;

/// <summary>
///     认证控制器
/// </summary>
[ApiController]
[Route("api/auth")]
[Authorize]
public class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>
    ///     登录
    /// </summary>
    /// <param name="request">用户名与密码。</param>
    /// <returns>访问令牌与刷新令牌。</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<LoginResDto?>>> Login([FromBody] LoginReqDto request)
    {
        var loginUser = await authService.LoginAsync(request);
        return Ok(ApiResponse<LoginResDto?>.Ok(loginUser));
    }

    /// <summary>
    ///     获取用户信息
    /// </summary>
    /// <returns>当前登录用户的角色、权限与按钮编码。</returns>
    [HttpGet("getUserInfo")]
    public async Task<ActionResult<ApiResponse<UserInfoDto>>> GetUserInfo()
    {
        var userInfo = await authService.GetUserInfoAsync();
        return Ok(ApiResponse<UserInfoDto>.Ok(userInfo));
    }

    /// <summary>
    ///     获取路由信息
    /// </summary>
    /// <returns>前端动态路由树与默认首页路径。</returns>
    [HttpGet("getRoutes")]
    public async Task<ActionResult<ApiResponse<GetRoutesResDto>>> GetRoutes()
    {
        var routes = await authService.GetRoutesAsync();
        return Ok(ApiResponse<GetRoutesResDto>.Ok(new GetRoutesResDto
        {
            Routes = routes,
            Home = "/home"
        }));
    }

    /// <summary>
    ///     刷新令牌
    /// </summary>
    /// <param name="request">刷新令牌。</param>
    /// <returns>新的访问令牌与刷新令牌。</returns>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<LoginResDto?>>> RefreshToken([FromBody] RefreshTokenReqDto request)
    {
        var loginUser = await authService.RefreshTokenAsync(request.RefreshToken);
        return Ok(ApiResponse<LoginResDto?>.Ok(loginUser));
    }

    /// <summary>
    ///     注销登录
    /// </summary>
    /// <param name="request">待失效的刷新令牌。</param>
    /// <returns>是否注销成功。</returns>
    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<bool>>> Logout([FromBody] RefreshTokenReqDto request)
    {
        var result = await authService.LogoutAsync(request.RefreshToken);
        return Ok(ApiResponse<bool>.Ok(result));
    }
}
