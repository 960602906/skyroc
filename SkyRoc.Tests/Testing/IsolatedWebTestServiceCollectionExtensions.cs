using Application.DTOs.System;
using Application.interfaces.System;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SkyRoc.Tests.Testing;

/// <summary>
///     为不使用真实 PostgreSQL 的 HTTP 测试宿主统一替换持久化和审计写入依赖。
/// </summary>
internal static class IsolatedWebTestServiceCollectionExtensions
{
    /// <summary>
    ///     配置命名的 InMemory 上下文，并阻断登录与操作审计向任何外部数据库写入。
    /// </summary>
    /// <param name="services">待替换应用依赖的测试服务集合。</param>
    /// <param name="databaseName">当前测试宿主独占的 InMemory 数据库名称。</param>
    internal static void UseIsolatedInMemoryPersistence(this IServiceCollection services, string databaseName)
    {
        ArgumentNullException.ThrowIfNull(services);
        if (string.IsNullOrWhiteSpace(databaseName))
            throw new ArgumentException("The in-memory database name is required.", nameof(databaseName));

        // 生产使用 AddDbContextPool；测试改 InMemory 前必须卸掉池相关单例，否则会与 scoped Options 冲突
        RemoveDbContextPoolRegistrations(services);
        services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
        services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
        services.RemoveAll<ApplicationDbContext>();
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.RemoveAll<IOperationAuditService>();
        services.RemoveAll<ILoginAuditService>();
        services.AddSingleton<IOperationAuditService, NoOpOperationAuditService>();
        services.AddSingleton<ILoginAuditService, NoOpLoginAuditService>();
    }

    /// <summary>
    ///     移除 AddDbContextPool 注册的池类型（不依赖 EF Internal API，避免 EF1001）。
    /// </summary>
    private static void RemoveDbContextPoolRegistrations(IServiceCollection services)
    {
        var poolDescriptors = services
            .Where(descriptor =>
                descriptor.ServiceType.IsGenericType
                && descriptor.ServiceType.GenericTypeArguments.Length == 1
                && descriptor.ServiceType.GenericTypeArguments[0] == typeof(ApplicationDbContext)
                && (descriptor.ServiceType.Name.StartsWith("IDbContextPool", StringComparison.Ordinal)
                    || descriptor.ServiceType.Name.StartsWith("IScopedDbContextLease", StringComparison.Ordinal)))
            .ToList();

        foreach (var descriptor in poolDescriptors)
            services.Remove(descriptor);
    }

    /// <summary>
    ///     在隔离 HTTP 宿主中吞掉操作审计，避免中间件独立作用域越过测试数据库边界。
    /// </summary>
    private sealed class NoOpOperationAuditService : IOperationAuditService
    {
        /// <inheritdoc />
        public Task RecordAsync(OperationAuditEntry entry) => Task.CompletedTask;
    }

    /// <summary>
    ///     在隔离 HTTP 宿主中吞掉登录审计，避免认证路径生成共享库中的不可归属日志。
    /// </summary>
    private sealed class NoOpLoginAuditService : ILoginAuditService
    {
        /// <inheritdoc />
        public Task RecordAsync(string username, Guid? userId, bool isSuccess, string? failureReason) => Task.CompletedTask;
    }
}
