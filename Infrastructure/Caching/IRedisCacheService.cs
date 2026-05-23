using Shared.Common;

namespace Infrastructure.Caching;

/// <summary>
///     Redis 主缓存实现标记，用于运行时组合降级。
/// </summary>
public interface IRedisCacheService : ICacheService
{
}
