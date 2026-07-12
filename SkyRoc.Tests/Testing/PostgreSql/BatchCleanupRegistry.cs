namespace SkyRoc.Tests.Testing.PostgreSql;

/// <summary>
///     按创建顺序登记本轮临时实体，并提供外键逆序清理计划。
/// </summary>
public sealed class BatchCleanupRegistry(TestBatchContext batch)
{
    private readonly List<BatchCleanupEntry> _entries = [];
    private readonly HashSet<(Type EntityType, Guid EntityId)> _registeredEntities = [];

    /// <summary>
    ///     当前清理登记所属的唯一批次。
    /// </summary>
    public TestBatchContext Batch { get; } = batch ?? throw new ArgumentNullException(nameof(batch));

    /// <summary>
    ///     登记一条由本轮创建的临时实体。
    /// </summary>
    public void Register<TEntity>(Guid entityId, string ownershipPropertyName, string ownershipValue)
        where TEntity : class
    {
        if (entityId == Guid.Empty)
            throw new ArgumentException("A cleanup registration requires a non-empty entity id.", nameof(entityId));
        if (string.IsNullOrWhiteSpace(ownershipPropertyName))
            throw new ArgumentException("A cleanup registration requires an ownership property.", nameof(ownershipPropertyName));
        if (string.IsNullOrWhiteSpace(ownershipValue)
            || !ownershipValue.StartsWith(Batch.Id, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Cleanup ownership must start with the current batch id '{Batch.Id}'.");
        }

        var key = (typeof(TEntity), entityId);
        if (!_registeredEntities.Add(key))
            throw new InvalidOperationException($"Entity '{typeof(TEntity).Name}/{entityId}' is already registered.");

        _entries.Add(new BatchCleanupEntry(
            typeof(TEntity),
            entityId,
            ownershipPropertyName,
            ownershipValue));
    }

    /// <summary>
    ///     按登记逆序返回清理项，使子记录先于父记录删除。
    /// </summary>
    public IReadOnlyList<BatchCleanupEntry> GetCleanupOrder()
    {
        return _entries.AsEnumerable().Reverse().ToArray();
    }
}
