namespace SkyRoc.Tests.Testing.PostgreSql;

/// <summary>
///     标识一次跨连接测试运行，使临时业务记录可被精确归属和清理。
/// </summary>
public sealed class TestBatchContext
{
    /// <summary>
    ///     所有自动测试临时业务编码共用的固定前缀。
    /// </summary>
    public const string Prefix = "SKYROC-AUTOTEST-";

    private TestBatchContext(string id)
    {
        Id = id;
    }

    /// <summary>
    ///     本轮唯一批次标识。
    /// </summary>
    public string Id { get; }

    /// <summary>
    ///     创建包含 UTC 时间和随机后缀的本轮批次标识。
    /// </summary>
    public static TestBatchContext Create()
    {
        var id = $"{Prefix}{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..40].ToUpperInvariant();
        return new TestBatchContext(id);
    }
}
