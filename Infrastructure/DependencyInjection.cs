using System.Reflection;
using Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        // 在此处注册基础设施层的服务，例如数据库上下文、存储库等
        // 配置DbContext
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        // .EnableSensitiveDataLogging() // 显示具体冲突的 ID
        // .LogTo(Console.WriteLine)); // 将日志输出到控制台
        //注册仓储和工作单元
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        var assembly = Assembly.GetExecutingAssembly();
        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(classes => classes
                .InNamespaces("Infrastructure.Repositories")
                .Where(type => type.Name.EndsWith("Repository") && !type.IsAbstract))
            .AsImplementedInterfaces()
            .WithScopedLifetime());
        return services;
    }
}