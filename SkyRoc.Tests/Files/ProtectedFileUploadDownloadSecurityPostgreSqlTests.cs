using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs.Auth;
using Application.DTOs.Files;
using Domain.Entities;
using Domain.Entities.Files;
using Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
using Shared.Common;
using Shared.Constants;
using Shared.Utils;
using SkyRoc.Tests.Common;
using SkyRoc.Tests.Testing.PostgreSql;
using Xunit;

namespace SkyRoc.Tests.Files;

/// <summary>
///     T12 切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证受保护文件上传下载、
///     内容签名校验、创建人隔离与 401/403 权限矩阵。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class ProtectedFileUploadDownloadSecurityPostgreSqlTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly byte[] ValidPdfPayload = "%PDF-1.7\nSkyRoc联调检测报告"u8.ToArray();
    private static readonly byte[] ValidPngPayload = [137, 80, 78, 71, 13, 10, 26, 10, 0, 1, 2, 3];
    private static readonly byte[] FakePdfAsPngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>
    ///     有效 PDF/PNG 上传落库与对象存储→本人下载字节一致→跨用户下载 404 隐藏存在性→
    ///     路径文件名/内容伪装拒绝且零写入→401/403（导入导出相邻权限无文件）权限矩阵；
    ///     临时批次数据与对象键精确清理。
    /// </summary>
    [Fact]
    public async Task ProtectedFile_UploadDownloadSecurityAndPermissionMatrix_OnPostgreSql()
    {
        var batch = TestBatchContext.Create();
        var registry = new BatchCleanupRegistry(batch);

        var limitedRoleId = Guid.NewGuid();
        var peerRoleId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var limitedUserId = Guid.NewGuid();
        var peerUserId = Guid.NewGuid();
        var limitedMenuId = Guid.NewGuid();
        var peerMenuId = Guid.NewGuid();
        var limitedImportReadButtonId = Guid.NewGuid();
        var peerFileReadButtonId = Guid.NewGuid();
        var peerFileCreateButtonId = Guid.NewGuid();

        var ownerUsername = $"{batch.Id}O";
        var limitedUsername = $"{batch.Id}L";
        var peerUsername = $"{batch.Id}P";
        var limitedRoleCode = $"{batch.Id}R1";
        var peerRoleCode = $"{batch.Id}R2";
        var limitedRoleName = $"{batch.Id}N1";
        var peerRoleName = $"{batch.Id}N2";
        var limitedMenuName = $"{batch.Id}M1";
        var peerMenuName = $"{batch.Id}M2";
        var pdfFileName = $"{batch.Id}-检测报告.pdf";
        var pngFileName = $"{batch.Id}-货品图.png";
        var peerPdfFileName = $"{batch.Id}-对端报告.pdf";
        var pathTraversalName = $"{batch.Id}/../escape.pdf";
        var spoofedPdfName = $"{batch.Id}-伪装报告.pdf";
        var password = "SkyRocT12FileSecurity!2026";
        var userAgent = $"SkyRoc-T12-Files/{batch.Id}";
        var createName = "T12-Files";

        var filesCreatePermission = PermissionCodes.Business.Files.Create;
        var filesReadPermission = PermissionCodes.Business.Files.Read;
        var importExportReadPermission = PermissionCodes.Business.ImportExport.Read;

        Guid adminRoleId;
        var ownedStorageKeys = new List<string>();

        await using (var seedContext = fixture.CreateDbContext())
        {
            var adminRole = await seedContext.Roles.AsNoTracking()
                .SingleOrDefaultAsync(role => role.Code == SeedConstants.AdminRoleCode);
            Assert.NotNull(adminRole);
            adminRoleId = adminRole.Id;

            await seedContext.Roles.AddRangeAsync(
                new Role
                {
                    Id = limitedRoleId,
                    Code = limitedRoleCode,
                    Name = limitedRoleName,
                    Desc = "T12 文件相邻导入导出只读临时角色，仅本轮自动测试使用",
                    Status = Status.Enable,
                    CreateName = createName
                },
                new Role
                {
                    Id = peerRoleId,
                    Code = peerRoleCode,
                    Name = peerRoleName,
                    Desc = "T12 文件读写临时角色，用于创建人隔离断言",
                    Status = Status.Enable,
                    CreateName = createName
                });

            await seedContext.Users.AddRangeAsync(
                new User
                {
                    Id = ownerUserId,
                    Username = ownerUsername,
                    NickName = "T12文件操作员",
                    Gender = GenderType.Male,
                    Phone = "13900001211",
                    Email = $"{batch.Id}-o@skyroc-autotest.example",
                    PasswordHash = PasswordHasher.Hash(password),
                    Status = Status.Enable,
                    CreateName = createName
                },
                new User
                {
                    Id = limitedUserId,
                    Username = limitedUsername,
                    NickName = "T12导入导出只读用户",
                    Gender = GenderType.Female,
                    Phone = "13900001212",
                    Email = $"{batch.Id}-l@skyroc-autotest.example",
                    PasswordHash = PasswordHasher.Hash(password),
                    Status = Status.Enable,
                    CreateName = createName
                },
                new User
                {
                    Id = peerUserId,
                    Username = peerUsername,
                    NickName = "T12文件对端用户",
                    Gender = GenderType.Male,
                    Phone = "13900001213",
                    Email = $"{batch.Id}-p@skyroc-autotest.example",
                    PasswordHash = PasswordHasher.Hash(password),
                    Status = Status.Enable,
                    CreateName = createName
                });

            await seedContext.UserRoles.AddRangeAsync(
                new UserRole
                {
                    UserId = ownerUserId,
                    RoleId = adminRoleId
                },
                new UserRole
                {
                    UserId = limitedUserId,
                    RoleId = limitedRoleId
                },
                new UserRole
                {
                    UserId = peerUserId,
                    RoleId = peerRoleId
                });

            await seedContext.Menus.AddRangeAsync(
                new Menu
                {
                    Id = limitedMenuId,
                    Name = limitedMenuName,
                    Path = $"/{batch.Id}lim",
                    Title = "T12导入导出相邻菜单",
                    Component = "page.t12.files.limited",
                    MenuType = MenuType.Menu,
                    Order = 9613,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new Menu
                {
                    Id = peerMenuId,
                    Name = peerMenuName,
                    Path = $"/{batch.Id}peer",
                    Title = "T12文件读写菜单",
                    Component = "page.t12.files.peer",
                    MenuType = MenuType.Menu,
                    Order = 9614,
                    Status = Status.Enable,
                    CreateName = createName
                });

            await seedContext.MenuButtons.AddRangeAsync(
                new MenuButton
                {
                    Id = limitedImportReadButtonId,
                    Code = importExportReadPermission,
                    Desc = "T12 导入导出读取相邻权限按钮",
                    MenuId = limitedMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = peerFileReadButtonId,
                    Code = filesReadPermission,
                    Desc = "T12 文件读取权限按钮",
                    MenuId = peerMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = peerFileCreateButtonId,
                    Code = filesCreatePermission,
                    Desc = "T12 文件上传权限按钮",
                    MenuId = peerMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                });

            await seedContext.RoleMenus.AddRangeAsync(
                new RoleMenu
                {
                    RoleId = limitedRoleId,
                    MenuId = limitedMenuId
                },
                new RoleMenu
                {
                    RoleId = peerRoleId,
                    MenuId = peerMenuId
                });

            await seedContext.SaveChangesAsync();
        }

        registry.Register<Role>(limitedRoleId, nameof(Role.Code), limitedRoleCode);
        registry.Register<Role>(peerRoleId, nameof(Role.Code), peerRoleCode);
        registry.Register<User>(ownerUserId, nameof(User.Username), ownerUsername);
        registry.Register<User>(limitedUserId, nameof(User.Username), limitedUsername);
        registry.Register<User>(peerUserId, nameof(User.Username), peerUsername);
        registry.Register<Menu>(limitedMenuId, nameof(Menu.Name), limitedMenuName);
        registry.Register<Menu>(peerMenuId, nameof(Menu.Name), peerMenuName);

        Guid? ownerPdfId = null;
        Guid? ownerPngId = null;
        Guid? peerPdfId = null;

        try
        {
            using var factory = fixture.CreateWebApplicationFactory();
            using var anonymousClient = factory.CreateClient();
            anonymousClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            using (var anonymousUpload = await anonymousClient.PostAsync(
                       "/api/files",
                       BuildUploadContent(pdfFileName, "application/pdf", ValidPdfPayload)))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousUpload, ResponseCode.Unauthorized);
            }

            using (var anonymousDownload = await anonymousClient.GetAsync($"/api/files/{Guid.NewGuid()}/download"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousDownload, ResponseCode.Unauthorized);
            }

            LoginResDto limitedLogin;
            using (var loginResponse = await anonymousClient.PostAsJsonAsync("/api/auth/login", new LoginReqDto
            {
                Username = limitedUsername,
                Password = password
            }))
            {
                Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
                limitedLogin = await ReadApiDataAsync<LoginResDto>(loginResponse);
            }

            using var limitedClient = factory.CreateClient();
            limitedClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.BearerScheme, limitedLogin.Token);
            limitedClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            using (var infoResponse = await limitedClient.GetAsync("/api/auth/getUserInfo"))
            {
                var info = await ReadApiDataAsync<UserInfoDto>(infoResponse);
                Assert.Contains(importExportReadPermission, info.Permissions);
                Assert.DoesNotContain(filesCreatePermission, info.Permissions);
                Assert.DoesNotContain(filesReadPermission, info.Permissions);
            }

            using (var deniedUpload = await limitedClient.PostAsync(
                       "/api/files",
                       BuildUploadContent(pdfFileName, "application/pdf", ValidPdfPayload)))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedUpload, ResponseCode.Forbidden);
            }

            using (var deniedDownload = await limitedClient.GetAsync($"/api/files/{Guid.NewGuid()}/download"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedDownload, ResponseCode.Forbidden);
            }

            LoginResDto ownerLogin;
            using (var loginResponse = await anonymousClient.PostAsJsonAsync("/api/auth/login", new LoginReqDto
            {
                Username = ownerUsername,
                Password = password
            }))
            {
                Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
                ownerLogin = await ReadApiDataAsync<LoginResDto>(loginResponse);
            }

            using var ownerClient = factory.CreateClient();
            ownerClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.BearerScheme, ownerLogin.Token);
            ownerClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            var batchFileCountBeforeRejects = await CountBatchStoredFilesAsync(batch.Id);

            using (var missingFile = await ownerClient.PostAsync("/api/files", new MultipartFormDataContent()))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(missingFile, ResponseCode.BadRequest);
            }

            using (var pathTraversal = await ownerClient.PostAsync(
                       "/api/files",
                       BuildUploadContent(pathTraversalName, "application/pdf", ValidPdfPayload)))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(pathTraversal, ResponseCode.DatabaseError);
            }

            using (var spoofedContent = await ownerClient.PostAsync(
                       "/api/files",
                       BuildUploadContent(spoofedPdfName, "application/pdf", FakePdfAsPngBytes)))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(spoofedContent, ResponseCode.DatabaseError);
            }

            Assert.Equal(batchFileCountBeforeRejects, await CountBatchStoredFilesAsync(batch.Id));

            StoredFileDto ownerPdf;
            using (var uploadResponse = await ownerClient.PostAsync(
                       "/api/files",
                       BuildUploadContent(pdfFileName, "application/pdf", ValidPdfPayload)))
            {
                Assert.Equal(HttpStatusCode.OK, uploadResponse.StatusCode);
                ownerPdf = await ReadApiDataAsync<StoredFileDto>(uploadResponse);
                Assert.Equal(pdfFileName, ownerPdf.FileName);
                Assert.Equal("application/pdf", ownerPdf.ContentType);
                Assert.Equal(ValidPdfPayload.Length, ownerPdf.Size);
                Assert.Equal($"/api/files/{ownerPdf.Id}/download", ownerPdf.DownloadUrl);
                ownerPdfId = ownerPdf.Id;
            }

            await using (var metaContext = fixture.CreateDbContext())
            {
                var stored = await metaContext.StoredFiles.AsNoTracking()
                    .SingleAsync(file => file.Id == ownerPdf.Id);
                Assert.Equal(ownerUserId, stored.CreateBy);
                Assert.Equal(pdfFileName, stored.OriginalFileName);
                Assert.Equal("application/pdf", stored.ContentType);
                Assert.DoesNotContain(pdfFileName, stored.StorageKey, StringComparison.Ordinal);
                Assert.DoesNotContain("..", stored.StorageKey, StringComparison.Ordinal);
                Assert.True(await fixture.ObjectStorage.ExistsAsync(stored.StorageKey));
                ownedStorageKeys.Add(stored.StorageKey);
                registry.Register<StoredFile>(stored.Id, nameof(StoredFile.OriginalFileName), pdfFileName);
            }

            using (var downloadResponse = await ownerClient.GetAsync(ownerPdf.DownloadUrl))
            {
                Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);
                Assert.Equal("application/pdf", downloadResponse.Content.Headers.ContentType?.MediaType);
                var disposition = downloadResponse.Content.Headers.ContentDisposition;
                Assert.NotNull(disposition);
                Assert.Equal(pdfFileName, disposition.FileNameStar ?? disposition.FileName?.Trim('"'));
                var bytes = await downloadResponse.Content.ReadAsByteArrayAsync();
                Assert.Equal(ValidPdfPayload, bytes);
            }

            StoredFileDto ownerPng;
            using (var uploadPng = await ownerClient.PostAsync(
                       "/api/files",
                       BuildUploadContent(pngFileName, "image/png", ValidPngPayload)))
            {
                Assert.Equal(HttpStatusCode.OK, uploadPng.StatusCode);
                ownerPng = await ReadApiDataAsync<StoredFileDto>(uploadPng);
                Assert.Equal("image/png", ownerPng.ContentType);
                ownerPngId = ownerPng.Id;
            }

            await using (var pngContext = fixture.CreateDbContext())
            {
                var storedPng = await pngContext.StoredFiles.AsNoTracking()
                    .SingleAsync(file => file.Id == ownerPng.Id);
                Assert.True(await fixture.ObjectStorage.ExistsAsync(storedPng.StorageKey));
                ownedStorageKeys.Add(storedPng.StorageKey);
                registry.Register<StoredFile>(storedPng.Id, nameof(StoredFile.OriginalFileName), pngFileName);
            }

            LoginResDto peerLogin;
            using (var loginResponse = await anonymousClient.PostAsJsonAsync("/api/auth/login", new LoginReqDto
            {
                Username = peerUsername,
                Password = password
            }))
            {
                Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
                peerLogin = await ReadApiDataAsync<LoginResDto>(loginResponse);
            }

            using var peerClient = factory.CreateClient();
            peerClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.BearerScheme, peerLogin.Token);
            peerClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            using (var infoResponse = await peerClient.GetAsync("/api/auth/getUserInfo"))
            {
                var info = await ReadApiDataAsync<UserInfoDto>(infoResponse);
                Assert.Contains(filesCreatePermission, info.Permissions);
                Assert.Contains(filesReadPermission, info.Permissions);
            }

            using (var crossUserDownload = await peerClient.GetAsync(ownerPdf.DownloadUrl))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(crossUserDownload, ResponseCode.NotFound);
            }

            StoredFileDto peerPdf;
            using (var peerUpload = await peerClient.PostAsync(
                       "/api/files",
                       BuildUploadContent(peerPdfFileName, "application/pdf", ValidPdfPayload)))
            {
                Assert.Equal(HttpStatusCode.OK, peerUpload.StatusCode);
                peerPdf = await ReadApiDataAsync<StoredFileDto>(peerUpload);
                peerPdfId = peerPdf.Id;
            }

            await using (var peerMeta = fixture.CreateDbContext())
            {
                var storedPeer = await peerMeta.StoredFiles.AsNoTracking()
                    .SingleAsync(file => file.Id == peerPdf.Id);
                Assert.Equal(peerUserId, storedPeer.CreateBy);
                Assert.True(await fixture.ObjectStorage.ExistsAsync(storedPeer.StorageKey));
                ownedStorageKeys.Add(storedPeer.StorageKey);
                registry.Register<StoredFile>(storedPeer.Id, nameof(StoredFile.OriginalFileName), peerPdfFileName);
            }

            using (var peerSelfDownload = await peerClient.GetAsync(peerPdf.DownloadUrl))
            {
                Assert.Equal(HttpStatusCode.OK, peerSelfDownload.StatusCode);
                Assert.Equal(ValidPdfPayload, await peerSelfDownload.Content.ReadAsByteArrayAsync());
            }

            using (var ownerCannotSeePeer = await ownerClient.GetAsync(peerPdf.DownloadUrl))
            {
                // 创建人隔离在服务层按 CreateBy 过滤，即使持有 *:*:* 也不能下载他人文件。
                await ApiHttpAssert.AssertBusinessCodeAsync(ownerCannotSeePeer, ResponseCode.NotFound);
            }

            using (var ownerStillOwns = await ownerClient.GetAsync(ownerPdf.DownloadUrl))
            {
                Assert.Equal(HttpStatusCode.OK, ownerStillOwns.StatusCode);
                Assert.Equal(ValidPdfPayload, await ownerStillOwns.Content.ReadAsByteArrayAsync());
            }

            await using (var logContext = fixture.CreateDbContext())
            {
                var loginLogs = await logContext.LoginLogs.AsNoTracking()
                    .Where(log => log.Username == ownerUsername
                                  || log.Username == limitedUsername
                                  || log.Username == peerUsername)
                    .ToListAsync();
                Assert.Contains(loginLogs, log => log.Username == ownerUsername && log.IsSuccess);
                Assert.Contains(loginLogs, log => log.Username == limitedUsername && log.IsSuccess);
                Assert.Contains(loginLogs, log => log.Username == peerUsername && log.IsSuccess);
                RegisterLoginLogs(registry, loginLogs);
            }

            await RegisterBatchOperationLogsAsync(fixture, registry, ownerUsername, limitedUsername, peerUsername);
        }
        finally
        {
            await RegisterResidualLoginLogsAsync(
                fixture,
                registry,
                ownerUsername,
                limitedUsername,
                peerUsername);
            await RegisterBatchOperationLogsAsync(
                fixture,
                registry,
                ownerUsername,
                limitedUsername,
                peerUsername);

            await fixture.CleanupBatchAsync(registry);
            await fixture.CleanupBatchAsync(registry);

            foreach (var storageKey in ownedStorageKeys.Distinct(StringComparer.Ordinal))
            {
                await fixture.ObjectStorage.DeleteAsync(storageKey);
            }

            await using (var cleanupContext = fixture.CreateDbContext())
            {
                var residualUsernames = new[]
                {
                    ownerUsername,
                    limitedUsername,
                    peerUsername
                };
                var residualUserIds = new List<Guid> { ownerUserId, limitedUserId, peerUserId };
                var residualFileIds = new List<Guid>();
                if (ownerPdfId.HasValue)
                    residualFileIds.Add(ownerPdfId.Value);
                if (ownerPngId.HasValue)
                    residualFileIds.Add(ownerPngId.Value);
                if (peerPdfId.HasValue)
                    residualFileIds.Add(peerPdfId.Value);

                var residualFiles = await cleanupContext.StoredFiles
                    .Where(file => residualFileIds.Contains(file.Id)
                                   || file.OriginalFileName.StartsWith(batch.Id)
                                   || file.CreateBy == ownerUserId
                                   || file.CreateBy == peerUserId
                                   || file.CreateBy == limitedUserId)
                    .ToListAsync();
                if (residualFiles.Count > 0)
                {
                    foreach (var file in residualFiles)
                    {
                        await fixture.ObjectStorage.DeleteAsync(file.StorageKey);
                    }

                    cleanupContext.StoredFiles.RemoveRange(residualFiles);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualUserRoles = await cleanupContext.UserRoles
                    .Where(relation => residualUserIds.Contains(relation.UserId)
                                       || cleanupContext.Users.Any(user =>
                                           user.Id == relation.UserId
                                           && residualUsernames.Contains(user.Username)))
                    .ToListAsync();
                if (residualUserRoles.Count > 0)
                {
                    cleanupContext.UserRoles.RemoveRange(residualUserRoles);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualUsers = await cleanupContext.Users
                    .Where(user => residualUserIds.Contains(user.Id)
                                   || residualUsernames.Contains(user.Username)
                                   || (user.Username != null && user.Username.StartsWith(batch.Id)))
                    .ToListAsync();
                if (residualUsers.Count > 0)
                {
                    cleanupContext.Users.RemoveRange(residualUsers);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualRoleMenus = await cleanupContext.RoleMenus
                    .Where(relation => relation.RoleId == limitedRoleId
                                       || relation.RoleId == peerRoleId
                                       || relation.MenuId == limitedMenuId
                                       || relation.MenuId == peerMenuId)
                    .ToListAsync();
                if (residualRoleMenus.Count > 0)
                {
                    cleanupContext.RoleMenus.RemoveRange(residualRoleMenus);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualRoles = await cleanupContext.Roles
                    .Where(role => role.Id == limitedRoleId
                                   || role.Id == peerRoleId
                                   || role.Code == limitedRoleCode
                                   || role.Code == peerRoleCode
                                   || (role.Code != null && role.Code.StartsWith(batch.Id)))
                    .ToListAsync();
                if (residualRoles.Count > 0)
                {
                    cleanupContext.Roles.RemoveRange(residualRoles);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualButtons = await cleanupContext.MenuButtons
                    .Where(button => button.Id == limitedImportReadButtonId
                                     || button.Id == peerFileReadButtonId
                                     || button.Id == peerFileCreateButtonId
                                     || button.MenuId == limitedMenuId
                                     || button.MenuId == peerMenuId
                                     || button.CreateName == createName)
                    .ToListAsync();
                if (residualButtons.Count > 0)
                {
                    cleanupContext.MenuButtons.RemoveRange(residualButtons);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualMenus = await cleanupContext.Menus
                    .Where(menu => menu.Id == limitedMenuId
                                   || menu.Id == peerMenuId
                                   || menu.Name == limitedMenuName
                                   || menu.Name == peerMenuName)
                    .ToListAsync();
                if (residualMenus.Count > 0)
                {
                    cleanupContext.Menus.RemoveRange(residualMenus);
                    await cleanupContext.SaveChangesAsync();
                }
            }

            await using var residualContext = fixture.CreateDbContext();
            Assert.False(await residualContext.Users.AnyAsync(user =>
                user.Username == ownerUsername
                || user.Username == limitedUsername
                || user.Username == peerUsername
                || (user.Username != null && user.Username.StartsWith(batch.Id))));
            Assert.False(await residualContext.Roles.AnyAsync(role =>
                role.Id == limitedRoleId
                || role.Id == peerRoleId
                || role.Code == limitedRoleCode
                || role.Code == peerRoleCode
                || (role.Code != null && role.Code.StartsWith(batch.Id))));
            Assert.False(await residualContext.Menus.AnyAsync(menu =>
                menu.Id == limitedMenuId
                || menu.Id == peerMenuId
                || menu.Name == limitedMenuName
                || menu.Name == peerMenuName));
            Assert.False(await residualContext.MenuButtons.AnyAsync(button =>
                button.Id == limitedImportReadButtonId
                || button.Id == peerFileReadButtonId
                || button.Id == peerFileCreateButtonId
                || button.CreateName == createName));
            Assert.False(await residualContext.StoredFiles.AnyAsync(file =>
                (ownerPdfId.HasValue && file.Id == ownerPdfId.Value)
                || (ownerPngId.HasValue && file.Id == ownerPngId.Value)
                || (peerPdfId.HasValue && file.Id == peerPdfId.Value)
                || file.OriginalFileName.StartsWith(batch.Id)
                || file.CreateBy == ownerUserId
                || file.CreateBy == peerUserId
                || file.CreateBy == limitedUserId));

            foreach (var storageKey in ownedStorageKeys.Distinct(StringComparer.Ordinal))
            {
                Assert.False(await fixture.ObjectStorage.ExistsAsync(storageKey));
            }
        }
    }

    private async Task<int> CountBatchStoredFilesAsync(string batchId)
    {
        await using var context = fixture.CreateDbContext();
        return await context.StoredFiles.AsNoTracking()
            .CountAsync(file => file.OriginalFileName.StartsWith(batchId));
    }

    private static MultipartFormDataContent BuildUploadContent(string fileName, string contentType, byte[] payload)
    {
        var fileContent = new ByteArrayContent(payload);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        var form = new MultipartFormDataContent();
        form.Add(fileContent, "file", fileName);
        return form;
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
