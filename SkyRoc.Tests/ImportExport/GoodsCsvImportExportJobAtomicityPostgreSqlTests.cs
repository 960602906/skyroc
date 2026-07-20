using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Application.DTOs.Auth;
using Application.DTOs.ImportExport;
using Domain.Entities;
using Domain.Entities.ImportExport;
using Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
using Shared.Common;
using Shared.Constants;
using Shared.Utils;
using SkyRoc.Tests.Common;
using SkyRoc.Tests.Testing.PostgreSql;
using Xunit;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace SkyRoc.Tests.ImportExport;

/// <summary>
///     T12 切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证商品 CSV 导入导出
///     整文件校验原子写入、任务状态落库、导出快照与 401/403 权限矩阵。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class GoodsCsvImportExportJobAtomicityPostgreSqlTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     模板下载→有效多行导入落库并成功任务→同文件校验失败零写入→导出含公式转义与任务头→
    ///     本人任务可查/他人任务 404→空文件/非 CSV 拒绝→401/403（文件相邻权限无导入导出）权限矩阵；
    ///     临时批次数据精确清理。
    /// </summary>
    [Fact]
    public async Task GoodsCsvImportExport_JobAtomicityAndPermissionMatrix_OnPostgreSql()
    {
        var batch = TestBatchContext.Create();
        var registry = new BatchCleanupRegistry(batch);

        var limitedRoleId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var limitedUserId = Guid.NewGuid();
        var seedMenuId = Guid.NewGuid();
        var seedFileReadButtonId = Guid.NewGuid();

        var adminUsername = $"{batch.Id}A";
        var limitedUsername = $"{batch.Id}L";
        var limitedRoleCode = $"{batch.Id}R";
        var limitedRoleName = $"{batch.Id}N";
        var seedMenuName = $"{batch.Id}S";
        var importFileName = $"{batch.Id}-goods-import.csv";
        var failImportFileName = $"{batch.Id}-goods-import-fail.csv";
        var formulaImportFileName = $"{batch.Id}-goods-formula.csv";
        var goodsCode1 = $"{batch.Id}G1";
        var goodsCode2 = $"{batch.Id}G2";
        var goodsCodeFormula = $"{batch.Id}GF";
        var goodsName1 = $"{batch.Id}-联调黄瓜";
        var goodsName2 = $"{batch.Id}-联调青椒";
        var goodsNameFormula = $"=HYPERLINK(\"https://unsafe.example/{batch.Id}\")";
        var password = "SkyRocT12ImportExport!2026";
        var userAgent = $"SkyRoc-T12-ImportExport/{batch.Id}";
        var createName = "T12-ImportExport";

        var importExportCreatePermission = PermissionCodes.Business.ImportExport.Create;
        var importExportReadPermission = PermissionCodes.Business.ImportExport.Read;
        var filesReadPermission = PermissionCodes.Business.Files.Read;

        Guid adminRoleId;
        Guid managedGoodsTypeId;

        await using (var seedContext = fixture.CreateDbContext())
        {
            var adminRole = await seedContext.Roles.AsNoTracking()
                .SingleOrDefaultAsync(role => role.Code == SeedConstants.AdminRoleCode);
            Assert.NotNull(adminRole);
            adminRoleId = adminRole.Id;

            var goodsTypeCode = DemoDataStableKeyCatalog.Create("GOODS-TYPE", 1);
            var goodsType = await seedContext.GoodsTypes.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == goodsTypeCode);
            Assert.NotNull(goodsType);
            managedGoodsTypeId = goodsType.Id;

            await seedContext.Roles.AddAsync(new Role
            {
                Id = limitedRoleId,
                Code = limitedRoleCode,
                Name = limitedRoleName,
                Desc = "T12 导入导出相邻文件只读临时角色，仅本轮自动测试使用",
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.Users.AddRangeAsync(
                new User
                {
                    Id = adminUserId,
                    Username = adminUsername,
                    NickName = "T12导入导出操作员",
                    Gender = GenderType.Male,
                    Phone = "13900001201",
                    Email = $"{batch.Id}-a@skyroc-autotest.example",
                    PasswordHash = PasswordHasher.Hash(password),
                    Status = Status.Enable,
                    CreateName = createName
                },
                new User
                {
                    Id = limitedUserId,
                    Username = limitedUsername,
                    NickName = "T12文件只读用户",
                    Gender = GenderType.Female,
                    Phone = "13900001202",
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
                Title = "T12文件只读菜单",
                Component = "page.t12.files.seed",
                MenuType = MenuType.Menu,
                Order = 9612,
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.MenuButtons.AddAsync(new MenuButton
            {
                Id = seedFileReadButtonId,
                Code = filesReadPermission,
                Desc = "T12 文件读取相邻权限按钮",
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

        Guid? successImportJobId = null;
        Guid? failedImportJobId = null;
        Guid? formulaImportJobId = null;
        Guid? exportJobId = null;
        Guid? goods1Id = null;
        Guid? goods2Id = null;
        Guid? formulaGoodsId = null;

        try
        {
            using var factory = fixture.CreateWebApplicationFactory();
            using var anonymousClient = factory.CreateClient();
            anonymousClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            using (var anonymousTemplate = await anonymousClient.GetAsync("/api/import-export/jobs/templates/1"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousTemplate, ResponseCode.Unauthorized);
            }

            using (var anonymousImport = await anonymousClient.PostAsync(
                       "/api/import-export/jobs/import/1",
                       BuildCsvUploadContent(importFileName, BuildValidCsv(managedGoodsTypeId, goodsCode1, goodsName1))))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousImport, ResponseCode.Unauthorized);
            }

            using (var anonymousExport = await anonymousClient.GetAsync("/api/import-export/jobs/export/1"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousExport, ResponseCode.Unauthorized);
            }

            using (var anonymousGet = await anonymousClient.GetAsync($"/api/import-export/jobs/{Guid.NewGuid()}"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousGet, ResponseCode.Unauthorized);
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
                Assert.Contains(filesReadPermission, info.Permissions);
                Assert.DoesNotContain(importExportCreatePermission, info.Permissions);
                Assert.DoesNotContain(importExportReadPermission, info.Permissions);
            }

            using (var deniedTemplate = await limitedClient.GetAsync("/api/import-export/jobs/templates/1"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedTemplate, ResponseCode.Forbidden);
            }

            using (var deniedImport = await limitedClient.PostAsync(
                       "/api/import-export/jobs/import/1",
                       BuildCsvUploadContent(importFileName, BuildValidCsv(managedGoodsTypeId, goodsCode1, goodsName1))))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedImport, ResponseCode.Forbidden);
            }

            using (var deniedExport = await limitedClient.GetAsync("/api/import-export/jobs/export/1"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedExport, ResponseCode.Forbidden);
            }

            using (var deniedGet = await limitedClient.GetAsync($"/api/import-export/jobs/{Guid.NewGuid()}"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedGet, ResponseCode.Forbidden);
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
            }

            using var adminClient = factory.CreateClient();
            adminClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.BearerScheme, adminLogin.Token);
            adminClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            using (var templateResponse = await adminClient.GetAsync("/api/import-export/jobs/templates/1"))
            {
                Assert.Equal(HttpStatusCode.OK, templateResponse.StatusCode);
                Assert.Equal("text/csv; charset=utf-8", templateResponse.Content.Headers.ContentType?.ToString());
                var disposition = templateResponse.Content.Headers.ContentDisposition?.FileName?.Trim('"');
                Assert.Equal("goods-import-template.csv", disposition);
                var text = await templateResponse.Content.ReadAsStringAsync();
                Assert.StartsWith("Name,Code,GoodsTypeId,Spec,Brand,Origin,TaxRate,IsOnSale,Remark", text);
                Assert.Contains("示例商品", text);
            }

            using (var emptyImport = await adminClient.PostAsync(
                       "/api/import-export/jobs/import/1",
                       BuildCsvUploadContent($"{batch.Id}-empty.csv", string.Empty)))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(emptyImport, ResponseCode.BadRequest);
            }

            using (var nonCsvImport = await adminClient.PostAsync(
                       "/api/import-export/jobs/import/1",
                       BuildCsvUploadContent($"{batch.Id}-goods.txt", "not-a-csv")))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(nonCsvImport, ResponseCode.BadRequest);
            }

            var successCsv = string.Join(
                "\r\n",
                "Name,Code,GoodsTypeId,Spec,Brand,Origin,TaxRate,IsOnSale,Remark",
                $"{Escape(goodsName1)},{goodsCode1},{managedGoodsTypeId},500g/份,华东鲜蔬,山东寿光,6.0000,true,{Escape($"{batch.Id}-导入黄瓜")}",
                $"{Escape(goodsName2)},{goodsCode2},{managedGoodsTypeId},300g/份,华东鲜蔬,山东寿光,9.0000,false,{Escape($"{batch.Id}-导入青椒")}",
                string.Empty);

            ImportExportJobDto successJob;
            using (var importResponse = await adminClient.PostAsync(
                       "/api/import-export/jobs/import/1",
                       BuildCsvUploadContent(importFileName, successCsv)))
            {
                Assert.Equal(HttpStatusCode.OK, importResponse.StatusCode);
                successJob = await ReadApiDataAsync<ImportExportJobDto>(importResponse);
                Assert.Equal(ImportExportJobStatus.Succeeded, successJob.JobStatus);
                Assert.Equal(ImportExportDirection.Import, successJob.Direction);
                Assert.Equal(ImportExportJobType.Goods, successJob.JobType);
                Assert.Equal(importFileName, successJob.FileName);
                Assert.Equal(2, successJob.TotalRows);
                Assert.Equal(2, successJob.SuccessRows);
                Assert.Equal(0, successJob.FailureRows);
                Assert.True(string.IsNullOrWhiteSpace(successJob.ErrorSummary));
                Assert.NotNull(successJob.FinishedTime);
                successImportJobId = successJob.Id;
                registry.Register<ImportExportJob>(
                    successJob.Id,
                    nameof(ImportExportJob.SourceFileName),
                    importFileName);
            }

            await using (var afterSuccess = fixture.CreateDbContext())
            {
                var goods = await afterSuccess.Goods.AsNoTracking()
                    .Where(item => item.Code == goodsCode1 || item.Code == goodsCode2)
                    .OrderBy(item => item.Code)
                    .ToListAsync();
                Assert.Equal(2, goods.Count);
                Assert.Equal(goodsName1, goods[0].Name);
                Assert.Equal(goodsCode1, goods[0].Code);
                Assert.Equal(managedGoodsTypeId, goods[0].GoodsTypeId);
                Assert.Equal(6.0000m, goods[0].TaxRate);
                Assert.True(goods[0].IsOnSale);
                Assert.Equal(goodsName2, goods[1].Name);
                Assert.Equal(9.0000m, goods[1].TaxRate);
                Assert.False(goods[1].IsOnSale);
                goods1Id = goods[0].Id;
                goods2Id = goods[1].Id;
                registry.Register<GoodsEntity>(goods[0].Id, nameof(GoodsEntity.Code), goodsCode1);
                registry.Register<GoodsEntity>(goods[1].Id, nameof(GoodsEntity.Code), goodsCode2);

                var jobEntity = await afterSuccess.ImportExportJobs.AsNoTracking()
                    .SingleAsync(job => job.Id == successJob.Id);
                Assert.Equal(ImportExportJobStatus.Succeeded, jobEntity.JobStatus);
                Assert.Equal(2, jobEntity.SuccessRows);
                Assert.Equal(adminUserId, jobEntity.CreateBy);
                Assert.Equal(adminUsername, jobEntity.CreateName);
            }

            using (var getSuccess = await adminClient.GetAsync($"/api/import-export/jobs/{successJob.Id}"))
            {
                var queried = await ReadApiDataAsync<ImportExportJobDto>(getSuccess);
                Assert.Equal(successJob.Id, queried.Id);
                Assert.Equal(ImportExportJobStatus.Succeeded, queried.JobStatus);
                Assert.Equal(2, queried.SuccessRows);
            }

            using (var limitedGetOther = await limitedClient.GetAsync($"/api/import-export/jobs/{successJob.Id}"))
            {
                // 无导入导出读权限时先被权限拒绝，不泄露他人任务是否存在
                await ApiHttpAssert.AssertBusinessCodeAsync(limitedGetOther, ResponseCode.Forbidden);
            }

            var failCsv = string.Join(
                "\r\n",
                "Name,Code,GoodsTypeId,Spec,Brand,Origin,TaxRate,IsOnSale,Remark",
                $"{Escape($"{batch.Id}-联调茄子")},{batch.Id}G3,{managedGoodsTypeId},,,,6.0000,true,{Escape($"{batch.Id}-应整文件拒绝")}",
                $"{Escape($"{batch.Id}-联调重复编码")},{goodsCode1},{managedGoodsTypeId},,,,6.0000,true,{Escape($"{batch.Id}-编码已存在")}",
                string.Empty);

            var goodsCountBeforeFail = await CountBatchGoodsAsync(goodsCode1, goodsCode2, goodsCodeFormula);
            ImportExportJobDto failedJob;
            using (var failImportResponse = await adminClient.PostAsync(
                       "/api/import-export/jobs/import/1",
                       BuildCsvUploadContent(failImportFileName, failCsv)))
            {
                Assert.Equal(HttpStatusCode.OK, failImportResponse.StatusCode);
                failedJob = await ReadApiDataAsync<ImportExportJobDto>(failImportResponse);
                Assert.Equal(ImportExportJobStatus.Failed, failedJob.JobStatus);
                Assert.Equal(2, failedJob.TotalRows);
                Assert.Equal(0, failedJob.SuccessRows);
                Assert.Equal(2, failedJob.FailureRows);
                Assert.Contains("商品编码已存在", failedJob.ErrorSummary);
                Assert.NotNull(failedJob.FinishedTime);
                failedImportJobId = failedJob.Id;
                registry.Register<ImportExportJob>(
                    failedJob.Id,
                    nameof(ImportExportJob.SourceFileName),
                    failImportFileName);
            }

            Assert.Equal(goodsCountBeforeFail, await CountBatchGoodsAsync(goodsCode1, goodsCode2, goodsCodeFormula));
            await using (var afterFail = fixture.CreateDbContext())
            {
                Assert.False(await afterFail.Goods.AnyAsync(item => item.Code == $"{batch.Id}G3"));
                var jobEntity = await afterFail.ImportExportJobs.AsNoTracking()
                    .SingleAsync(job => job.Id == failedJob.Id);
                Assert.Equal(ImportExportJobStatus.Failed, jobEntity.JobStatus);
                Assert.Equal(0, jobEntity.SuccessRows);
                Assert.Equal(2, jobEntity.FailureRows);
                Assert.Contains("商品编码已存在", jobEntity.ErrorSummary);
            }

            var formulaCsv = string.Join(
                "\r\n",
                "Name,Code,GoodsTypeId,Spec,Brand,Origin,TaxRate,IsOnSale,Remark",
                $"{Escape(goodsNameFormula)},{goodsCodeFormula},{managedGoodsTypeId},,,,6.0000,true,{Escape($"{batch.Id}-公式转义")}",
                string.Empty);

            using (var formulaImportResponse = await adminClient.PostAsync(
                       "/api/import-export/jobs/import/1",
                       BuildCsvUploadContent(formulaImportFileName, formulaCsv)))
            {
                var formulaJob = await ReadApiDataAsync<ImportExportJobDto>(formulaImportResponse);
                Assert.Equal(ImportExportJobStatus.Succeeded, formulaJob.JobStatus);
                Assert.Equal(1, formulaJob.SuccessRows);
                formulaImportJobId = formulaJob.Id;
                registry.Register<ImportExportJob>(
                    formulaJob.Id,
                    nameof(ImportExportJob.SourceFileName),
                    formulaImportFileName);
            }

            await using (var afterFormula = fixture.CreateDbContext())
            {
                var formulaGoods = await afterFormula.Goods.AsNoTracking()
                    .SingleAsync(item => item.Code == goodsCodeFormula);
                Assert.Equal(goodsNameFormula, formulaGoods.Name);
                formulaGoodsId = formulaGoods.Id;
                registry.Register<GoodsEntity>(formulaGoods.Id, nameof(GoodsEntity.Code), goodsCodeFormula);
            }

            using (var exportResponse = await adminClient.GetAsync("/api/import-export/jobs/export/1"))
            {
                Assert.Equal(HttpStatusCode.OK, exportResponse.StatusCode);
                Assert.Equal("text/csv; charset=utf-8", exportResponse.Content.Headers.ContentType?.ToString());
                Assert.True(exportResponse.Headers.TryGetValues("X-Import-Export-Job-Id", out var jobHeaderValues));
                var exportJobHeaderId = Assert.Single(jobHeaderValues);
                Assert.True(Guid.TryParse(exportJobHeaderId, out var parsedExportJobId));
                exportJobId = parsedExportJobId;

                var exportText = await exportResponse.Content.ReadAsStringAsync();
                Assert.Contains(goodsCode1, exportText);
                Assert.Contains(goodsCode2, exportText);
                Assert.Contains(goodsCodeFormula, exportText);
                Assert.Contains("'=HYPERLINK", exportText);
                Assert.DoesNotContain(goodsNameFormula + ",", exportText);
            }

            await using (var afterExport = fixture.CreateDbContext())
            {
                var exportJob = await afterExport.ImportExportJobs.AsNoTracking()
                    .SingleAsync(job => job.Id == exportJobId);
                Assert.Equal(ImportExportJobStatus.Succeeded, exportJob.JobStatus);
                Assert.Equal(ImportExportDirection.Export, exportJob.JobDirection);
                Assert.Equal("goods-export.csv", exportJob.SourceFileName);
                Assert.True(exportJob.SuccessRows >= 3);
                Assert.Equal(adminUserId, exportJob.CreateBy);
                Assert.Equal(adminUsername, exportJob.CreateName);
                registry.Register<ImportExportJob>(
                    exportJob.Id,
                    nameof(ImportExportJob.CreateName),
                    adminUsername);
            }

            using (var getExport = await adminClient.GetAsync($"/api/import-export/jobs/{exportJobId}"))
            {
                var queried = await ReadApiDataAsync<ImportExportJobDto>(getExport);
                Assert.Equal(exportJobId, queried.Id);
                Assert.Equal(ImportExportJobStatus.Succeeded, queried.JobStatus);
                Assert.Equal(ImportExportDirection.Export, queried.Direction);
            }

            // 另一名具备 Admin 特权的临时用户查询他人任务应 404，避免跨用户泄露
            var otherAdminUserId = Guid.NewGuid();
            var otherAdminUsername = $"{batch.Id}B";
            await using (var otherSeed = fixture.CreateDbContext())
            {
                await otherSeed.Users.AddAsync(new User
                {
                    Id = otherAdminUserId,
                    Username = otherAdminUsername,
                    NickName = "T12另一操作员",
                    Gender = GenderType.Male,
                    Phone = "13900001203",
                    Email = $"{batch.Id}-b@skyroc-autotest.example",
                    PasswordHash = PasswordHasher.Hash(password),
                    Status = Status.Enable,
                    CreateName = createName
                });
                await otherSeed.UserRoles.AddAsync(new UserRole
                {
                    UserId = otherAdminUserId,
                    RoleId = adminRoleId
                });
                await otherSeed.SaveChangesAsync();
            }

            registry.Register<User>(otherAdminUserId, nameof(User.Username), otherAdminUsername);

            LoginResDto otherAdminLogin;
            using (var otherLogin = await anonymousClient.PostAsJsonAsync("/api/auth/login", new LoginReqDto
            {
                Username = otherAdminUsername,
                Password = password
            }))
            {
                otherAdminLogin = await ReadApiDataAsync<LoginResDto>(otherLogin);
            }

            using var otherAdminClient = factory.CreateClient();
            otherAdminClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.BearerScheme, otherAdminLogin.Token);
            otherAdminClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            using (var crossUserGet = await otherAdminClient.GetAsync($"/api/import-export/jobs/{successJob.Id}"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(crossUserGet, ResponseCode.NotFound);
            }

            await using (var logContext = fixture.CreateDbContext())
            {
                var loginLogs = await logContext.LoginLogs.AsNoTracking()
                    .Where(log => log.Username == adminUsername
                                  || log.Username == limitedUsername
                                  || log.Username == otherAdminUsername)
                    .ToListAsync();
                Assert.Contains(loginLogs, log => log.Username == adminUsername && log.IsSuccess);
                Assert.Contains(loginLogs, log => log.Username == limitedUsername && log.IsSuccess);
                RegisterLoginLogs(registry, loginLogs);
            }

            await RegisterBatchOperationLogsAsync(fixture, registry, adminUsername, limitedUsername, otherAdminUsername);
        }
        finally
        {
            await RegisterResidualLoginLogsAsync(
                fixture,
                registry,
                adminUsername,
                limitedUsername,
                $"{batch.Id}B");
            await RegisterBatchOperationLogsAsync(
                fixture,
                registry,
                adminUsername,
                limitedUsername,
                $"{batch.Id}B");

            await fixture.CleanupBatchAsync(registry);
            await fixture.CleanupBatchAsync(registry);

            await using (var cleanupContext = fixture.CreateDbContext())
            {
                var residualUsernames = new[]
                {
                    adminUsername,
                    limitedUsername,
                    $"{batch.Id}B"
                };
                var residualUserIds = new List<Guid> { adminUserId, limitedUserId };

                var residualGoodsCodes = new[]
                {
                    goodsCode1,
                    goodsCode2,
                    goodsCodeFormula,
                    $"{batch.Id}G3"
                };
                var residualGoodsIds = new List<Guid>();
                if (goods1Id.HasValue)
                    residualGoodsIds.Add(goods1Id.Value);
                if (goods2Id.HasValue)
                    residualGoodsIds.Add(goods2Id.Value);
                if (formulaGoodsId.HasValue)
                    residualGoodsIds.Add(formulaGoodsId.Value);

                var residualGoods = await cleanupContext.Goods
                    .Where(item => residualGoodsIds.Contains(item.Id)
                                   || residualGoodsCodes.Contains(item.Code)
                                   || (item.Code != null && item.Code.StartsWith(batch.Id)))
                    .ToListAsync();
                if (residualGoods.Count > 0)
                {
                    cleanupContext.Goods.RemoveRange(residualGoods);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualJobFileNames = new[]
                {
                    importFileName,
                    failImportFileName,
                    formulaImportFileName,
                    "goods-export.csv"
                };
                var residualJobIds = new List<Guid>();
                if (successImportJobId.HasValue)
                    residualJobIds.Add(successImportJobId.Value);
                if (failedImportJobId.HasValue)
                    residualJobIds.Add(failedImportJobId.Value);
                if (formulaImportJobId.HasValue)
                    residualJobIds.Add(formulaImportJobId.Value);
                if (exportJobId.HasValue)
                    residualJobIds.Add(exportJobId.Value);

                // 仅删除本轮操作员任务；SourceFileName=goods-export.csv 仅匹配同步导出默认名，与联调 batch 文件名不同
                var residualJobs = await cleanupContext.ImportExportJobs
                    .Where(job => residualJobIds.Contains(job.Id)
                                  || residualJobFileNames.Contains(job.SourceFileName)
                                  || job.CreateBy == adminUserId
                                  || job.CreateBy == limitedUserId
                                  || job.CreateName == adminUsername)
                    .ToListAsync();
                if (residualJobs.Count > 0)
                {
                    cleanupContext.ImportExportJobs.RemoveRange(residualJobs);
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
                    .Where(relation => relation.RoleId == limitedRoleId || relation.MenuId == seedMenuId)
                    .ToListAsync();
                if (residualRoleMenus.Count > 0)
                {
                    cleanupContext.RoleMenus.RemoveRange(residualRoleMenus);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualRoles = await cleanupContext.Roles
                    .Where(role => role.Id == limitedRoleId
                                   || role.Code == limitedRoleCode
                                   || (role.Code != null && role.Code.StartsWith(batch.Id)))
                    .ToListAsync();
                if (residualRoles.Count > 0)
                {
                    cleanupContext.Roles.RemoveRange(residualRoles);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualButtons = await cleanupContext.MenuButtons
                    .Where(button => button.Id == seedFileReadButtonId
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
            }

            await using var residualContext = fixture.CreateDbContext();
            Assert.False(await residualContext.Users.AnyAsync(user =>
                user.Username == adminUsername
                || user.Username == limitedUsername
                || user.Username == $"{batch.Id}B"
                || (user.Username != null && user.Username.StartsWith(batch.Id))));
            Assert.False(await residualContext.Roles.AnyAsync(role =>
                role.Id == limitedRoleId
                || role.Code == limitedRoleCode
                || (role.Code != null && role.Code.StartsWith(batch.Id))));
            Assert.False(await residualContext.Menus.AnyAsync(menu =>
                menu.Id == seedMenuId || menu.Name == seedMenuName));
            Assert.False(await residualContext.MenuButtons.AnyAsync(button =>
                button.Id == seedFileReadButtonId || button.CreateName == createName));
            Assert.False(await residualContext.Goods.AnyAsync(item =>
                item.Code == goodsCode1
                || item.Code == goodsCode2
                || item.Code == goodsCodeFormula
                || (item.Code != null && item.Code.StartsWith(batch.Id))));
            Assert.False(await residualContext.ImportExportJobs.AnyAsync(job =>
                job.SourceFileName == importFileName
                || job.SourceFileName == failImportFileName
                || job.SourceFileName == formulaImportFileName
                || (successImportJobId.HasValue && job.Id == successImportJobId.Value)
                || (failedImportJobId.HasValue && job.Id == failedImportJobId.Value)
                || (formulaImportJobId.HasValue && job.Id == formulaImportJobId.Value)
                || (exportJobId.HasValue && job.Id == exportJobId.Value)
                || job.CreateBy == adminUserId
                || job.CreateBy == limitedUserId));
        }
    }

    private async Task<int> CountBatchGoodsAsync(params string[] codes)
    {
        await using var context = fixture.CreateDbContext();
        return await context.Goods.AsNoTracking()
            .CountAsync(item => codes.Contains(item.Code));
    }

    private static MultipartFormDataContent BuildCsvUploadContent(string fileName, string csv)
    {
        var bytes = Encoding.UTF8.GetBytes(csv);
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        var form = new MultipartFormDataContent();
        form.Add(fileContent, "file", fileName);
        return form;
    }

    private static string BuildValidCsv(Guid goodsTypeId, string code, string name)
    {
        return string.Join(
            "\r\n",
            "Name,Code,GoodsTypeId,Spec,Brand,Origin,TaxRate,IsOnSale,Remark",
            $"{Escape(name)},{code},{goodsTypeId},,,,6.0000,true,",
            string.Empty);
    }

    private static string Escape(string value)
    {
        return value.IndexOfAny([',', '"', '\r', '\n']) >= 0
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
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
