using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs.Auth;
using Application.DTOs.System;
using Domain.Entities;
using Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
using Shared.Common;
using Shared.Constants;
using Shared.Utils;
using SkyRoc.Tests.Common;
using SkyRoc.Tests.Testing.PostgreSql;
using Xunit;

namespace SkyRoc.Tests.SystemSupport;

/// <summary>
///     T12 切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证运营设置、公告状态机与审计脱敏。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class SystemSupportOpsNoticeAuditPostgreSqlTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     服务时段 CRUD/边界→小程序与分拣权重读写并恢复原值→公告草稿发布撤回与 HTML 拒绝→
    ///     写操作 query 脱敏与登录失败不落密→401/403（日志相邻权限无运营/公告）权限矩阵；
    ///     临时批次数据精确清理。
    /// </summary>
    [Fact]
    public async Task SystemSupport_OpsNoticeAuditDesensitizationAndPermissionMatrix_OnPostgreSql()
    {
        var batch = TestBatchContext.Create();
        var registry = new BatchCleanupRegistry(batch);

        var limitedRoleId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var limitedUserId = Guid.NewGuid();
        var seedMenuId = Guid.NewGuid();
        var seedLogReadButtonId = Guid.NewGuid();

        var adminUsername = $"{batch.Id}A";
        var limitedUsername = $"{batch.Id}L";
        var limitedRoleCode = $"{batch.Id}R";
        var limitedRoleName = $"{batch.Id}N";
        var seedMenuName = $"{batch.Id}S";
        var enabledPeriodName = $"{batch.Id}-午间配送窗口";
        var disabledPeriodName = $"{batch.Id}-停用晚间窗口";
        var updatedPeriodName = $"{batch.Id}-午间配送窗口改";
        var draftNoticeTitle = $"{batch.Id}-草稿配送提醒";
        var publishedNoticeTitle = $"{batch.Id}-已发布配送提醒";
        var noticeContent = "明日配送窗口调整为 10:00-12:00，请提前备货。";
        var updatedNoticeContent = "明日配送窗口调整为 09:30-11:30，请提前备货。";
        var password = "SkyRocT12SysSupport!2026";
        var wrongPassword = "Definitely-Wrong-T12-Password!";
        var secretToken = "super-secret-t12-token-value";
        var userAgent = $"SkyRoc-T12-SysSupport/{batch.Id}";
        var createName = "T12-SysSupport";

        var logsReadPermission = PermissionCodes.System.Logs.Read;
        var operationsReadPermission = PermissionCodes.System.Operations.Read;
        var noticesReadPermission = PermissionCodes.System.Notices.Read;

        Guid adminRoleId;
        SystemSettingSnapshot? originalMiniProgramSetting = null;
        SystemSettingSnapshot? originalSortingWeightSetting = null;
        Guid? enabledPeriodId = null;
        Guid? disabledPeriodId = null;
        Guid? noticeId = null;

        await using (var seedContext = fixture.CreateDbContext())
        {
            var adminRole = await seedContext.Roles.AsNoTracking()
                .SingleOrDefaultAsync(role => role.Code == SeedConstants.AdminRoleCode);
            Assert.NotNull(adminRole);
            adminRoleId = adminRole.Id;

            var managedAuditUser = await seedContext.Users.AsNoTracking()
                .Where(user => user.Username != null
                               && !user.Username.StartsWith(TestBatchContext.Prefix))
                .OrderBy(user => user.Username)
                .Select(user => new { user.Id, user.Username })
                .FirstAsync();
            originalMiniProgramSetting = await CaptureSystemSettingAsync(
                seedContext,
                SystemSettingKey.MiniProgramOrder,
                managedAuditUser.Id,
                managedAuditUser.Username!);
            originalSortingWeightSetting = await CaptureSystemSettingAsync(
                seedContext,
                SystemSettingKey.SortingWeight,
                managedAuditUser.Id,
                managedAuditUser.Username!);

            await seedContext.Roles.AddAsync(new Role
            {
                Id = limitedRoleId,
                Code = limitedRoleCode,
                Name = limitedRoleName,
                Desc = "T12 系统支撑相邻日志只读临时角色，仅本轮自动测试使用",
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.Users.AddRangeAsync(
                new User
                {
                    Id = adminUserId,
                    Username = adminUsername,
                    NickName = "T12系统支撑操作员",
                    Gender = GenderType.Male,
                    Phone = "13900001231",
                    Email = $"{batch.Id}-a@skyroc-autotest.example",
                    PasswordHash = PasswordHasher.Hash(password),
                    Status = Status.Enable,
                    CreateName = createName
                },
                new User
                {
                    Id = limitedUserId,
                    Username = limitedUsername,
                    NickName = "T12日志只读用户",
                    Gender = GenderType.Female,
                    Phone = "13900001232",
                    Email = $"{batch.Id}-l@skyroc-autotest.example",
                    PasswordHash = PasswordHasher.Hash(password),
                    Status = Status.Enable,
                    CreateName = createName
                });

            await seedContext.UserRoles.AddRangeAsync(
                new UserRole
                {
                    UserId = adminUserId,
                    RoleId = adminRoleId
                },
                new UserRole
                {
                    UserId = limitedUserId,
                    RoleId = limitedRoleId
                });

            await seedContext.Menus.AddAsync(new Menu
            {
                Id = seedMenuId,
                Name = seedMenuName,
                Path = $"/{batch.Id}s",
                Title = "T12日志相邻菜单",
                Component = "page.t12.syssupport.limited",
                MenuType = MenuType.Menu,
                Order = 9631,
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.MenuButtons.AddAsync(new MenuButton
            {
                Id = seedLogReadButtonId,
                Code = logsReadPermission,
                Desc = "T12 审计日志读取相邻权限按钮",
                MenuId = seedMenuId,
                Menu = null!,
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.RoleMenus.AddAsync(new RoleMenu
            {
                RoleId = limitedRoleId,
                MenuId = seedMenuId
            });

            await seedContext.SaveChangesAsync();
        }

        registry.Register<Role>(limitedRoleId, nameof(Role.Code), limitedRoleCode);
        registry.Register<User>(adminUserId, nameof(User.Username), adminUsername);
        registry.Register<User>(limitedUserId, nameof(User.Username), limitedUsername);
        registry.Register<Menu>(seedMenuId, nameof(Menu.Name), seedMenuName);

        try
        {
            using var factory = fixture.CreateWebApplicationFactory();
            using var anonymousClient = factory.CreateClient();
            anonymousClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            using (var anonymousPeriods = await anonymousClient.GetAsync("/api/system-settings/service-periods"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousPeriods, ResponseCode.Unauthorized);
            }

            using (var anonymousNotices = await anonymousClient.GetAsync("/api/notices?current=1&size=10"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousNotices, ResponseCode.Unauthorized);
            }

            using (var anonymousLogs = await anonymousClient.GetAsync("/api/logs/operations?current=1&size=10"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousLogs, ResponseCode.Unauthorized);
            }

            using (var badLogin = await anonymousClient.PostAsJsonAsync("/api/auth/login", new LoginReqDto
            {
                Username = limitedUsername,
                Password = wrongPassword
            }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(badLogin, ResponseCode.DatabaseError);
            }

            LoginResDto adminLogin;
            using (var loginResponse = await anonymousClient.PostAsJsonAsync("/api/auth/login", new LoginReqDto
            {
                Username = adminUsername,
                Password = password
            }))
            {
                Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
                adminLogin = await ReadApiDataAsync<LoginResDto>(loginResponse);
                Assert.False(string.IsNullOrWhiteSpace(adminLogin.Token));
            }

            using var adminClient = factory.CreateClient();
            adminClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.BearerScheme, adminLogin.Token);
            adminClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            var baselineMiniProgram = await ReadApiDataAsync<MiniProgramOrderSettingsDto>(
                await adminClient.GetAsync("/api/system-settings/mini-program-order"));
            var baselineSortingWeights = await ReadApiDataAsync<SortingWeightSettingsDto>(
                await adminClient.GetAsync("/api/system-settings/sorting-weights"));

            var enabledPeriod = await ReadApiDataAsync<ServicePeriodDto>(
                await adminClient.PostAsJsonAsync("/api/system-settings/service-periods", new UpsertServicePeriodDto
                {
                    Name = $"  {enabledPeriodName}  ",
                    StartTime = new TimeOnly(10, 0),
                    EndTime = new TimeOnly(12, 0),
                    SortOrder = 2,
                    IsEnabled = true
                }));
            enabledPeriodId = enabledPeriod.Id;
            Assert.Equal(enabledPeriodName, enabledPeriod.Name);
            Assert.Equal(Status.Enable, enabledPeriod.Status);
            registry.Register<ServicePeriod>(enabledPeriod.Id, nameof(ServicePeriod.Name), enabledPeriodName);

            var disabledPeriod = await ReadApiDataAsync<ServicePeriodDto>(
                await adminClient.PostAsJsonAsync("/api/system-settings/service-periods", new UpsertServicePeriodDto
                {
                    Name = disabledPeriodName,
                    StartTime = new TimeOnly(18, 0),
                    EndTime = new TimeOnly(20, 0),
                    SortOrder = 1,
                    IsEnabled = false
                }));
            disabledPeriodId = disabledPeriod.Id;
            Assert.Equal(Status.Disable, disabledPeriod.Status);
            registry.Register<ServicePeriod>(disabledPeriod.Id, nameof(ServicePeriod.Name), disabledPeriodName);

            using (var enabledOnly = await adminClient.GetAsync("/api/system-settings/service-periods"))
            {
                var periods = await ReadApiDataAsync<IReadOnlyList<ServicePeriodDto>>(enabledOnly);
                Assert.Contains(periods, period => period.Id == enabledPeriod.Id);
                Assert.DoesNotContain(periods, period => period.Id == disabledPeriod.Id);
            }

            using (var includeDisabled = await adminClient.GetAsync("/api/system-settings/service-periods?includeDisabled=true"))
            {
                var periods = await ReadApiDataAsync<IReadOnlyList<ServicePeriodDto>>(includeDisabled);
                Assert.Contains(periods, period => period.Id == enabledPeriod.Id);
                Assert.Contains(periods, period => period.Id == disabledPeriod.Id);
            }

            using (var invalidPeriod = await adminClient.PostAsJsonAsync("/api/system-settings/service-periods", new UpsertServicePeriodDto
            {
                Name = $"{batch.Id}-跨日非法",
                StartTime = new TimeOnly(18, 0),
                EndTime = new TimeOnly(9, 0),
                SortOrder = 0,
                IsEnabled = true
            }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(invalidPeriod, ResponseCode.DatabaseError);
            }

            using (var duplicatePeriod = await adminClient.PostAsJsonAsync("/api/system-settings/service-periods", new UpsertServicePeriodDto
            {
                Name = enabledPeriodName,
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(9, 0),
                SortOrder = 9,
                IsEnabled = true
            }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(duplicatePeriod, ResponseCode.DatabaseError);
            }

            var updatedPeriod = await ReadApiDataAsync<ServicePeriodDto>(
                await adminClient.PutAsJsonAsync(
                    $"/api/system-settings/service-periods/{enabledPeriod.Id}",
                    new UpsertServicePeriodDto
                    {
                        Name = updatedPeriodName,
                        StartTime = new TimeOnly(9, 30),
                        EndTime = new TimeOnly(11, 30),
                        SortOrder = 3,
                        IsEnabled = true
                    }));
            Assert.Equal(updatedPeriodName, updatedPeriod.Name);
            Assert.Equal(new TimeOnly(9, 30), updatedPeriod.StartTime);
            // 名称变更后按新归属登记，旧 Name 登记项在清理前已无对应行，需补新登记并允许旧项幂等跳过
            try
            {
                registry.Register<ServicePeriod>(updatedPeriod.Id, nameof(ServicePeriod.Name), updatedPeriodName);
            }
            catch (InvalidOperationException)
            {
                // 同主键已登记时跳过；后续 finally 按 Name 兜底删除
            }

            var savedMini = await ReadApiDataAsync<MiniProgramOrderSettingsDto>(
                await adminClient.PutAsJsonAsync("/api/system-settings/mini-program-order", new MiniProgramOrderSettingsDto
                {
                    IsEnabled = !baselineMiniProgram.IsEnabled,
                    MaxAdvanceOrderDays = 7
                }));
            Assert.Equal(7, savedMini.MaxAdvanceOrderDays);
            Assert.Equal(!baselineMiniProgram.IsEnabled, savedMini.IsEnabled);

            using (var invalidMini = await adminClient.PutAsJsonAsync("/api/system-settings/mini-program-order", new MiniProgramOrderSettingsDto
            {
                IsEnabled = true,
                MaxAdvanceOrderDays = 31
            }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(invalidMini, ResponseCode.DatabaseError);
            }

            var savedSorting = await ReadApiDataAsync<SortingWeightSettingsDto>(
                await adminClient.PutAsJsonAsync("/api/system-settings/sorting-weights", new SortingWeightSettingsDto
                {
                    OrderTimeWeight = 1.2500m,
                    RouteWeight = 2.5000m,
                    CustomerWeight = 0.7500m
                }));
            Assert.Equal(1.2500m, savedSorting.OrderTimeWeight);
            Assert.Equal(2.5000m, savedSorting.RouteWeight);
            Assert.Equal(0.7500m, savedSorting.CustomerWeight);

            using (var invalidSorting = await adminClient.PutAsJsonAsync("/api/system-settings/sorting-weights", new SortingWeightSettingsDto
            {
                OrderTimeWeight = -1m,
                RouteWeight = 1m,
                CustomerWeight = 1m
            }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(invalidSorting, ResponseCode.DatabaseError);
            }

            using (var htmlNotice = await adminClient.PostAsJsonAsync("/api/notices", new UpsertNoticeDto
            {
                Title = $"{batch.Id}-风险公告",
                Content = "<script>alert(1)</script>"
            }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(htmlNotice, ResponseCode.DatabaseError);
            }

            // 写操作携带敏感 query，验证操作审计脱敏
            var createNoticeUri =
                $"/api/notices?token={Uri.EscapeDataString(secretToken)}&keyword=delivery";
            var createdNotice = await ReadApiDataAsync<NoticeDto>(
                await adminClient.PostAsJsonAsync(createNoticeUri, new UpsertNoticeDto
                {
                    Title = $"  {draftNoticeTitle}  ",
                    Content = $"  {noticeContent}  "
                }));
            noticeId = createdNotice.Id;
            Assert.Equal(draftNoticeTitle, createdNotice.Title);
            Assert.Equal(noticeContent, createdNotice.Content);
            Assert.Equal(NoticeStatus.Draft, createdNotice.NoticeStatus);
            Assert.Null(createdNotice.PublishedTime);
            registry.Register<Notice>(createdNotice.Id, nameof(Notice.Title), draftNoticeTitle);

            using (var publishedOnly = await adminClient.GetAsync("/api/notices?current=1&size=50&includeDraft=false"))
            {
                var page = await ReadApiDataAsync<PagedResult<NoticeDto>>(publishedOnly);
                Assert.DoesNotContain(page.Records!, item => item.Id == createdNotice.Id);
            }

            using (var withDraft = await adminClient.GetAsync("/api/notices?current=1&size=50&includeDraft=true"))
            {
                var page = await ReadApiDataAsync<PagedResult<NoticeDto>>(withDraft);
                Assert.Contains(page.Records!, item => item.Id == createdNotice.Id && item.NoticeStatus == NoticeStatus.Draft);
            }

            var renamedNotice = await ReadApiDataAsync<NoticeDto>(
                await adminClient.PutAsJsonAsync($"/api/notices/{createdNotice.Id}", new UpsertNoticeDto
                {
                    Title = publishedNoticeTitle,
                    Content = updatedNoticeContent
                }));
            Assert.Equal(publishedNoticeTitle, renamedNotice.Title);
            Assert.Equal(updatedNoticeContent, renamedNotice.Content);
            Assert.Equal(NoticeStatus.Draft, renamedNotice.NoticeStatus);
            try
            {
                registry.Register<Notice>(renamedNotice.Id, nameof(Notice.Title), publishedNoticeTitle);
            }
            catch (InvalidOperationException)
            {
                // 同主键已登记
            }

            var published = await ReadApiDataAsync<NoticeDto>(
                await adminClient.PatchAsJsonAsync(
                    $"/api/notices/{createdNotice.Id}/status",
                    new UpdateNoticeStatusDto { NoticeStatus = NoticeStatus.Published }));
            Assert.Equal(NoticeStatus.Published, published.NoticeStatus);
            Assert.NotNull(published.PublishedTime);

            using (var publishedOnlyAfter = await adminClient.GetAsync("/api/notices?current=1&size=50&includeDraft=false"))
            {
                var page = await ReadApiDataAsync<PagedResult<NoticeDto>>(publishedOnlyAfter);
                Assert.Contains(page.Records!, item => item.Id == createdNotice.Id && item.NoticeStatus == NoticeStatus.Published);
            }

            var withdrawn = await ReadApiDataAsync<NoticeDto>(
                await adminClient.PatchAsJsonAsync(
                    $"/api/notices/{createdNotice.Id}/status",
                    new UpdateNoticeStatusDto { NoticeStatus = NoticeStatus.Draft }));
            Assert.Equal(NoticeStatus.Draft, withdrawn.NoticeStatus);
            Assert.Null(withdrawn.PublishedTime);

            await using (var sideEffectContext = fixture.CreateDbContext())
            {
                var periodEntity = await sideEffectContext.ServicePeriods.AsNoTracking()
                    .SingleAsync(period => period.Id == updatedPeriod.Id);
                Assert.Equal(updatedPeriodName, periodEntity.Name);
                Assert.Equal(new TimeOnly(9, 30), periodEntity.StartTime);
                Assert.Equal(Status.Enable, periodEntity.Status);

                var noticeEntity = await sideEffectContext.Notices.AsNoTracking()
                    .SingleAsync(notice => notice.Id == createdNotice.Id);
                Assert.Equal(publishedNoticeTitle, noticeEntity.Title);
                Assert.Equal(updatedNoticeContent, noticeEntity.Content);
                Assert.Equal(NoticeStatus.Draft, noticeEntity.NoticeStatus);
                Assert.Null(noticeEntity.PublishedTime);

                var miniSetting = await sideEffectContext.SystemSettings.AsNoTracking()
                    .SingleAsync(setting => setting.SettingKey == SystemSettingKey.MiniProgramOrder);
                Assert.Contains("\"MaxAdvanceOrderDays\":7", miniSetting.SettingValue, StringComparison.Ordinal);

                var operationLogs = await sideEffectContext.OperationLogs.AsNoTracking()
                    .Where(log => log.CreateName == adminUsername && log.Url != null && log.Url.Contains("/api/notices"))
                    .OrderByDescending(log => log.CreateTime)
                    .Take(20)
                    .ToListAsync();
                Assert.NotEmpty(operationLogs);
                Assert.Contains(operationLogs, log =>
                    log.RequestParams != null
                    && log.RequestParams.Contains("token=***", StringComparison.Ordinal)
                    && !log.RequestParams.Contains(secretToken, StringComparison.Ordinal)
                    && log.RequestParams.Contains("keyword=delivery", StringComparison.Ordinal));
                Assert.All(operationLogs, log =>
                {
                    AssertDoesNotLeakSecrets(log.RequestParams);
                    Assert.DoesNotContain(password, log.RequestParams ?? string.Empty, StringComparison.Ordinal);
                    Assert.DoesNotContain(secretToken, log.RequestParams ?? string.Empty, StringComparison.Ordinal);
                    Assert.DoesNotContain(adminLogin.Token, log.RequestParams ?? string.Empty, StringComparison.Ordinal);
                });
            }

            using (var opsResponse = await adminClient.GetAsync(
                       $"/api/logs/operations?current=1&size=50&keyword={Uri.EscapeDataString(adminUsername)}"))
            {
                var page = await ReadApiDataAsync<PagedResult<OperationLogDto>>(opsResponse);
                Assert.NotEmpty(page.Records!);
                Assert.All(page.Records!, log =>
                {
                    AssertDoesNotLeakSecrets(log.RequestParams);
                    Assert.DoesNotContain(password, log.RequestParams ?? string.Empty, StringComparison.Ordinal);
                    Assert.DoesNotContain(secretToken, log.RequestParams ?? string.Empty, StringComparison.Ordinal);
                    Assert.DoesNotContain(adminLogin.Token, log.RequestParams ?? string.Empty, StringComparison.Ordinal);
                });
            }

            using (var loginsResponse = await adminClient.GetAsync(
                       $"/api/logs/logins?current=1&size=50&username={Uri.EscapeDataString(limitedUsername)}"))
            {
                var page = await ReadApiDataAsync<PagedResult<LoginLogDto>>(loginsResponse);
                Assert.Contains(page.Records!, log => log.Username == limitedUsername && !log.IsSuccess);
                Assert.All(page.Records!, log =>
                {
                    AssertDoesNotLeakSecrets(log.FailureReason);
                    Assert.DoesNotContain(password, log.FailureReason ?? string.Empty, StringComparison.Ordinal);
                    Assert.DoesNotContain(wrongPassword, log.FailureReason ?? string.Empty, StringComparison.Ordinal);
                });
            }

            using (var deleteNotice = await adminClient.DeleteAsync($"/api/notices/{createdNotice.Id}"))
            {
                Assert.True(await ReadApiDataAsync<bool>(deleteNotice));
            }

            noticeId = null;
            await using (var afterDelete = fixture.CreateDbContext())
            {
                Assert.False(await afterDelete.Notices.AnyAsync(notice => notice.Id == createdNotice.Id));
            }

            using (var deletePeriod = await adminClient.DeleteAsync($"/api/system-settings/service-periods/{disabledPeriod.Id}"))
            {
                Assert.True(await ReadApiDataAsync<bool>(deletePeriod));
            }

            disabledPeriodId = null;

            // 相邻权限：仅 system:log:read，运营/公告应 403，日志可读且继续脱敏
            LoginResDto limitedLogin;
            using (var limitedLoginResponse = await anonymousClient.PostAsJsonAsync("/api/auth/login", new LoginReqDto
            {
                Username = limitedUsername,
                Password = password
            }))
            {
                Assert.Equal(HttpStatusCode.OK, limitedLoginResponse.StatusCode);
                limitedLogin = await ReadApiDataAsync<LoginResDto>(limitedLoginResponse);
            }

            using var limitedClient = factory.CreateClient();
            limitedClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.BearerScheme, limitedLogin.Token);
            limitedClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            using (var limitedInfo = await limitedClient.GetAsync("/api/auth/getUserInfo"))
            {
                var info = await ReadApiDataAsync<UserInfoDto>(limitedInfo);
                Assert.Contains(logsReadPermission, info.Permissions);
                Assert.DoesNotContain(operationsReadPermission, info.Permissions);
                Assert.DoesNotContain(noticesReadPermission, info.Permissions);
                Assert.DoesNotContain(PermissionCodes.All, info.Permissions);
            }

            using (var allowedLogs = await limitedClient.GetAsync("/api/logs/operations?current=1&size=10"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedLogs.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<OperationLogDto>>(allowedLogs);
                Assert.All(page.Records!, log => AssertDoesNotLeakSecrets(log.RequestParams));
            }

            using (var deniedPeriods = await limitedClient.GetAsync("/api/system-settings/service-periods"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedPeriods, ResponseCode.Forbidden);
            }

            using (var deniedMini = await limitedClient.GetAsync("/api/system-settings/mini-program-order"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedMini, ResponseCode.Forbidden);
            }

            using (var deniedNotices = await limitedClient.GetAsync("/api/notices?current=1&size=10"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedNotices, ResponseCode.Forbidden);
            }

            using (var deniedCreate = await limitedClient.PostAsJsonAsync("/api/notices", new UpsertNoticeDto
            {
                Title = $"{batch.Id}-越权公告",
                Content = "无权创建"
            }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedCreate, ResponseCode.Forbidden);
            }

            // 清理剩余启用时段
            using (var deleteEnabled = await adminClient.DeleteAsync($"/api/system-settings/service-periods/{updatedPeriod.Id}"))
            {
                Assert.True(await ReadApiDataAsync<bool>(deleteEnabled));
            }

            enabledPeriodId = null;

            await using (var loginDb = fixture.CreateDbContext())
            {
                var loginLogs = await loginDb.LoginLogs.AsNoTracking()
                    .Where(log => log.Username == adminUsername || log.Username == limitedUsername)
                    .ToListAsync();
                RegisterLoginLogs(registry, loginLogs);
            }

            await RegisterBatchOperationLogsAsync(fixture, registry, adminUsername, limitedUsername);
        }
        finally
        {
            await RestoreSystemSettingsAsync(originalMiniProgramSetting, originalSortingWeightSetting);

            if (noticeId.HasValue)
            {
                await using var noticeCleanup = fixture.CreateDbContext();
                var residualNotice = await noticeCleanup.Notices
                    .FirstOrDefaultAsync(notice => notice.Id == noticeId.Value
                                                   || notice.Title == draftNoticeTitle
                                                   || notice.Title == publishedNoticeTitle);
                if (residualNotice is not null)
                {
                    noticeCleanup.Notices.Remove(residualNotice);
                    await noticeCleanup.SaveChangesAsync();
                }
            }

            if (enabledPeriodId.HasValue || disabledPeriodId.HasValue)
            {
                await using var periodCleanup = fixture.CreateDbContext();
                var residualPeriods = await periodCleanup.ServicePeriods
                    .Where(period =>
                        (enabledPeriodId.HasValue && period.Id == enabledPeriodId.Value)
                        || (disabledPeriodId.HasValue && period.Id == disabledPeriodId.Value)
                        || period.Name == enabledPeriodName
                        || period.Name == disabledPeriodName
                        || period.Name == updatedPeriodName)
                    .ToListAsync();
                if (residualPeriods.Count > 0)
                {
                    periodCleanup.ServicePeriods.RemoveRange(residualPeriods);
                    await periodCleanup.SaveChangesAsync();
                }
            }

            await RegisterResidualLoginLogsAsync(fixture, registry, adminUsername, limitedUsername);
            await RegisterBatchOperationLogsAsync(fixture, registry, adminUsername, limitedUsername);

            await fixture.CleanupBatchAsync(registry);
            await fixture.CleanupBatchAsync(registry);

            await using (var cleanupContext = fixture.CreateDbContext())
            {
                var residualUserRoles = await cleanupContext.UserRoles
                    .Where(relation => relation.UserId == adminUserId || relation.UserId == limitedUserId)
                    .ToListAsync();
                if (residualUserRoles.Count > 0)
                {
                    cleanupContext.UserRoles.RemoveRange(residualUserRoles);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualRoleMenus = await cleanupContext.RoleMenus
                    .Where(relation => relation.RoleId == limitedRoleId || relation.MenuId == seedMenuId)
                    .ToListAsync();
                if (residualRoleMenus.Count > 0)
                {
                    cleanupContext.RoleMenus.RemoveRange(residualRoleMenus);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualButtons = await cleanupContext.MenuButtons
                    .Where(button => button.Id == seedLogReadButtonId
                                     || button.MenuId == seedMenuId
                                     || button.CreateName == createName)
                    .ToListAsync();
                if (residualButtons.Count > 0)
                {
                    cleanupContext.MenuButtons.RemoveRange(residualButtons);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualMenus = await cleanupContext.Menus
                    .Where(menu => menu.Id == seedMenuId || menu.Name == seedMenuName)
                    .ToListAsync();
                if (residualMenus.Count > 0)
                {
                    cleanupContext.Menus.RemoveRange(residualMenus);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualUsers = await cleanupContext.Users
                    .Where(user => user.Id == adminUserId
                                   || user.Id == limitedUserId
                                   || user.Username == adminUsername
                                   || user.Username == limitedUsername)
                    .ToListAsync();
                if (residualUsers.Count > 0)
                {
                    cleanupContext.Users.RemoveRange(residualUsers);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualRoles = await cleanupContext.Roles
                    .Where(role => role.Id == limitedRoleId || role.Code == limitedRoleCode)
                    .ToListAsync();
                if (residualRoles.Count > 0)
                {
                    cleanupContext.Roles.RemoveRange(residualRoles);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualPeriodsByName = await cleanupContext.ServicePeriods
                    .Where(period => period.Name == enabledPeriodName
                                     || period.Name == disabledPeriodName
                                     || period.Name == updatedPeriodName
                                     || (period.Name != null && period.Name.StartsWith(batch.Id)))
                    .ToListAsync();
                if (residualPeriodsByName.Count > 0)
                {
                    cleanupContext.ServicePeriods.RemoveRange(residualPeriodsByName);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualNoticesByTitle = await cleanupContext.Notices
                    .Where(notice => notice.Title == draftNoticeTitle
                                     || notice.Title == publishedNoticeTitle
                                     || (notice.Title != null && notice.Title.StartsWith(batch.Id)))
                    .ToListAsync();
                if (residualNoticesByTitle.Count > 0)
                {
                    cleanupContext.Notices.RemoveRange(residualNoticesByTitle);
                    await cleanupContext.SaveChangesAsync();
                }
            }

            await using var residualContext = fixture.CreateDbContext();
            Assert.False(await residualContext.Users.AnyAsync(user =>
                user.Username == adminUsername
                || user.Username == limitedUsername
                || (user.Username != null && user.Username.StartsWith(batch.Id))));
            Assert.False(await residualContext.Roles.AnyAsync(role =>
                role.Id == limitedRoleId
                || role.Code == limitedRoleCode
                || (role.Code != null && role.Code.StartsWith(batch.Id))));
            Assert.False(await residualContext.Menus.AnyAsync(menu =>
                menu.Name == seedMenuName
                || (menu.Name != null && menu.Name.StartsWith(batch.Id))));
            Assert.False(await residualContext.ServicePeriods.AnyAsync(period =>
                period.Name == enabledPeriodName
                || period.Name == disabledPeriodName
                || period.Name == updatedPeriodName
                || (period.Name != null && period.Name.StartsWith(batch.Id))));
            Assert.False(await residualContext.Notices.AnyAsync(notice =>
                notice.Title == draftNoticeTitle
                || notice.Title == publishedNoticeTitle
                || (notice.Title != null && notice.Title.StartsWith(batch.Id))));
            Assert.False(await residualContext.LoginLogs.AnyAsync(log =>
                log.Username == adminUsername
                || log.Username == limitedUsername
                || (log.Username != null && log.Username.StartsWith(batch.Id))));

            if (originalMiniProgramSetting is not null)
            {
                var mini = await residualContext.SystemSettings.AsNoTracking()
                    .SingleAsync(setting => setting.SettingKey == SystemSettingKey.MiniProgramOrder);
                Assert.Equal(originalMiniProgramSetting.SettingValue, mini.SettingValue);
                Assert.False(
                    mini.UpdateName != null && mini.UpdateName.StartsWith(TestBatchContext.Prefix, StringComparison.Ordinal));
                Assert.False(
                    mini.CreateName != null && mini.CreateName.StartsWith(TestBatchContext.Prefix, StringComparison.Ordinal));
            }

            if (originalSortingWeightSetting is not null)
            {
                var sorting = await residualContext.SystemSettings.AsNoTracking()
                    .SingleAsync(setting => setting.SettingKey == SystemSettingKey.SortingWeight);
                Assert.Equal(originalSortingWeightSetting.SettingValue, sorting.SettingValue);
                Assert.False(
                    sorting.UpdateName != null
                    && sorting.UpdateName.StartsWith(TestBatchContext.Prefix, StringComparison.Ordinal));
                Assert.False(
                    sorting.CreateName != null
                    && sorting.CreateName.StartsWith(TestBatchContext.Prefix, StringComparison.Ordinal));
            }
        }
    }

    private async Task RestoreSystemSettingsAsync(
        SystemSettingSnapshot? originalMiniProgramSetting,
        SystemSettingSnapshot? originalSortingWeightSetting)
    {
        if (originalMiniProgramSetting is null && originalSortingWeightSetting is null)
            return;

        await using var context = fixture.CreateDbContext();
        await ApplySystemSettingSnapshotAsync(context, originalMiniProgramSetting);
        await ApplySystemSettingSnapshotAsync(context, originalSortingWeightSetting);
        await context.SaveChangesAsync();
    }

    private static async Task ApplySystemSettingSnapshotAsync(
        Infrastructure.Data.ApplicationDbContext context,
        SystemSettingSnapshot? snapshot)
    {
        if (snapshot is null)
            return;

        var setting = await context.SystemSettings
            .SingleOrDefaultAsync(item => item.SettingKey == snapshot.SettingKey);
        if (setting is null)
            return;

        setting.SettingValue = snapshot.SettingValue;
        setting.CreateBy = snapshot.CreateBy;
        setting.CreateName = snapshot.CreateName;
        setting.UpdateBy = snapshot.UpdateBy;
        setting.UpdateName = snapshot.UpdateName;
        setting.UpdateTime = snapshot.UpdateTime;
    }

    private static async Task<SystemSettingSnapshot> CaptureSystemSettingAsync(
        Infrastructure.Data.ApplicationDbContext context,
        SystemSettingKey settingKey,
        Guid managedAuditUserId,
        string managedAuditUsername)
    {
        var setting = await context.SystemSettings.AsNoTracking()
            .SingleAsync(item => item.SettingKey == settingKey);

        var createName = SanitizeManagedAuditName(setting.CreateName, managedAuditUsername);
        var updateName = SanitizeManagedAuditName(setting.UpdateName, managedAuditUsername);
        var createBy = setting.CreateBy;
        var updateBy = setting.UpdateBy;
        if (setting.CreateName != createName)
            createBy = managedAuditUserId;
        if (setting.UpdateName != updateName)
            updateBy = string.IsNullOrWhiteSpace(updateName) ? null : managedAuditUserId;

        return new SystemSettingSnapshot(
            setting.SettingKey,
            setting.SettingValue,
            createBy,
            createName,
            updateBy,
            updateName,
            setting.UpdateTime);
    }

    private static string? SanitizeManagedAuditName(string? name, string managedAuditUsername)
    {
        if (string.IsNullOrWhiteSpace(name))
            return name;
        return name.StartsWith(TestBatchContext.Prefix, StringComparison.Ordinal)
            ? managedAuditUsername
            : name;
    }

    private sealed record SystemSettingSnapshot(
        SystemSettingKey SettingKey,
        string SettingValue,
        Guid? CreateBy,
        string? CreateName,
        Guid? UpdateBy,
        string? UpdateName,
        DateTime? UpdateTime);

    private static void AssertDoesNotLeakSecrets(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return;

        Assert.DoesNotContain("Bearer ", value, StringComparison.Ordinal);
        Assert.DoesNotContain("eyJ", value, StringComparison.Ordinal);
    }

    private static void RegisterLoginLogs(BatchCleanupRegistry registry, IEnumerable<LoginLog> loginLogs)
    {
        foreach (var log in loginLogs)
        {
            try
            {
                registry.Register<LoginLog>(log.Id, nameof(LoginLog.Username), log.Username!);
            }
            catch (InvalidOperationException)
            {
                // 已登记则跳过
            }
        }
    }

    private static async Task RegisterResidualLoginLogsAsync(
        PostgreSqlTestFixture fixture,
        BatchCleanupRegistry registry,
        params string[] usernames)
    {
        await using var context = fixture.CreateDbContext();
        var nameSet = usernames.Where(name => !string.IsNullOrWhiteSpace(name)).ToArray();
        if (nameSet.Length == 0)
            return;

        var residualLogs = await context.LoginLogs.AsNoTracking()
            .Where(log => log.Username != null && nameSet.Contains(log.Username))
            .ToListAsync();
        RegisterLoginLogs(registry, residualLogs);
    }

    private static async Task RegisterBatchOperationLogsAsync(
        PostgreSqlTestFixture fixture,
        BatchCleanupRegistry registry,
        params string[] usernames)
    {
        await using var context = fixture.CreateDbContext();
        var nameSet = usernames.Where(name => !string.IsNullOrWhiteSpace(name)).ToArray();
        if (nameSet.Length == 0)
            return;

        var operationLogs = await context.OperationLogs.AsNoTracking()
            .Where(log => log.CreateName != null && nameSet.Contains(log.CreateName))
            .ToListAsync();
        foreach (var log in operationLogs)
        {
            if (string.IsNullOrWhiteSpace(log.CreateName))
                continue;
            try
            {
                registry.Register<OperationLog>(log.Id, nameof(OperationLog.CreateName), log.CreateName);
            }
            catch (InvalidOperationException)
            {
                // 已登记则跳过
            }
        }
    }

    private static async Task<T> ReadApiDataAsync<T>(HttpResponseMessage response)
    {
        var payload = await ReadApiResponseAsync<T>(response);
        Assert.Equal(ResponseCode.Success, payload.Code);
        Assert.NotNull(payload.Data);
        return payload.Data!;
    }

    private static async Task<ApiResponse<T>> ReadApiResponseAsync<T>(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        var payload = await JsonSerializer.DeserializeAsync<ApiResponse<T>>(stream, JsonOptions);
        Assert.NotNull(payload);
        return payload!;
    }
}
