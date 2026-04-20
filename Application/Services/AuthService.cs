using Application.DTOs.Auth;
using Application.Exceptions;
using Application.interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.Extensions.Options;
using Shared.Common;
using Shared.Utils;

namespace Application.Services;

public class AuthService(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IMenuRepository menuRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IMapper mapper,
    IJwtService jwtService,
    ICurrentUserService currentUserService,
    IOptions<JwtSettings> jwtSettings)
    : IAuthService
{
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;

    public async Task<LoginResDto?> LoginAsync(LoginReqDto request)
    {
        var user = await userRepository.FindByUsernameAsync(request.Username);
        if (user == null) throw new NotFoundException("用户不存在");

        if (!PasswordHasher.Verify(user.PasswordHash, request.Password)) throw new BusinessException("密码错误");
        var roles = await roleRepository.GetRolesByUserIdAsync(user.Id);
        var roleList = roles.Select(r => r.Id.ToString()).ToList();
        // 生成 JWT 令牌
        var accessToken = jwtService.GenerateAccessToken(user, roleList);
        var refreshToken = jwtService.GenerateRefreshToken();
        //保存刷新令牌到数据库
        var userRefreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays)
        };
        await refreshTokenRepository.AddAsync(userRefreshToken);
        return new LoginResDto
        {
            Token = accessToken,
            RefreshToken = refreshToken,
            TokenType = "Bearer",
            ExpiresIn = _jwtSettings.ExpirationMinutes * 60
        };
    }

    /// <summary>
    ///     刷新令牌
    /// </summary>
    /// <param name="refreshToken"></param>
    /// <returns></returns>
    public async Task<LoginResDto?> RefreshTokenAsync(string refreshToken)
    {
        var storedToken = await refreshTokenRepository
            .FirstFindAsync(r => r.Token == refreshToken
                                 && r.ExpiresAt > DateTime.UtcNow
                                 && !r.IsRevoked);
        if (storedToken is null) return null;
        var roles = await roleRepository.GetRolesByUserIdAsync(storedToken.UserId);
        var roleList = roles.Select(r => r.Id.ToString()).ToList();
        // 生成新的令牌
        var newAccessToken = jwtService.GenerateAccessToken(storedToken.User, roleList);
        var newRefreshToken = jwtService.GenerateRefreshToken();
        // 撤销旧的刷新令牌
        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;
        // 保存新的刷新令牌
        var userRefreshToken = new RefreshToken
        {
            UserId = storedToken.UserId,
            Token = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays)
        };
        await refreshTokenRepository.AddAsync(userRefreshToken);
        return new LoginResDto
        {
            Token = newAccessToken,
            RefreshToken = newRefreshToken,
            TokenType = "Bearer",
            ExpiresIn = _jwtSettings.ExpirationMinutes * 60
        };
    }

    /// <summary>
    ///     注销
    /// </summary>
    /// <param name="refreshToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<bool> LogoutAsync(string refreshToken)
    {
        var storedToken = await refreshTokenRepository.FirstFindAsync(r => r.Token == refreshToken);
        if (storedToken is null) return false;
        // 撤销刷新令牌
        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;
        await refreshTokenRepository.UpdateAsync(storedToken);
        return true;
    }

    /// <summary>
    ///     获取用户信息
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<UserInfoDto> GetUserInfoAsync()
    {
        var userId = currentUserService.GetUserId();
        if (userId is null) throw new BusinessException("用户未登录");
        var user = await userRepository.GetByIdAsync(userId.Value);
        return user is null ? throw new NotFoundException("用户不存在") : mapper.Map<UserInfoDto>(user);
    }

    /// <summary>
    ///     获取路由信息
    /// </summary>
    /// <returns></returns>
    public async Task<List<RoutesDto>> GetRoutesAsync()
    {
        var roleId =  currentUserService.GetRole();
        if (roleId is null) throw new NotFoundException("用户角色不存在");
        var menus = await menuRepository.GetMenusByRoleIdAsync(Guid.Parse(roleId));
        return mapper.Map<List<RoutesDto>>(menus);
    }
}