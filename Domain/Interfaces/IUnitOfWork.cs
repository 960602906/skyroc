namespace Domain.Interfaces;

/// <summary>
///     工作单元接口 - 用于事务处理和多个仓储的协调
/// </summary>
public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    /// <summary>
    ///     获取当前是否在事务中
    /// </summary>
    bool HasActiveTransaction { get; }

    /// <summary>
    ///     保存所有更改到数据库
    /// </summary>
    /// <returns>受影响的行数</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     开始事务
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     提交事务
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     回滚事务
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     执行 SQL 语句
    /// </summary>
    Task<int> ExecuteSqlAsync(string sql, params object[] parameters);

    /// <summary>
    ///     清空所有追踪的更改
    /// </summary>
    void ClearChangeTracking();
}