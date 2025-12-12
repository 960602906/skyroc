using Application.DTOs.Auth;

namespace Application.interfaces;

public interface ITokenCacheService
{
    Task SaveTokenAsync(string token, TokenCacheDataDto data, TimeSpan? expiration = null);
    Task<TokenCacheDataDto?> GetTokenDataAsync(string token);
    Task<bool> IsTokenValidAsync(string token);
    Task RevokeTokenAsync(string token);
    Task RevokeAllUserTokensAsync(int userId);
    Task<List<TokenCacheDataDto>> GetUserActiveTokensAsync(int userId);
}