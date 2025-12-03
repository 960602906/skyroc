using Application.DTOs.Auth;
using Application.interfaces;
using Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SkyRoc.Controllers;

/// <summary>
///     认证控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>
    ///     登录
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginReqDto request)
    {
        // 登录逻辑
        var loginUser = await authService.LoginAsync(request);
        return Ok(ApiResponse<LoginResDto?>.Ok(loginUser));
    }

    /// <summary>
    ///     获取用户信息
    /// </summary>
    /// <returns></returns>
    [HttpGet("getUserInfo")]
    public async Task<IActionResult> GetUserInfo()
    {
        var userInfo = await authService.GetUserInfoAsync();
        return Ok(ApiResponse<UserInfoDto>.Ok(userInfo));
    }

    /// <summary>
    ///     获取路由信息
    /// </summary>
    /// <returns></returns>
    [HttpGet("getRoutes")]
    public async Task<IActionResult> GetRoutes()
    {
        var routes = await authService.GetRoutesAsync();
        return Ok(ApiResponse<object>.Ok(new { routes, Home = "/home" }));
    }

    /// <summary>
    ///     刷新令牌
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenReqDto request)
    {
        var loginUser = await authService.RefreshTokenAsync(request.RefreshToken);
        return Ok(ApiResponse<LoginResDto?>.Ok(loginUser));
    }

    /// <summary>
    ///     注销登录
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenReqDto request)
    {
        var result = await authService.LogoutAsync(request.RefreshToken);
        return Ok(ApiResponse<bool>.Ok(result));
    }
}