using System.Reflection;
using Application.Mappers;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    /// <summary>
    ///     注册服务
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    private static IServiceCollection AddAutoServiceConfiguration(this IServiceCollection services)
    {
        // 自动注册服务
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
    ///     注册 FluentValidation
    /// </summary>
    public static IServiceCollection AddAuthFluentValidationConfiguration(this IServiceCollection services)
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

    /// <summary>
    ///     注册所有基础设施服务
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // 注册应用层服务
        services.AddAutoServiceConfiguration();
        // 注册 AutoMapper
        services.AddAutoMapperConfiguration();
        // 注册FluentValidation验证器
        services.AddAuthFluentValidationConfiguration();
        return services;
    }
}