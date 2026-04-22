using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shared.Common;

/// <summary>
///     基于 JSON + SHA1 的缓存 Key 生成器。
///     相同参数组合一定得到同一个 Key，不同组合几乎不可能冲突。
/// </summary>
public static class CacheKeyBuilder
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        WriteIndented = false
    };

    /// <summary>
    ///     根据 prefix + 参数列表生成 key。
    /// </summary>
    public static string Build(string prefix, IReadOnlyList<object?> args)
    {
        var payload = SerializeArgs(args);
        var hashBytes = SHA1.HashData(Encoding.UTF8.GetBytes(payload));
        var hash = Convert.ToHexString(hashBytes)[..16];
        return string.IsNullOrWhiteSpace(prefix) ? hash : $"{prefix}:{hash}";
    }

    private static string SerializeArgs(IReadOnlyList<object?> args)
    {
        if (args.Count == 0) return "[]";
        try
        {
            return JsonSerializer.Serialize(args, Options);
        }
        catch
        {
            // 兜底：序列化失败时使用 ToString 拼接（例如含不可序列化类型）
            var sb = new StringBuilder();
            for (var i = 0; i < args.Count; i++)
            {
                if (i > 0) sb.Append('|');
                sb.Append(args[i]?.ToString() ?? "null");
            }

            return sb.ToString();
        }
    }
}
