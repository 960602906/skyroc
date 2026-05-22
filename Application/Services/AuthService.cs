using Application.DTOs.Auth;
using Application.Exceptions;
using Application.interfaces;
using AutoMapper;
using Domain.Interfaces;
using Microsoft.Extensions.Options;
using Shared.Common;
using Shared.Utils;

namespace Application.Services;

public class AuthService(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IMenuRepository menuRepository,
    ITokenCacheService tokenCache,
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
        var roleList = roles.ToList();
        var roleCodes = roleList.Select(r => r.Code).ToList();
        var currentRoleId = roleList.FirstOrDefault()?.Id.ToString();

        var access = jwtService.GenerateAccessToken(user, roleCodes, currentRoleId);
        var refreshToken = jwtService.GenerateRefreshToken();
        var refreshExpire = TimeSpan.FromDays(_jwtSettings.RefreshTokenExpirationDays);

        await tokenCache.SaveAccessTokenAsync(new AccessTokenCacheDto
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            Roles = roleCodes.ToArray(),
            Jti = access.Jti,
            LoginTime = DateTime.UtcNow,
            ExpiresAt = access.ExpiresAt
        }, TimeSpan.FromMinutes(_jwtSettings.ExpirationMinutes));

        await tokenCache.SaveRefreshTokenAsync(new RefreshTokenCacheDto
        {
            UserId = user.Id,
            Token = refreshToken,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(refreshExpire)
        }, refreshExpire);

        return new LoginResDto
        {
            Token = access.Token,
            RefreshToken = refreshToken,
            TokenType = "Bearer",
            ExpiresIn = _jwtSettings.ExpirationMinutes * 60
        };
    }

    /// <summary>
    ///     刷新令牌
    /// </summary>
    public async Task<LoginResDto?> RefreshTokenAsync(string refreshToken)
    {
        var stored = await tokenCache.GetRefreshTokenAsync(refreshToken);
        if (stored is null || stored.ExpiresAt <= DateTime.UtcNow) return null;

        var user = await userRepository.GetByIdAsync(stored.UserId);
        if (user is null) return null;

        var roles = await roleRepository.GetRolesByUserIdAsync(user.Id);
        var roleList = roles.ToList();
        var roleCodes = roleList.Select(r => r.Code).ToList();
        var currentRoleId = roleList.FirstOrDefault()?.Id.ToString();

        var access = jwtService.GenerateAccessToken(user, roleCodes, currentRoleId);
        var newRefresh = jwtService.GenerateRefreshToken();
        var refreshExpire = TimeSpan.FromDays(_jwtSettings.RefreshTokenExpirationDays);

        await tokenCache.RevokeRefreshTokenAsync(refreshToken);

        await tokenCache.SaveAccessTokenAsync(new AccessTokenCacheDto
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            Roles = roleCodes.ToArray(),
            Jti = access.Jti,
            LoginTime = DateTime.UtcNow,
            ExpiresAt = access.ExpiresAt
        }, TimeSpan.FromMinutes(_jwtSettings.ExpirationMinutes));

        await tokenCache.SaveRefreshTokenAsync(new RefreshTokenCacheDto
        {
            UserId = user.Id,
            Token = newRefresh,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(refreshExpire)
        }, refreshExpire);

        return new LoginResDto
        {
            Token = access.Token,
            RefreshToken = newRefresh,
            TokenType = "Bearer",
            ExpiresIn = _jwtSettings.ExpirationMinutes * 60
        };
    }

    /// <summary>
    ///     注销
    /// </summary>
    public async Task<bool> LogoutAsync(string refreshToken)
    {
        var stored = await tokenCache.GetRefreshTokenAsync(refreshToken);
        if (stored is null) return false;

        await tokenCache.RevokeRefreshTokenAsync(refreshToken);
        await tokenCache.RevokeAllByUserAsync(stored.UserId);
        return true;
    }

    /// <summary>
    ///     获取用户信息
    /// </summary>
    public async Task<UserInfoDto> GetUserInfoAsync()
    {
        var userId = currentUserService.GetUserId();
        if (userId is null) throw new BusinessException("用户未登录");
        var user = await userRepository.GetByIdAsync(userId.Value);
        if (user is null) throw new NotFoundException("用户不存在");
        var userInfo = mapper.Map<UserInfoDto>(user);
        userInfo.Roles = currentUserService.GetRoles().ToList();
        return userInfo;
    }

    /// <summary>
    ///     获取路由信息
    /// </summary>
    public async Task<List<RoutesDto>> GetRoutesAsync()
    {
        var roleId = currentUserService.GetRole();
        if (roleId is null) throw new NotFoundException("用户角色不存在");
        var menus = await menuRepository.GetMenusByRoleIdAsync(Guid.Parse(roleId));
        return mapper.Map<List<RoutesDto>>(menus);
    }
}
