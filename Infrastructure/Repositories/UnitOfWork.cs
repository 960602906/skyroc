using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Repositories;

public class UnitOfWork(ApplicationDbContext dbContext) : IUnitOfWork
{
    private readonly ApplicationDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    private bool _disposed;
    private IDbContextTransaction? _transaction;

    /// <summary>
    ///     获取当前是否在事务中
    /// </summary>
    public bool HasActiveTransaction => _transaction != null;

    /// <summary>
    ///     保存所有更改到数据库
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("数据库更新异常，请检查数据完整性和约束条件", ex);
        }
    }

    /// <summary>
    ///     开始事务
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
            throw new InvalidOperationException("已存在活动事务，请先提交或回滚当前事务");
        _transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    /// <summary>
    ///     提交事务
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
            throw new InvalidCastException("当前没有活动事务可提交");
        try
        {
            await SaveChangesAsync(cancellationToken);
            await _transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }

    /// <summary>
    ///     回滚事务
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
            throw new InvalidCastException("当前没有活动事务可提交");
        try
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }

    /// <summary>
    ///     执行原始 SQL 语句
    /// </summary>
    public async Task<int> ExecuteSqlAsync(string sql, params object[] parameters)
    {
        return await _dbContext.Database.ExecuteSqlRawAsync(sql, parameters);
    }

    /// <summary>
    ///     清空所有追踪的更改
    /// </summary>
    public void ClearChangeTracking()
    {
        _dbContext.ChangeTracker.Clear();
    }

    /// <summary>
    ///     释放资源
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     异步释放资源
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        Dispose(false);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     受保护的 Dispose 方法
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _transaction?.Dispose();
            _dbContext?.Dispose();
        }

        _disposed = true;
    }

    /// <summary>
    ///     异步释放核心资源
    /// </summary>
    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (_disposed)
            return;

        if (_transaction is not null)
            await _transaction.DisposeAsync();

        await _dbContext.DisposeAsync();

        _disposed = true;
    }
}