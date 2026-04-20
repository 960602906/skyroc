using System.Text.Json;
using Application.DTOs.Auth;
using Application.interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Application.Services;

public class TokenCacheService(
        IConnectionMultiplexer redis,
        IConfiguration configuration,
        ILogger<TokenCacheService> logger
    ): ITokenCacheService
{
    // Redis Key前缀
    private const string TokenPrefix = "token:";
    private const string UserTokensPrefix = "user:tokens:";
    private readonly IDatabase _db =  redis.GetDatabase();
    /// <summary>
    /// 保存Token到Redis
    /// </summary>
    /// <param name="token"></param>
    /// <param name="data"></param>
    /// <param name="expiration"></param>
    public async Task SaveTokenAsync(string token, TokenCacheDataDto data, TimeSpan? expiration = null)
    {
        try
        {
            var exp = expiration ?? TimeSpan.FromMinutes(
                int.Parse(configuration["Redis:TokenExpireMinutes"]!));
            var tokenKey = $"{TokenPrefix}{token}";
            var userTokensKey = $"{UserTokensPrefix}{data.UserId}";
            // 序列化数据
            var jsonData = JsonSerializer.Serialize(data);
            // 1. 保存Token数据（主要存储）
            await _db.StringSetAsync(tokenKey, jsonData, exp);
            // 2. 添加到用户Token集合（用于管理用户的所有Token）
            await _db.SetAddAsync(userTokensKey, token);
            await _db.KeyExpireAsync(userTokensKey, exp);
            // 3. 可选：保存Token到有序集合，方便按时间查询
            var score = data.LoginTime.ToUniversalTime().Ticks;
            await _db.SortedSetAddAsync($"{userTokensKey}:sorted", token, score);
            await _db.KeyExpireAsync($"{userTokensKey}:sorted", exp);
            logger.LogInformation(
                "Token saved for user {UserId}, JTI: {Jti}", 
                data.UserId, data.TokenJti);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save token for user {UserId}", data.UserId);
            throw;
        }
    }
    
    /// <summary>
    /// 获取Token数据
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public async Task<TokenCacheDataDto?> GetTokenDataAsync(string token)
    {
        try
        {
            var tokenKey = $"{TokenPrefix}{token}";
            var jsonData = await _db.StringGetAsync(tokenKey);
            if (jsonData.IsNullOrEmpty)
            {
                logger.LogWarning("Token not found or expired");
                return null;
            }
            return JsonSerializer.Deserialize<TokenCacheDataDto>(jsonData!);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get token data");
            return null;
        }
    }
    
    /// <summary>
    /// 验证Token是否有效（是否在Redis中存在）
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public async Task<bool> IsTokenValidAsync(string token)
    {
        try
        {
            var tokenKey = $"{TokenPrefix}{token}";
            return await _db.KeyExistsAsync(tokenKey);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to validate token");
            return false;
        }
    }
    
    /// <summary>
    ///  撤销单个Token（登出）
    /// </summary>
    /// <param name="token"></param>
    public async Task RevokeTokenAsync(string token)
    {
        try
        {
            var tokenData = await GetTokenDataAsync(token);
            if (tokenData == null) return;
            var tokenKey = $"{TokenPrefix}{token}";
            var userTokensKey = $"{UserTokensPrefix}{tokenData.UserId}";
            // 删除Token
            await _db.KeyDeleteAsync(tokenKey);
            
            // 从用户Token集合中移除
            await _db.SetRemoveAsync(userTokensKey, token);
            await _db.SortedSetRemoveAsync($"{userTokensKey}:sorted", token);
            logger.LogInformation(
                "Token revoked for user {UserId}, JTI: {Jti}", 
                tokenData.UserId, tokenData.TokenJti);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to revoke token");
            throw;
        }
    }
    /// <summary>
    /// 撤销用户的所有Token（强制登出所有设备）
    /// </summary>
    public async Task RevokeAllUserTokensAsync(int userId)
    {
        try
        {
            var userTokensKey = $"{UserTokensPrefix}{userId}";
            
            // 获取用户所有Token
            var tokens = await _db.SetMembersAsync(userTokensKey);
            // 批量删除Token
            var tasks = tokens.Select(token => 
                _db.KeyDeleteAsync($"{TokenPrefix}{token}")).ToArray();
            
            await Task.WhenAll(tasks);
            // 删除用户Token集合
            await _db.KeyDeleteAsync(userTokensKey);
            await _db.KeyDeleteAsync($"{userTokensKey}:sorted");
            logger.LogInformation(
                "All tokens revoked for user {UserId}, count: {Count}", 
                userId, tokens.Length);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to revoke all tokens for user {UserId}", userId);
            throw;
        }
    }
    /// <summary>
    /// 获取用户当前活跃的所有Token
    /// </summary>
    public async Task<List<TokenCacheDataDto>> GetUserActiveTokensAsync(int userId)
    {
        try
        {
            var userTokensKey = $"{UserTokensPrefix}{userId}";
            var tokens = await _db.SetMembersAsync(userTokensKey);
            var activeTokens = new List<TokenCacheDataDto>();
            foreach (var token in tokens)
            {
                var data = await GetTokenDataAsync(token!);
                if (data != null)
                {
                    activeTokens.Add(data);
                }
            }
            return activeTokens.OrderByDescending(t => t.LoginTime).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get active tokens for user {UserId}", userId);
            return [];
        }
    }
}