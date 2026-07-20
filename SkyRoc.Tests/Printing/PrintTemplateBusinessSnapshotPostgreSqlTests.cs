using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs.Auth;
using Application.DTOs.Orders;
using Application.DTOs.Printing;
using Domain.Entities;
using Domain.Entities.Orders;
using Domain.Entities.Printing;
using Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
using Shared.Common;
using Shared.Constants;
using Shared.Utils;
using SkyRoc.Tests.Common;
using SkyRoc.Tests.Testing.PostgreSql;
using Xunit;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace SkyRoc.Tests.Printing;

/// <summary>
///     T12 切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证打印模板字段维护、
///     销售订单打印快照、正式打印确认副作用与 401/403 权限矩阵。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class PrintTemplateBusinessSnapshotPostgreSqlTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     模板创建/字段替换/停用不可选→销售订单预览快照不改打印状态→仅确认目标单 Printed→
    ///     非法字段/重复编码/采购确认拒绝→401/403（文件相邻权限无打印）权限矩阵；
    ///     临时批次数据精确清理。
    /// </summary>
    [Fact]
    public async Task PrintTemplate_BusinessSnapshotConfirmAndPermissionMatrix_OnPostgreSql()
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
        var enabledTemplateCode = $"T12P_{batch.Id.Replace("-", "_", StringComparison.Ordinal)}";
        var disabledTemplateCode = $"T12D_{batch.Id.Replace("-", "_", StringComparison.Ordinal)}";
        var enabledTemplateName = $"{batch.Id}-客户配送单模板";
        var disabledTemplateName = $"{batch.Id}-停用预览模板";
        var updatedTemplateName = $"{batch.Id}-客户配送单模板改";
        var printedOrderRemark = $"{batch.Id}O1";
        var untouchedOrderRemark = $"{batch.Id}O2";
        var printedDetailRemark = $"{batch.Id}L1";
        var untouchedDetailRemark = $"{batch.Id}L2";
        var password = "SkyRocT12PrintSnapshot!2026";
        var userAgent = $"SkyRoc-T12-Print/{batch.Id}";
        var createName = "T12-Print";

        var filesReadPermission = PermissionCodes.Business.Files.Read;
        var printTemplatesReadPermission = PermissionCodes.System.PrintTemplates.Read;
        var printTemplatesCreatePermission = PermissionCodes.System.PrintTemplates.Create;
        var printingReadPermission = PermissionCodes.Business.Printing.Read;
        var printingUpdatePermission = PermissionCodes.Business.Printing.Update;

        var orderQuantity = NumericPrecision.RoundQuantity(2m);
        var orderFixedPrice = NumericPrecision.RoundMoney(15.5m);

        Guid adminRoleId;
        Guid managedCustomerId;
        string managedCustomerName = null!;
        Guid managedGoodsId;
        string managedGoodsCode = null!;
        string managedGoodsName = null!;
        Guid managedGoodsUnitId;
        string managedGoodsUnitName = null!;
        Guid managedWareId;
        decimal expectedOrderTotal;

        await using (var seedContext = fixture.CreateDbContext())
        {
            var adminRole = await seedContext.Roles.AsNoTracking()
                .SingleOrDefaultAsync(role => role.Code == SeedConstants.AdminRoleCode);
            Assert.NotNull(adminRole);
            adminRoleId = adminRole.Id;

            var customerCode = DemoDataStableKeyCatalog.Create("CUSTOMER", 1);
            var goodsCode = DemoDataStableKeyCatalog.Create("GOODS", 1);
            var goodsUnitCode = DemoDataStableKeyCatalog.Create("GOODS-UNIT", 1);
            var wareCode = DemoDataStableKeyCatalog.Create("WARE", 1);

            var customer = await seedContext.Customers.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == customerCode);
            Assert.NotNull(customer);
            managedCustomerId = customer.Id;
            managedCustomerName = customer.Name;

            var goods = await seedContext.Set<GoodsEntity>().AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == goodsCode);
            Assert.NotNull(goods);
            managedGoodsId = goods.Id;
            managedGoodsCode = goods.Code;
            managedGoodsName = goods.Name;

            var goodsUnit = await seedContext.GoodsUnits.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == goodsUnitCode);
            Assert.NotNull(goodsUnit);
            Assert.Equal(goods.Id, goodsUnit.GoodsId);
            managedGoodsUnitId = goodsUnit.Id;
            managedGoodsUnitName = goodsUnit.Name;
            Assert.True(goodsUnit.ConversionRate > 0);
            expectedOrderTotal = NumericPrecision.RoundMoney(
                NumericPrecision.RoundQuantity(orderQuantity * goodsUnit.ConversionRate)
                / goodsUnit.ConversionRate
                * orderFixedPrice);

            var ware = await seedContext.Wares.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == wareCode);
            Assert.NotNull(ware);
            managedWareId = ware.Id;

            await seedContext.Roles.AddAsync(new Role
            {
                Id = limitedRoleId,
                Code = limitedRoleCode,
                Name = limitedRoleName,
                Desc = "T12 打印相邻文件只读临时角色，仅本轮自动测试使用",
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.Users.AddRangeAsync(
                new User
                {
                    Id = adminUserId,
                    Username = adminUsername,
                    NickName = "T12打印操作员",
                    Gender = GenderType.Male,
                    Phone = "13900001221",
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
                    Phone = "13900001222",
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
                Title = "T12文件相邻菜单",
                Component = "page.t12.print.limited",
                MenuType = MenuType.Menu,
                Order = 9621,
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

        Guid? enabledTemplateId = null;
        Guid? disabledTemplateId = null;
        Guid? printedOrderId = null;
        Guid? untouchedOrderId = null;

        try
        {
            using var factory = fixture.CreateWebApplicationFactory();
            using var anonymousClient = factory.CreateClient();
            anonymousClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            using (var anonymousTemplates = await anonymousClient.GetAsync("/api/print-templates?pageNumber=1&pageSize=10"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousTemplates, ResponseCode.Unauthorized);
            }

            using (var anonymousPrintData = await anonymousClient.GetAsync(
                       $"/api/print-data/{(int)PrintBusinessType.SaleOrder}?ids={Guid.NewGuid()}"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousPrintData, ResponseCode.Unauthorized);
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
                Assert.DoesNotContain(printTemplatesReadPermission, info.Permissions);
                Assert.DoesNotContain(printTemplatesCreatePermission, info.Permissions);
                Assert.DoesNotContain(printingReadPermission, info.Permissions);
                Assert.DoesNotContain(printingUpdatePermission, info.Permissions);
            }

            using (var deniedTemplates = await limitedClient.GetAsync("/api/print-templates?pageNumber=1&pageSize=10"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedTemplates, ResponseCode.Forbidden);
            }

            using (var deniedCreateTemplate = await limitedClient.PostAsJsonAsync(
                       "/api/print-templates",
                       new CreatePrintTemplateDto
                       {
                           TemplateCode = enabledTemplateCode,
                           Name = enabledTemplateName,
                           BusinessType = PrintBusinessType.SaleOrder,
                           DesignJson = "{\"version\":\"1\"}",
                           Fields =
                           [
                               new PrintTemplateFieldInputDto
                               {
                                   FieldKey = "documentNo",
                                   DisplayName = "订单号",
                                   DisplayOrder = 0
                               }
                           ]
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedCreateTemplate, ResponseCode.Forbidden);
            }

            using (var deniedPrintData = await limitedClient.GetAsync(
                       $"/api/print-data/{(int)PrintBusinessType.SaleOrder}?ids={Guid.NewGuid()}"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedPrintData, ResponseCode.Forbidden);
            }

            using (var deniedConfirm = await limitedClient.PostAsJsonAsync(
                       $"/api/print-data/{(int)PrintBusinessType.SaleOrder}/confirm",
                       new ConfirmPrintDto { Ids = [Guid.NewGuid()] }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedConfirm, ResponseCode.Forbidden);
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

            using (var invalidField = await adminClient.PostAsJsonAsync(
                       "/api/print-templates",
                       new CreatePrintTemplateDto
                       {
                           TemplateCode = enabledTemplateCode,
                           Name = enabledTemplateName,
                           BusinessType = PrintBusinessType.SaleOrder,
                           DesignJson = "{\"version\":\"1\"}",
                           Fields =
                           [
                               new PrintTemplateFieldInputDto
                               {
                                   FieldKey = "details[].goodsName",
                                   DisplayName = "非法字段",
                                   DisplayOrder = 0
                               }
                           ]
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(invalidField, ResponseCode.DatabaseError);
            }

            PrintTemplateDto enabledTemplate;
            using (var createEnabled = await adminClient.PostAsJsonAsync(
                       "/api/print-templates",
                       new CreatePrintTemplateDto
                       {
                           TemplateCode = enabledTemplateCode,
                           Name = enabledTemplateName,
                           BusinessType = PrintBusinessType.SaleOrder,
                           DesignJson = "{\"version\":\"1\",\"paper\":\"A4\"}",
                           IsEnabled = true,
                           Fields =
                           [
                               new PrintTemplateFieldInputDto
                               {
                                   FieldKey = "details[].itemName",
                                   DisplayName = "商品名称",
                                   DisplayOrder = 1
                               },
                               new PrintTemplateFieldInputDto
                               {
                                   FieldKey = "documentNo",
                                   DisplayName = "订单号",
                                   DisplayOrder = 0
                               }
                           ]
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createEnabled.StatusCode);
                enabledTemplate = await ReadApiDataAsync<PrintTemplateDto>(createEnabled);
                Assert.Equal(enabledTemplateCode, enabledTemplate.TemplateCode);
                Assert.Equal(enabledTemplateName, enabledTemplate.Name);
                Assert.Equal(PrintBusinessType.SaleOrder, enabledTemplate.BusinessType);
                Assert.True(enabledTemplate.IsEnabled);
                Assert.Equal(
                    ["documentNo", "details[].itemName"],
                    enabledTemplate.Fields.Select(field => field.FieldKey));
                enabledTemplateId = enabledTemplate.Id;
            }

            using (var duplicateCode = await adminClient.PostAsJsonAsync(
                       "/api/print-templates",
                       new CreatePrintTemplateDto
                       {
                           TemplateCode = enabledTemplateCode,
                           Name = $"{batch.Id}-重复编码模板",
                           BusinessType = PrintBusinessType.SaleOrder,
                           DesignJson = "{}",
                           Fields =
                           [
                               new PrintTemplateFieldInputDto
                               {
                                   FieldKey = "documentNo",
                                   DisplayName = "订单号",
                                   DisplayOrder = 0
                               }
                           ]
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(duplicateCode, ResponseCode.DatabaseError);
            }

            PrintTemplateDto updatedTemplate;
            using (var updateResponse = await adminClient.PutAsJsonAsync(
                       "/api/print-templates",
                       new UpdatePrintTemplateDto
                       {
                           Id = enabledTemplate.Id,
                           TemplateCode = enabledTemplateCode,
                           Name = updatedTemplateName,
                           BusinessType = PrintBusinessType.SaleOrder,
                           DesignJson = "{\"version\":\"2\",\"paper\":\"A4\"}",
                           IsEnabled = true,
                           Fields =
                           [
                               new PrintTemplateFieldInputDto
                               {
                                   FieldKey = "documentNo",
                                   DisplayName = "单据号",
                                   DisplayOrder = 0
                               },
                               new PrintTemplateFieldInputDto
                               {
                                   FieldKey = "details[].itemName",
                                   DisplayName = "品名",
                                   DisplayOrder = 1
                               },
                               new PrintTemplateFieldInputDto
                               {
                                   FieldKey = "totalAmount",
                                   DisplayName = "合计金额",
                                   DisplayOrder = 2
                               }
                           ]
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
                updatedTemplate = await ReadApiDataAsync<PrintTemplateDto>(updateResponse);
                Assert.Equal(updatedTemplateName, updatedTemplate.Name);
                Assert.Equal("{\"version\":\"2\",\"paper\":\"A4\"}", updatedTemplate.DesignJson);
                Assert.Equal(
                    [("documentNo", "单据号", 0), ("details[].itemName", "品名", 1), ("totalAmount", "合计金额", 2)],
                    updatedTemplate.Fields.Select(field => (field.FieldKey, field.DisplayName, field.DisplayOrder)));
            }

            await using (var afterUpdate = fixture.CreateDbContext())
            {
                var fieldCount = await afterUpdate.Set<PrintTemplateField>().AsNoTracking()
                    .CountAsync(field => field.PrintTemplateId == enabledTemplate.Id);
                Assert.Equal(3, fieldCount);
                var renamed = await afterUpdate.PrintTemplates.AsNoTracking()
                    .SingleAsync(item => item.Id == enabledTemplate.Id);
                Assert.Equal(updatedTemplateName, renamed.Name);
            }

            // 更新后名称已变，按最终归属值登记以便批次清理核对
            registry.Register<PrintTemplate>(
                enabledTemplate.Id,
                nameof(PrintTemplate.Name),
                updatedTemplateName);

            PrintTemplateDto disabledTemplate;
            using (var createDisabled = await adminClient.PostAsJsonAsync(
                       "/api/print-templates",
                       new CreatePrintTemplateDto
                       {
                           TemplateCode = disabledTemplateCode,
                           Name = disabledTemplateName,
                           BusinessType = PrintBusinessType.SaleOrder,
                           DesignJson = "{\"version\":\"disabled\"}",
                           IsEnabled = false,
                           Fields =
                           [
                               new PrintTemplateFieldInputDto
                               {
                                   FieldKey = "documentNo",
                                   DisplayName = "订单号",
                                   DisplayOrder = 0
                               }
                           ]
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createDisabled.StatusCode);
                disabledTemplate = await ReadApiDataAsync<PrintTemplateDto>(createDisabled);
                Assert.False(disabledTemplate.IsEnabled);
                disabledTemplateId = disabledTemplate.Id;
                registry.Register<PrintTemplate>(
                    disabledTemplate.Id,
                    nameof(PrintTemplate.Name),
                    disabledTemplateName);
            }

            using (var getEnabled = await adminClient.GetAsync($"/api/print-templates/by-code/{enabledTemplateCode}"))
            {
                var loaded = await ReadApiDataAsync<PrintTemplateDto>(getEnabled);
                Assert.Equal(enabledTemplate.Id, loaded.Id);
                Assert.Equal(3, loaded.Fields.Count);
            }

            using (var getDisabled = await adminClient.GetAsync($"/api/print-templates/by-code/{disabledTemplateCode}"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(getDisabled, ResponseCode.NotFound);
            }

            SaleOrderDto printedOrder;
            using (var createPrinted = await adminClient.PostAsJsonAsync(
                       "/api/orders",
                       new
                       {
                           customerId = managedCustomerId,
                           wareId = managedWareId,
                           orderDate = "2026-07-20T08:30:00Z",
                           receiveDate = "2026-07-21T06:00:00Z",
                           contactName = "联调食堂",
                           contactPhone = "13800131221",
                           deliveryAddress = "上海市浦东新区打印联调路 12 号",
                           remark = "T12打印确认目标订单",
                           innerRemark = printedOrderRemark,
                           details = new[]
                           {
                               new
                               {
                                   goodsId = managedGoodsId,
                                   goodsUnitId = managedGoodsUnitId,
                                   quantity = orderQuantity,
                                   fixedPrice = orderFixedPrice,
                                   fixedGoodsUnitId = managedGoodsUnitId,
                                   remark = "打印目标明细",
                                   innerRemark = printedDetailRemark
                               }
                           }
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createPrinted.StatusCode);
                printedOrder = await ReadApiDataAsync<SaleOrderDto>(createPrinted);
                Assert.Equal(managedCustomerName, printedOrder.CustomerName);
                Assert.Equal(expectedOrderTotal, printedOrder.OrderPrice);
                Assert.Equal(OrderPrintStatus.NotPrinted, printedOrder.PrintStatus);
                printedOrderId = printedOrder.Id;
                registry.Register<SaleOrder>(printedOrder.Id, nameof(SaleOrder.InnerRemark), printedOrderRemark);
            }

            SaleOrderDto untouchedOrder;
            using (var createUntouched = await adminClient.PostAsJsonAsync(
                       "/api/orders",
                       new
                       {
                           customerId = managedCustomerId,
                           wareId = managedWareId,
                           orderDate = "2026-07-20T09:00:00Z",
                           remark = "T12打印未确认对照订单",
                           innerRemark = untouchedOrderRemark,
                           details = new[]
                           {
                               new
                               {
                                   goodsId = managedGoodsId,
                                   goodsUnitId = managedGoodsUnitId,
                                   quantity = orderQuantity,
                                   fixedPrice = orderFixedPrice,
                                   fixedGoodsUnitId = managedGoodsUnitId,
                                   remark = "对照明细",
                                   innerRemark = untouchedDetailRemark
                               }
                           }
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createUntouched.StatusCode);
                untouchedOrder = await ReadApiDataAsync<SaleOrderDto>(createUntouched);
                untouchedOrderId = untouchedOrder.Id;
                registry.Register<SaleOrder>(untouchedOrder.Id, nameof(SaleOrder.InnerRemark), untouchedOrderRemark);
            }

            using (var previewResponse = await adminClient.GetAsync(
                       $"/api/print-data/{(int)PrintBusinessType.SaleOrder}?ids={printedOrder.Id}&ids={untouchedOrder.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);
                var documents = await ReadApiDataAsync<List<PrintDocumentDto>>(previewResponse);
                Assert.Equal(2, documents.Count);
                Assert.Equal(printedOrder.Id, documents[0].Id);
                Assert.Equal(printedOrder.OrderNo, documents[0].DocumentNo);
                Assert.Equal(managedCustomerName, documents[0].BusinessPartyName);
                Assert.Equal(expectedOrderTotal, documents[0].TotalAmount);
                var printedDetail = Assert.Single(documents[0].Details);
                Assert.Equal(managedGoodsName, printedDetail.ItemName);
                Assert.Equal(managedGoodsCode, printedDetail.ItemCode);
                Assert.Equal(managedGoodsUnitName, printedDetail.UnitName);
                Assert.Equal(orderQuantity, printedDetail.Quantity);
                Assert.Equal(orderFixedPrice, printedDetail.UnitPrice);
                Assert.Equal(expectedOrderTotal, printedDetail.TotalPrice);
                Assert.Equal(untouchedOrder.Id, documents[1].Id);
                Assert.Equal(untouchedOrder.OrderNo, documents[1].DocumentNo);
            }

            await using (var afterPreview = fixture.CreateDbContext())
            {
                Assert.Equal(
                    OrderPrintStatus.NotPrinted,
                    (await afterPreview.SaleOrders.AsNoTracking().SingleAsync(item => item.Id == printedOrder.Id))
                    .PrintStatus);
                Assert.Equal(
                    OrderPrintStatus.NotPrinted,
                    (await afterPreview.SaleOrders.AsNoTracking().SingleAsync(item => item.Id == untouchedOrder.Id))
                    .PrintStatus);
            }

            using (var confirmPurchase = await adminClient.PostAsJsonAsync(
                       $"/api/print-data/{(int)PrintBusinessType.PurchaseOrder}/confirm",
                       new ConfirmPrintDto { Ids = [printedOrder.Id] }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(confirmPurchase, ResponseCode.DatabaseError);
            }

            using (var confirmPrinted = await adminClient.PostAsJsonAsync(
                       $"/api/print-data/{(int)PrintBusinessType.SaleOrder}/confirm",
                       new ConfirmPrintDto { Ids = [printedOrder.Id] }))
            {
                Assert.Equal(HttpStatusCode.OK, confirmPrinted.StatusCode);
                Assert.True(await ReadApiDataAsync<bool>(confirmPrinted));
            }

            await using (var afterConfirm = fixture.CreateDbContext())
            {
                Assert.Equal(
                    OrderPrintStatus.Printed,
                    (await afterConfirm.SaleOrders.AsNoTracking().SingleAsync(item => item.Id == printedOrder.Id))
                    .PrintStatus);
                Assert.Equal(
                    OrderPrintStatus.NotPrinted,
                    (await afterConfirm.SaleOrders.AsNoTracking().SingleAsync(item => item.Id == untouchedOrder.Id))
                    .PrintStatus);
            }

            using (var missingIds = await adminClient.GetAsync(
                       $"/api/print-data/{(int)PrintBusinessType.SaleOrder}?ids={Guid.NewGuid()}"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(missingIds, ResponseCode.NotFound);
            }

            using (var deleteEnabled = await adminClient.DeleteAsync($"/api/print-templates/{enabledTemplate.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, deleteEnabled.StatusCode);
                Assert.True(await ReadApiDataAsync<bool>(deleteEnabled));
                enabledTemplateId = null;
            }

            await using (var afterDelete = fixture.CreateDbContext())
            {
                Assert.False(await afterDelete.PrintTemplates.AnyAsync(item => item.Id == enabledTemplate.Id));
                Assert.False(await afterDelete.Set<PrintTemplateField>()
                    .AnyAsync(field => field.PrintTemplateId == enabledTemplate.Id));
            }

            await using (var logContext = fixture.CreateDbContext())
            {
                var loginLogs = await logContext.LoginLogs.AsNoTracking()
                    .Where(log => log.Username == adminUsername || log.Username == limitedUsername)
                    .ToListAsync();
                Assert.Contains(loginLogs, log => log.Username == adminUsername && log.IsSuccess);
                Assert.Contains(loginLogs, log => log.Username == limitedUsername && log.IsSuccess);
                RegisterLoginLogs(registry, loginLogs);
            }

            await RegisterBatchOperationLogsAsync(fixture, registry, adminUsername, limitedUsername);
        }
        finally
        {
            await RegisterResidualLoginLogsAsync(fixture, registry, adminUsername, limitedUsername);
            await RegisterBatchOperationLogsAsync(fixture, registry, adminUsername, limitedUsername);

            await fixture.CleanupBatchAsync(registry);
            await fixture.CleanupBatchAsync(registry);

            await using (var cleanupContext = fixture.CreateDbContext())
            {
                var residualUsernames = new[] { adminUsername, limitedUsername };
                var residualUserIds = new List<Guid> { adminUserId, limitedUserId };

                var residualOrderIds = new List<Guid>();
                if (printedOrderId.HasValue)
                    residualOrderIds.Add(printedOrderId.Value);
                if (untouchedOrderId.HasValue)
                    residualOrderIds.Add(untouchedOrderId.Value);

                var residualOrders = await cleanupContext.SaleOrders
                    .Where(item => residualOrderIds.Contains(item.Id)
                                   || (item.InnerRemark != null
                                       && (item.InnerRemark == printedOrderRemark
                                           || item.InnerRemark == untouchedOrderRemark
                                           || item.InnerRemark.StartsWith(batch.Id))))
                    .ToListAsync();
                if (residualOrders.Count > 0)
                {
                    residualOrderIds = residualOrders.Select(item => item.Id).Distinct().ToList();
                    var residualDetails = await cleanupContext.SaleOrderDetails
                        .Where(detail => residualOrderIds.Contains(detail.SaleOrderId))
                        .ToListAsync();
                    if (residualDetails.Count > 0)
                    {
                        cleanupContext.SaleOrderDetails.RemoveRange(residualDetails);
                        await cleanupContext.SaveChangesAsync();
                    }

                    var residualAuditLogs = await cleanupContext.OrderAuditLogs
                        .Where(log => residualOrderIds.Contains(log.SaleOrderId))
                        .ToListAsync();
                    if (residualAuditLogs.Count > 0)
                    {
                        cleanupContext.OrderAuditLogs.RemoveRange(residualAuditLogs);
                        await cleanupContext.SaveChangesAsync();
                    }

                    cleanupContext.SaleOrders.RemoveRange(residualOrders);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualTemplateIds = new List<Guid>();
                if (enabledTemplateId.HasValue)
                    residualTemplateIds.Add(enabledTemplateId.Value);
                if (disabledTemplateId.HasValue)
                    residualTemplateIds.Add(disabledTemplateId.Value);

                var residualTemplates = await cleanupContext.PrintTemplates
                    .Where(item => residualTemplateIds.Contains(item.Id)
                                   || item.Name == enabledTemplateName
                                   || item.Name == updatedTemplateName
                                   || item.Name == disabledTemplateName
                                   || item.TemplateCode == enabledTemplateCode
                                   || item.TemplateCode == disabledTemplateCode
                                   || item.Name.StartsWith(batch.Id))
                    .ToListAsync();
                if (residualTemplates.Count > 0)
                {
                    residualTemplateIds = residualTemplates.Select(item => item.Id).Distinct().ToList();
                    var residualFields = await cleanupContext.Set<PrintTemplateField>()
                        .Where(field => residualTemplateIds.Contains(field.PrintTemplateId))
                        .ToListAsync();
                    if (residualFields.Count > 0)
                    {
                        cleanupContext.Set<PrintTemplateField>().RemoveRange(residualFields);
                        await cleanupContext.SaveChangesAsync();
                    }

                    cleanupContext.PrintTemplates.RemoveRange(residualTemplates);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualUserRoles = await cleanupContext.UserRoles
                    .Where(relation => residualUserIds.Contains(relation.UserId))
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
                    .Where(menu => menu.Id == seedMenuId
                                   || menu.Name == seedMenuName
                                   || (menu.Name != null && menu.Name.StartsWith(batch.Id)))
                    .ToListAsync();
                if (residualMenus.Count > 0)
                {
                    cleanupContext.Menus.RemoveRange(residualMenus);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualLoginLogs = await cleanupContext.LoginLogs
                    .Where(log => log.Username != null
                                  && (residualUsernames.Contains(log.Username)
                                      || log.Username.StartsWith(batch.Id)))
                    .ToListAsync();
                if (residualLoginLogs.Count > 0)
                {
                    cleanupContext.LoginLogs.RemoveRange(residualLoginLogs);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualOperationLogs = await cleanupContext.OperationLogs
                    .Where(log => log.CreateName != null
                                  && (residualUsernames.Contains(log.CreateName)
                                      || log.CreateName.StartsWith(batch.Id)
                                      || log.CreateName == createName))
                    .ToListAsync();
                if (residualOperationLogs.Count > 0)
                {
                    cleanupContext.OperationLogs.RemoveRange(residualOperationLogs);
                    await cleanupContext.SaveChangesAsync();
                }
            }

            await using var residualContext = fixture.CreateDbContext();
            Assert.False(await residualContext.SaleOrders.AnyAsync(item =>
                item.InnerRemark == printedOrderRemark
                || item.InnerRemark == untouchedOrderRemark
                || (item.InnerRemark != null && item.InnerRemark.StartsWith(batch.Id))));
            Assert.False(await residualContext.PrintTemplates.AnyAsync(item =>
                item.TemplateCode == enabledTemplateCode
                || item.TemplateCode == disabledTemplateCode
                || item.Name.StartsWith(batch.Id)));
            Assert.False(await residualContext.Users.AnyAsync(user =>
                user.Username == adminUsername
                || user.Username == limitedUsername
                || (user.Username != null && user.Username.StartsWith(batch.Id))));
            Assert.False(await residualContext.Roles.AnyAsync(role =>
                role.Code == limitedRoleCode
                || (role.Code != null && role.Code.StartsWith(batch.Id))));
            Assert.False(await residualContext.Menus.AnyAsync(menu =>
                menu.Name == seedMenuName
                || (menu.Name != null && menu.Name.StartsWith(batch.Id))));
            Assert.False(await residualContext.LoginLogs.AnyAsync(log =>
                log.Username == adminUsername
                || log.Username == limitedUsername
                || (log.Username != null && log.Username.StartsWith(batch.Id))));
        }
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
