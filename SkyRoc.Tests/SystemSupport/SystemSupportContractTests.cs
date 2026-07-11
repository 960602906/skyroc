using System.Reflection;
using Application.DTOs.System;
using Application.Exceptions;
using Application.Services.System;
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
    public async Task ServicePeriods_ValidateBoundariesAndPreserveEnabledOrder()
    {
        await using var context = CreateDbContext();
        var service = CreateService(context);
        var created = await service.CreateServicePeriodAsync(new UpsertServicePeriodDto
        {
            Name = "午间配送", StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(12, 0), SortOrder = 2, IsEnabled = true
        });
        await service.CreateServicePeriodAsync(new UpsertServicePeriodDto
        {
            Name = "停用时段", StartTime = new TimeOnly(13, 0), EndTime = new TimeOnly(14, 0), SortOrder = 1, IsEnabled = false
        });

        var periods = await service.GetServicePeriodsAsync(false);

        Assert.Equal(created.Id, Assert.Single(periods).Id);
        await Assert.ThrowsAsync<BusinessException>(() => service.CreateServicePeriodAsync(new UpsertServicePeriodDto
        {
            Name = "跨日", StartTime = new TimeOnly(18, 0), EndTime = new TimeOnly(9, 0), SortOrder = 0
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
            Module = "notices", OperationType = "Create", Description = "POST /api/notices", Method = "POST", Url = "/api/notices",
            IpAddress = "127.0.0.1", IsSuccess = true, ExecutionDuration = 12, RequestSummary = "title=配送提醒"
        });
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        accessor.HttpContext.Request.Headers["User-Agent"] = "SkyRoc.Tests";
        var loginAudit = new LoginAuditService(new LoginLogRepository(context), new UnitOfWork(context), accessor);
        await loginAudit.RecordAsync("admin", Guid.NewGuid(), false, "用户不存在或密码错误");

        var operations = await service.GetOperationLogsAsync(new Application.QueryParameters.System.OperationLogQueryParameters { IsSuccess = true });
        var logins = await service.GetLoginLogsAsync(new Application.QueryParameters.System.LoginLogQueryParameters { IsSuccess = false });

        Assert.Equal("notices", Assert.Single(operations.Records!).Module);
        var login = Assert.Single(logins.Records!);
        Assert.DoesNotContain("password", login.FailureReason!, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("SkyRoc.Tests", login.UserAgent);
    }

    [Fact]
    public async Task OperationAuditMiddleware_RecordsUnsafeApiRequestAndRedactsSensitiveQueryValues()
    {
        var sink = new CapturingOperationAuditService();
        await using var provider = new ServiceCollection().AddSingleton<IOperationAuditService>(sink).BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = provider };
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

    private static SystemSupportService CreateService(ApplicationDbContext context) => new(
        new ServicePeriodRepository(context), new SystemSettingRepository(context), new NoticeRepository(context),
        new OperationLogRepository(context), new LoginLogRepository(context), new UnitOfWork(context), new TestCurrentUserService());

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
}
