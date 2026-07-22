using System.Reflection;
using Application.AI;
using Application.AI.Abstractions;
using Application.AI.Configuration;
using Application.Events;
using Application.Mappers;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shared.Options;

namespace Application;

public static class DependencyInjection
{
    /// <summary>
    ///     自动扫描注册 Application.Services 下的服务
    /// </summary>
    private static IServiceCollection AddAutoServiceConfiguration(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();
        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(classes => classes
                .InNamespaces("Application.Services")
                .Where(type => type.Name.EndsWith("Service") && !type.IsAbstract))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        return services;
    }

    /// <summary>
    ///     注册进程内应用事件发布器与全部 handler。
    /// </summary>
    private static IServiceCollection AddApplicationEvents(this IServiceCollection services)
    {
        services.AddScoped<IApplicationEventPublisher, ApplicationEventPublisher>();
        var assembly = Assembly.GetExecutingAssembly();
        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(classes => classes.AssignableTo(typeof(IApplicationEventHandler<>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());
        return services;
    }

    /// <summary>
    ///     注册 FluentValidation
    /// </summary>
    private static IServiceCollection AddAuthFluentValidationConfiguration(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        return services;
    }

    /// <summary>
    ///     注册 AutoMapper
    /// </summary>
    private static IServiceCollection AddAutoMapperConfiguration(this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(MenuMappingProfile).Assembly);
        return services;
    }

    /// <summary>注册 AI/MCP 安全默认配置、启动校验和活动 Provider 注册表。</summary>
    private static IServiceCollection AddAiConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<AiOptions>, AiOptionsValidator>();
        services.AddOptions<AiOptions>()
            .Bind(configuration.GetSection(AiOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<McpOptions>, McpOptionsValidator>();
        services.AddOptions<McpOptions>()
            .Bind(configuration.GetSection(McpOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IAiModelProviderRegistry, AiModelProviderRegistry>();
        services.AddHostedService<AiConfigurationStartupService>();
        return services;
    }

    /// <summary>
    ///     注册所有应用层服务
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        services.AddAutoServiceConfiguration();
        services.AddApplicationEvents();
        services.AddAutoMapperConfiguration();
        services.AddAuthFluentValidationConfiguration();
        services.AddAiConfiguration(configuration);
        return services;
    }
}
