using System.Reflection;
using System.Security.Claims;
using Application.DTOs.System;
using Application.Exceptions;
using Application.Services.System;
using Application.Mappers;
using Application.interfaces.System;
using Domain.Entities;
using Domain.Entities.System;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Repositories.System;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using AutoMapper;
using Shared.Constants;
using SkyRoc.Middleware;
using Xunit;

namespace SkyRoc.Tests.SystemSupport;

/// <summary>
/// P4-09 系统支撑功能的公开契约回归测试。
/// </summary>
public class SystemSupportContractTests
{
    [Fact]
    public void SystemSupport_DeclaresRequiredPersistedModelsAndControllers()
    {
        var domainAssembly = typeof(Domain.Entities.OperationLog).Assembly;
        var webAssembly = typeof(Program).Assembly;

        Assert.NotNull(domainAssembly.GetType("Domain.Entities.System.ServicePeriod"));
        Assert.NotNull(domainAssembly.GetType("Domain.Entities.System.SystemSetting"));
        Assert.NotNull(domainAssembly.GetType("Domain.Entities.System.Notice"));
        Assert.NotNull(domainAssembly.GetType("Domain.Entities.System.LoginLog"));
        Assert.NotNull(webAssembly.GetType("SkyRoc.Controllers.SystemSettingsController"));
        Assert.NotNull(webAssembly.GetType("SkyRoc.Controllers.NoticesController"));
        Assert.NotNull(webAssembly.GetType("SkyRoc.Controllers.LogsController"));
        Assert.NotNull(webAssembly.GetType("SkyRoc.Middleware.OperationAuditMiddleware"));
    }

    [Fact]
    public void LoginAuditService_DoesNotDependOnAspNetCoreHttpContext()
    {
        var dependencyTypes = typeof(LoginAuditService).GetConstructors().Single()
            .GetParameters()
            .Select(parameter => parameter.ParameterType)
            .ToList();

        Assert.DoesNotContain(typeof(IHttpContextAccessor), dependencyTypes);
    }

    [Fact]
    public void AuditTextSanitizer_NormalizesRequiredAndOptionalValues()
    {
        var sanitizerType = typeof(LoginAuditService).Assembly
            .GetType("Application.Services.System.AuditTextSanitizer");

        Assert.NotNull(sanitizerType);
        var required = sanitizerType.GetMethod("Required", BindingFlags.Public | BindingFlags.Static);
        var optional = sanitizerType.GetMethod("Optional", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(required);
        Assert.NotNull(optional);
        Assert.Equal("未知", required.Invoke(null, ["   ", 10, "未知"]));
        Assert.Equal("ab", optional.Invoke(null, [" abc ", 2]));
        Assert.Null(optional.Invoke(null, ["   ", 10]));
    }

    [Fact]
    public async Task ServicePeriods_ValidateBoundariesAndPreserveEnabledOrder()
    {
        await using var context = CreateDbContext();
        var service = CreateService(context);
        var created = await service.CreateServicePeriodAsync(new UpsertServicePeriodDto
        {
            Name = "午间配送",
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(12, 0),
            SortOrder = 2,
            IsEnabled = true
        });
        await service.CreateServicePeriodAsync(new UpsertServicePeriodDto
        {
            Name = "停用时段",
            StartTime = new TimeOnly(13, 0),
            EndTime = new TimeOnly(14, 0),
            SortOrder = 1,
            IsEnabled = false
        });

        var periods = await service.GetServicePeriodsAsync(false);

        Assert.Equal(created.Id, Assert.Single(periods).Id);
        await Assert.ThrowsAsync<BusinessException>(() => service.CreateServicePeriodAsync(new UpsertServicePeriodDto
        {
            Name = "跨日",
            StartTime = new TimeOnly(18, 0),
            EndTime = new TimeOnly(9, 0),
            SortOrder = 0
        }));
    }

    [Fact]
    public async Task SettingsAndNotices_ValidateValuesAndPublishWithoutChangingContent()
    {
        await using var context = CreateDbContext();
        var service = CreateService(context);
        var settings = await service.SaveMiniProgramOrderSettingsAsync(new MiniProgramOrderSettingsDto { IsEnabled = true, MaxAdvanceOrderDays = 7 });
        var notice = await service.CreateNoticeAsync(new UpsertNoticeDto { Title = "  配送提醒  ", Content = "  明日配送窗口调整。  " });

        var published = await service.UpdateNoticeStatusAsync(notice.Id, new UpdateNoticeStatusDto { NoticeStatus = NoticeStatus.Published });

        Assert.Equal(7, (await service.GetMiniProgramOrderSettingsAsync()).MaxAdvanceOrderDays);
        Assert.Equal(("配送提醒", "明日配送窗口调整。", NoticeStatus.Published), (published.Title, published.Content, published.NoticeStatus));
        Assert.NotNull(published.PublishedTime);
        await Assert.ThrowsAsync<BusinessException>(() => service.SaveMiniProgramOrderSettingsAsync(new MiniProgramOrderSettingsDto { MaxAdvanceOrderDays = 31 }));
        await Assert.ThrowsAsync<BusinessException>(() => service.CreateNoticeAsync(new UpsertNoticeDto { Title = "风险内容", Content = "<script>alert(1)</script>" }));
    }

    [Fact]
    public async Task AuditLogs_QueryByResultAndKeepSensitiveValuesOutOfLoginRecords()
    {
        await using var context = CreateDbContext();
        var service = CreateService(context);
        var operationAudit = new OperationAuditService(new OperationLogRepository(context), new UnitOfWork(context), new TestCurrentUserService());
        await operationAudit.RecordAsync(new OperationAuditEntry
        {
            Module = "notices",
            OperationType = "Create",
            Description = "POST /api/notices",
            Method = "POST",
            Url = "/api/notices",
            IpAddress = "127.0.0.1",
            IsSuccess = true,
            ExecutionDuration = 12,
            RequestSummary = "title=配送提醒"
        });
        var loginAudit = new LoginAuditService(
            new LoginLogRepository(context),
            new UnitOfWork(context),
            new TestAuditRequestSourceAccessor());
        await loginAudit.RecordAsync("admin", Guid.NewGuid(), false, "用户不存在或密码错误");

        var operations = await service.GetOperationLogsAsync(new Application.QueryParameters.System.OperationLogQueryParameters { IsSuccess = true });
        var logins = await service.GetLoginLogsAsync(new Application.QueryParameters.System.LoginLogQueryParameters { IsSuccess = false });

        Assert.Equal("notices", Assert.Single(operations.Records!).Module);
        var login = Assert.Single(logins.Records!);
        Assert.DoesNotContain("password", login.FailureReason!, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("SkyRoc.Tests", login.UserAgent);
    }

    [Fact]
    public async Task AuditLogs_FilterByKeywordWithoutCaseSensitivity()
    {
        await using var context = CreateDbContext();
        var service = CreateService(context);
        var operationAudit = new OperationAuditService(
            new OperationLogRepository(context),
            new UnitOfWork(context),
            new TestCurrentUserService());
        await operationAudit.RecordAsync(new OperationAuditEntry
        {
            Module = "notices",
            OperationType = "Update",
            Description = "PUT /api/notices/delivery-window",
            Method = "PUT",
            Url = "/api/notices/delivery-window",
            IpAddress = "127.0.0.1",
            IsSuccess = true,
            RequestSummary = "title=Delivery Reminder"
        });
        await operationAudit.RecordAsync(new OperationAuditEntry
        {
            Module = "settings",
            OperationType = "Update",
            Description = "PUT /api/system-settings/sorting-weights",
            Method = "PUT",
            Url = "/api/system-settings/sorting-weights",
            IpAddress = "127.0.0.1",
            IsSuccess = true
        });
        var loginAudit = new LoginAuditService(
            new LoginLogRepository(context),
            new UnitOfWork(context),
            new TestAuditRequestSourceAccessor());
        await loginAudit.RecordAsync("DeliveryOperator", Guid.NewGuid(), false, "用户不存在或密码错误");
        await loginAudit.RecordAsync("WarehouseUser", Guid.NewGuid(), true, null);

        var operations = await service.GetOperationLogsAsync(new Application.QueryParameters.System.OperationLogQueryParameters
        {
            Keyword = "DELIVERY"
        });
        var logins = await service.GetLoginLogsAsync(new Application.QueryParameters.System.LoginLogQueryParameters
        {
            Keyword = "deliveryoperator"
        });

        Assert.Equal("notices", Assert.Single(operations.Records!).Module);
        Assert.Equal("DeliveryOperator", Assert.Single(logins.Records!).Username);
    }

    [Fact]
    public async Task OperationAuditMiddleware_RecordsUnsafeApiRequestAndRedactsSensitiveQueryValues()
    {
        var sink = new CapturingOperationAuditService();
        await using var provider = new ServiceCollection().AddSingleton<IOperationAuditService>(sink).BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = provider };
        context.User = CreateAuthenticatedPrincipal();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/notices";
        context.Request.QueryString = new QueryString("?token=secret&title=配送提醒");
        var middleware = new OperationAuditMiddleware(_ => Task.CompletedTask, NullLogger<OperationAuditMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        var entry = Assert.Single(sink.Entries);
        Assert.Equal(("notices", "Create", true), (entry.Module, entry.OperationType, entry.IsSuccess));
        Assert.Contains("token=***", entry.RequestSummary);
        Assert.DoesNotContain("secret", entry.RequestSummary);
    }

    [Fact]
    public async Task OperationAuditMiddleware_SkipsAnonymousWrites()
    {
        var sink = new CapturingOperationAuditService();
        await using var provider = new ServiceCollection().AddSingleton<IOperationAuditService>(sink).BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = provider };
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/Auth/login";
        var middleware = new OperationAuditMiddleware(_ => Task.CompletedTask, NullLogger<OperationAuditMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Empty(sink.Entries);
    }

    [Theory]
    [InlineData("apiKey")]
    [InlineData("access_key")]
    [InlineData("private-key")]
    public async Task OperationAuditMiddleware_RedactsKeyParametersWithoutRedactingKeyword(string parameterName)
    {
        var sink = new CapturingOperationAuditService();
        await using var provider = new ServiceCollection().AddSingleton<IOperationAuditService>(sink).BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = provider };
        context.User = CreateAuthenticatedPrincipal();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/notices";
        context.Request.QueryString = new QueryString($"?{parameterName}=sensitive&keyword=delivery");
        var middleware = new OperationAuditMiddleware(_ => Task.CompletedTask, NullLogger<OperationAuditMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        var summary = Assert.Single(sink.Entries).RequestSummary;
        Assert.Contains($"{parameterName}=***", summary);
        Assert.Contains("keyword=delivery", summary);
        Assert.DoesNotContain("sensitive", summary);
    }

    [Fact]
    public async Task OperationAuditMiddleware_RecordsFinalHttpStatus_WhenRequestThrows()
    {
        var sink = new CapturingOperationAuditService();
        await using var provider = new ServiceCollection().AddSingleton<IOperationAuditService>(sink).BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = provider };
        context.User = CreateAuthenticatedPrincipal();
        context.Request.Method = HttpMethods.Put;
        context.Request.Path = "/api/notices/11111111-1111-1111-1111-111111111111";
        context.Response.Body = new MemoryStream();
        var auditMiddleware = new OperationAuditMiddleware(
            _ => throw new NotFoundException("通知公告不存在"),
            NullLogger<OperationAuditMiddleware>.Instance);
        var exceptionMiddleware = new ExceptionHandlingMiddleware(
            auditMiddleware.InvokeAsync,
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await exceptionMiddleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        var entry = Assert.Single(sink.Entries);
        Assert.Equal("HTTP 404", entry.ResponseSummary);
        Assert.False(entry.IsSuccess);
    }

    [Fact]
    public void SystemSupportModels_HaveDatabaseCommentsAndConstraints()
    {
        using var context = CreateDbContext();
        var model = context.GetService<IDesignTimeModel>().Model;
        var period = model.FindEntityType(typeof(ServicePeriod))!;
        var notice = model.FindEntityType(typeof(Notice))!;
        var login = model.FindEntityType(typeof(LoginLog))!;

        Assert.Contains("运营服务时段", period.GetComment());
        Assert.Contains("结束", period.FindProperty(nameof(ServicePeriod.EndTime))!.GetComment());
        Assert.Contains(period.GetCheckConstraints(), constraint => constraint.Name == "ck_sys_service_period_time");
        Assert.Contains("通知公告", notice.GetComment());
        Assert.Contains("密码", login.FindProperty(nameof(LoginLog.FailureReason))!.GetComment());
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ApplicationDbContext(options);
    }

    private static ClaimsPrincipal CreateAuthenticatedPrincipal() =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())], "Test"));

    private static SystemSupportService CreateService(ApplicationDbContext context) => new(
        new ServicePeriodRepository(context), new SystemSettingRepository(context), new NoticeRepository(context),
        new OperationLogRepository(context), new LoginLogRepository(context), new UnitOfWork(context),
        new TestCurrentUserService(),
        new MapperConfiguration(config => config.AddProfile<SystemSupportMappingProfile>()).CreateMapper());

    private sealed class TestCurrentUserService : Application.interfaces.ICurrentUserService
    {
        public Guid? GetUserId() => Guid.Parse("11111111-1111-1111-1111-111111111111");
        public string? GetUserName() => "system-test";
        public string? GetEmail() => null;
        public string? GetRole() => "admin";
        public IReadOnlyList<string> GetRoles() => ["admin"];
        public bool HasClaim(string claimType, string claimValue) => false;
    }

    private sealed class CapturingOperationAuditService : IOperationAuditService
    {
        public List<OperationAuditEntry> Entries { get; } = [];
        public Task RecordAsync(OperationAuditEntry entry) { Entries.Add(entry); return Task.CompletedTask; }
    }

    private sealed class TestAuditRequestSourceAccessor : IAuditRequestSourceAccessor
    {
        public AuditRequestSource GetCurrent() => new()
        {
            IpAddress = "127.0.0.1",
            UserAgent = "SkyRoc.Tests"
        };
    }
}
