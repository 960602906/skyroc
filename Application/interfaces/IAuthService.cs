using Application.DTOs.Auth;

namespace Application.interfaces;

public interface IAuthService
{
    /// <summary>
    ///     用户登录
    /// </summary>
    Task<LoginResDto?> LoginAsync(LoginReqDto request);

    /// <summary>
    ///     刷新令牌
    /// </summary>
    Task<LoginResDto?> RefreshTokenAsync(string refreshToken);

    /// <summary>
    ///     注销
    /// </summary>
    Task<bool> LogoutAsync(string refreshToken);

    /// <summary>
    ///     获取用户信息
    /// </summary>
    /// <returns></returns>
    Task<UserInfoDto> GetUserInfoAsync();

    /// <summary>
    ///     获取路由信息
    /// </summary>
    /// <returns></returns>
    Task<List<RoutesDto>> GetRoutesAsync();
}