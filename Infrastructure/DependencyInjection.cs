using System.Reflection;
using Domain.Interfaces;
using Infrastructure.Caching;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Common;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services,
        IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddHttpContextAccessor();
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString) || connectionString.Contains("__SET_IN_ENV__"))
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is not configured. Set it via environment variable 'ConnectionStrings__DefaultConnection'.");
        // 池化 DbContext 降低高并发分配开销；启用瞬断重试与命令超时
        services.AddDbContextPool<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                // 连接瞬断/超时自动重试，避免高并发下偶发抖动直接抛错
                npgsql.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
                // 单条命令超时，防止慢查询长时间占用连接拖垮连接池
                npgsql.CommandTimeout(30);
            }));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        var assembly = Assembly.GetExecutingAssembly();
        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(classes => classes
                .InNamespaces("Infrastructure.Repositories")
                .Where(type => type.Name.EndsWith("Repository") && !type.IsAbstract))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services.AddRedisServices(configuration, environment);
        services.AddObjectStorage(configuration, environment);
        return services;
    }

    /// <summary>
    /// 注册 RustFS 对象存储；测试可设 <c>RustFS:UseInMemory=true</c>。
    /// 非生产环境缺少密钥时回退到进程内存储，生产环境缺少密钥则启动失败。
    /// </summary>
    private static IServiceCollection AddObjectStorage(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var section = configuration.GetSection(RustFsOptions.SectionName);
        services.Configure<RustFsOptions>(section);
        var options = section.Get<RustFsOptions>() ?? new RustFsOptions();
        var missingCredentials = string.IsNullOrWhiteSpace(options.AccessKey)
            || string.IsNullOrWhiteSpace(options.SecretKey);

        if (options.UseInMemory || missingCredentials)
        {
            if (missingCredentials && !options.UseInMemory && environment.IsProduction())
            {
                throw new InvalidOperationException(
                    "RustFS:AccessKey / RustFS:SecretKey is not configured. Set them via environment variables 'RustFS__AccessKey' and 'RustFS__SecretKey'.");
            }

            services.AddSingleton<IObjectStorage, InMemoryObjectStorage>();
            return services;
        }

        services.AddSingleton<IObjectStorage, RustFsObjectStorage>();
        return services;
    }
}
