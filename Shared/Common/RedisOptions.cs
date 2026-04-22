namespace Shared.Common;

/// <summary>
///     Redis 配置
/// </summary>
public class RedisOptions
{
    public const string SectionName = "Redis";

    /// <summary>
    ///     Redis 连接串，如 "localhost:6379,abortConnect=false,ssl=false"
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    ///     Key 前缀（用于 IDistributedCache 与 ICacheService）
    /// </summary>
    public string InstanceName { get; set; } = "SkyRoc:";

    /// <summary>
    ///     默认数据库
    /// </summary>
    public int Database { get; set; } = 0;

    /// <summary>
    ///     是否启用 Redis。禁用时自动降级为内存缓存
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    ///     访问令牌过期分钟数（仅用于 Redis 缓存 TTL 默认值）
    /// </summary>
    public int AccessTokenExpireMinutes { get; set; } = 60;

    /// <summary>
    ///     刷新令牌过期天数（仅用于 Redis 缓存 TTL 默认值）
    /// </summary>
    public int RefreshTokenExpireDays { get; set; } = 7;
}
