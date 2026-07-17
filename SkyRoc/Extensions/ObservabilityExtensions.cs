using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;

namespace SkyRoc.Extensions;

/// <summary>
///     Serilog 结构化日志与 OpenTelemetry 追踪注册扩展。
/// </summary>
public static class ObservabilityExtensions
{
    /// <summary>
    ///     配置 Serilog 为宿主日志提供程序，并注册 OpenTelemetry tracing（OTLP 可选）。
    /// </summary>
    public static WebApplicationBuilder AddSkyRocObservability(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "SkyRoc")
            .CreateLogger();

        builder.Host.UseSerilog();

        var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"];
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(
                serviceName: builder.Configuration["OpenTelemetry:ServiceName"] ?? "SkyRoc"))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation();

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
                }
            });

        return builder;
    }

    /// <summary>
    ///     启用 Serilog 请求日志（跳过健康检查噪声）。
    /// </summary>
    public static IApplicationBuilder UseSkyRocObservability(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.UseSerilogRequestLogging(options =>
        {
            options.GetLevel = (httpContext, _, exception) =>
            {
                if (exception is not null)
                    return LogEventLevel.Error;

                var path = httpContext.Request.Path.Value ?? string.Empty;
                if (path.StartsWith("/health", StringComparison.OrdinalIgnoreCase))
                    return LogEventLevel.Verbose;

                return LogEventLevel.Information;
            };
        });
        return app;
    }
}
