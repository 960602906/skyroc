using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs.Auth;
using Application.DTOs.Customers;
using Application.DTOs.Role;
using Domain.Entities;
using Domain.Entities.Customers;
using Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
using Shared.Common;
using Shared.Constants;
using SkyRoc.Tests.Common;
using Shared.Utils;
using SkyRoc.Tests.Testing.PostgreSql;
using Xunit;

namespace SkyRoc.Tests.Customers;

/// <summary>
///     T4 切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证客户档案 CRUD、标签关系、启停与权限矩阵。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class CustomerCrudPostgreSqlTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     操作员可创建/查询/更新/删除/批量删除客户并维护标签关系与启停；最小权限仅读；未认证与无权限拒绝；扩权/缩权后重新登录收口；临时数据精确清理。
    /// </summary>
    [Fact]
    public async Task Customer_CrudTagsStatusAndPermissionMatrix_OnPostgreSql()
    {
        var batch = TestBatchContext.Create();
        var registry = new BatchCleanupRegistry(batch);

        var limitedRoleId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var limitedUserId = Guid.NewGuid();
        var seedMenuId = Guid.NewGuid();
        var seedCustomerReadButtonId = Guid.NewGuid();
        var writeMenuId = Guid.NewGuid();
        var writeCreateButtonId = Guid.NewGuid();
        var writeUpdateButtonId = Guid.NewGuid();
        var writeDeleteButtonId = Guid.NewGuid();

        // batch.Id 已 40 字符，后缀必须保持 username/role/code ≤ varchar(50)
        var adminUsername = $"{batch.Id}A";
        var limitedUsername = $"{batch.Id}L";
        var limitedRoleCode = $"{batch.Id}R";
        var limitedRoleName = $"{batch.Id}N";
        var seedMenuName = $"{batch.Id}S";
        var writeMenuName = $"{batch.Id}W";
        var targetCustomerCode = $"{batch.Id}C";
        var targetCustomerName = $"{batch.Id}-联调食堂客户";
        var batchCustomerCode = $"{batch.Id}B";
        var batchCustomerName = $"{batch.Id}-批量删除客户";
        var expandedCustomerCode = $"{batch.Id}E";
        var expandedCustomerName = $"{batch.Id}-扩权客户";
        var deniedCustomerCode = $"{batch.Id}D";
        var deniedCustomerName = $"{batch.Id}-拒绝客户";
        var password = "SkyRocCustomerPerm!2026";
        var userAgent = $"SkyRoc-T4-Customer/{batch.Id}";
        var createName = "T4-CustomerCrud";

        var customerReadPermission = PermissionCodes.Business.Customers.Read;
        var customerCreatePermission = PermissionCodes.Business.Customers.Create;
        var customerUpdatePermission = PermissionCodes.Business.Customers.Update;
        var customerDeletePermission = PermissionCodes.Business.Customers.Delete;

        Guid adminRoleId;
        Guid managedCompanyId;
        Guid managedWareId;
        Guid managedTagId;
        Guid managedSecondaryTagId;

        await using (var seedContext = fixture.CreateDbContext())
        {
            // 复用长期联调库中的 Admin 特权角色签发 *:*:*，不创建/删除非受管特权角色
            var adminRole = await seedContext.Roles.AsNoTracking()
                .SingleOrDefaultAsync(role => role.Code == SeedConstants.AdminRoleCode);
            Assert.NotNull(adminRole);
            adminRoleId = adminRole.Id;

            // 复用 T2 受管基础资料，不修改非受管主数据
            var companyCode = DemoDataStableKeyCatalog.Create("COMPANY", 1);
            var wareCode = DemoDataStableKeyCatalog.Create("WARE", 1);
            var tagCode = DemoDataStableKeyCatalog.Create("CUSTOMER-TAG", 1);
            var secondaryTagCode = DemoDataStableKeyCatalog.Create("CUSTOMER-TAG", 2);

            var company = await seedContext.Companies.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == companyCode);
            Assert.NotNull(company);
            managedCompanyId = company.Id;

            var ware = await seedContext.Wares.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == wareCode);
            Assert.NotNull(ware);
            managedWareId = ware.Id;

            var tag = await seedContext.CustomerTags.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == tagCode);
            Assert.NotNull(tag);
            managedTagId = tag.Id;

            var secondaryTag = await seedContext.CustomerTags.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == secondaryTagCode);
            Assert.NotNull(secondaryTag);
            managedSecondaryTagId = secondaryTag.Id;

            await seedContext.Roles.AddAsync(new Role
            {
                Id = limitedRoleId,
                Code = limitedRoleCode,
                Name = limitedRoleName,
                Desc = "T4 客户权限最小权限临时角色，仅本轮自动测试使用",
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.Users.AddRangeAsync(
                new User
                {
                    Id = adminUserId,
                    Username = adminUsername,
                    NickName = "T4客户操作员",
                    Gender = GenderType.Male,
                    Phone = "13900008901",
                    Email = $"{batch.Id}-a@skyroc-autotest.example",
                    PasswordHash = PasswordHasher.Hash(password),
                    Status = Status.Enable,
                    CreateName = createName
                },
                new User
                {
                    Id = limitedUserId,
                    Username = limitedUsername,
                    NickName = "T4客户只读用户",
                    Gender = GenderType.Female,
                    Phone = "13900008902",
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

            await seedContext.Menus.AddRangeAsync(
                new Menu
                {
                    Id = seedMenuId,
                    Name = seedMenuName,
                    Path = $"/{batch.Id}s",
                    Title = "T4客户只读菜单",
                    Component = "page.t4.customer.seed",
                    MenuType = MenuType.Menu,
                    Order = 9611,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new Menu
                {
                    Id = writeMenuId,
                    Name = writeMenuName,
                    Path = $"/{batch.Id}w",
                    Title = "T4客户写权限菜单",
                    Component = "page.t4.customer.write",
                    MenuType = MenuType.Menu,
                    Order = 9612,
                    Status = Status.Enable,
                    CreateName = createName
                });

            await seedContext.MenuButtons.AddRangeAsync(
                new MenuButton
                {
                    Id = seedCustomerReadButtonId,
                    Code = customerReadPermission,
                    Desc = "T4 客户读取权限按钮",
                    MenuId = seedMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeCreateButtonId,
                    Code = customerCreatePermission,
                    Desc = "T4 客户创建权限按钮",
                    MenuId = writeMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeUpdateButtonId,
                    Code = customerUpdatePermission,
                    Desc = "T4 客户更新权限按钮",
                    MenuId = writeMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeDeleteButtonId,
                    Code = customerDeletePermission,
                    Desc = "T4 客户删除权限按钮",
                    MenuId = writeMenuId,
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
        registry.Register<Menu>(writeMenuId, nameof(Menu.Name), writeMenuName);

        Guid? targetCustomerId = null;
        Guid? batchCustomerId = null;
        Guid? expandedCustomerId = null;

        try
        {
            using var factory = fixture.CreateWebApplicationFactory();
            using var anonymousClient = factory.CreateClient();
            anonymousClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            // 未认证访问客户接口
            using (var anonymousList = await anonymousClient.GetAsync("/api/customers"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousList, ResponseCode.Unauthorized);
            }

            using (var anonymousCreate = await anonymousClient.PostAsJsonAsync(
                       "/api/customers",
                       new CreateCustomerDto
                       {
                           Code = $"{batch.Id}X",
                           Name = $"{batch.Id}-未认证客户",
                           Status = Status.Enable
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousCreate, ResponseCode.Unauthorized);
            }

            // 操作员登录（Admin → *:*:*）
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

            // 操作员创建目标客户（含公司/默认仓库与多标签关系）
            CustomerDto targetCustomer;
            using (var createTargetResponse = await adminClient.PostAsJsonAsync(
                       "/api/customers",
                       new CreateCustomerDto
                       {
                           Code = targetCustomerCode,
                           Name = targetCustomerName,
                           CompanyId = managedCompanyId,
                           DefaultWareId = managedWareId,
                           ContactName = "张采购",
                           ContactPhone = "13800138001",
                           Address = "上海市浦东新区联调路 88 号食堂",
                           UnifiedSocialCreditCode = "91310000MA1T4TEST1",
                           LegalRepresentative = "李法人",
                           RegisteredCapital = "500 万元人民币",
                           RegistrationStatus = "存续",
                           RegistrationAuthority = "上海市市场监督管理局",
                           RegisteredAddress = "上海市浦东新区注册路 1 号",
                           BusinessScope = "餐饮服务；农副产品销售",
                           InvoiceTitle = targetCustomerName,
                           TaxpayerIdentificationNumber = "91310000MA1T4TEST1",
                           InvoiceAddress = "上海市浦东新区开票路 2 号",
                           InvoicePhone = "021-58880001",
                           BankName = "中国工商银行上海分行",
                           BankAccount = "1001234567890123456",
                           InvoiceReceiverName = "王开票",
                           InvoiceReceiverPhone = "13800138002",
                           InvoiceReceiverAddress = "上海市浦东新区收票路 3 号",
                           InvoiceEmail = $"{batch.Id}-invoice@skyroc-autotest.example",
                           TagIds = [managedTagId, managedSecondaryTagId],
                           Remark = "T4客户CRUD切片目标客户",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createTargetResponse.StatusCode);
                targetCustomer = await ReadApiDataAsync<CustomerDto>(createTargetResponse);
                Assert.Equal(targetCustomerCode, targetCustomer.Code);
                Assert.Equal(targetCustomerName, targetCustomer.Name);
                Assert.Equal(managedCompanyId, targetCustomer.CompanyId);
                Assert.Equal(managedWareId, targetCustomer.DefaultWareId);
                Assert.Equal("张采购", targetCustomer.ContactName);
                Assert.Equal(Status.Enable, targetCustomer.Status);
                Assert.NotNull(targetCustomer.TagIds);
                Assert.Contains(managedTagId, targetCustomer.TagIds!);
                Assert.Contains(managedSecondaryTagId, targetCustomer.TagIds!);
                targetCustomerId = targetCustomer.Id;
                registry.Register<Customer>(targetCustomer.Id, nameof(Customer.Code), targetCustomerCode);
            }

            // 批量删除目标客户
            CustomerDto batchCustomer;
            using (var createBatchTargetResponse = await adminClient.PostAsJsonAsync(
                       "/api/customers",
                       new CreateCustomerDto
                       {
                           Code = batchCustomerCode,
                           Name = batchCustomerName,
                           CompanyId = managedCompanyId,
                           DefaultWareId = managedWareId,
                           ContactName = "赵批量",
                           ContactPhone = "13800138003",
                           Address = "上海市闵行区批量路 1 号",
                           TagIds = [managedTagId],
                           Remark = "T4客户CRUD切片批量删除客户",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createBatchTargetResponse.StatusCode);
                batchCustomer = await ReadApiDataAsync<CustomerDto>(createBatchTargetResponse);
                Assert.Equal(batchCustomerCode, batchCustomer.Code);
                batchCustomerId = batchCustomer.Id;
                registry.Register<Customer>(batchCustomer.Id, nameof(Customer.Code), batchCustomerCode);
            }

            // 详情应返回公司/仓库名称与标签关系
            using (var detailAfterCreate = await adminClient.GetAsync($"/api/customers/{targetCustomer.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, detailAfterCreate.StatusCode);
                var detail = await ReadApiDataAsync<CustomerDto>(detailAfterCreate);
                Assert.Equal(managedCompanyId, detail.CompanyId);
                Assert.False(string.IsNullOrWhiteSpace(detail.CompanyName));
                Assert.Equal(managedWareId, detail.DefaultWareId);
                Assert.False(string.IsNullOrWhiteSpace(detail.DefaultWareName));
                Assert.NotNull(detail.TagIds);
                Assert.Equal(2, detail.TagIds!.Count);
                Assert.Contains(managedTagId, detail.TagIds);
                Assert.Contains(managedSecondaryTagId, detail.TagIds);
            }

            await using (var relationContext = fixture.CreateDbContext())
            {
                var relations = await relationContext.CustomerTagRelations.AsNoTracking()
                    .Where(relation => relation.CustomerId == targetCustomer.Id)
                    .ToListAsync();
                Assert.Equal(2, relations.Count);
                Assert.Contains(relations, relation => relation.CustomerTagId == managedTagId);
                Assert.Contains(relations, relation => relation.CustomerTagId == managedSecondaryTagId);

                var customerEntity = await relationContext.Customers.AsNoTracking()
                    .SingleAsync(item => item.Id == targetCustomer.Id);
                Assert.Equal(managedCompanyId, customerEntity.CompanyId);
                Assert.Equal(managedWareId, customerEntity.DefaultWareId);
                Assert.Equal("T4客户CRUD切片目标客户", customerEntity.Remark);
            }

            // 分页/全量/详情
            using (var listResponse = await adminClient.GetAsync(
                       $"/api/customers/list?current=1&size=20&code={Uri.EscapeDataString(targetCustomerCode)}"))
            {
                Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<CustomerDto>>(listResponse);
                Assert.NotNull(page.Records);
                Assert.Contains(page.Records!, item => item.Id == targetCustomer.Id || item.Code == targetCustomerCode);
            }

            using (var allResponse = await adminClient.GetAsync("/api/customers"))
            {
                Assert.Equal(HttpStatusCode.OK, allResponse.StatusCode);
            }

            // 更新客户业务字段与标签关系（缩为单标签）
            using (var updateResponse = await adminClient.PutAsJsonAsync(
                       "/api/customers",
                       new UpdateCustomerDto
                       {
                           Id = targetCustomer.Id,
                           Code = targetCustomerCode,
                           Name = targetCustomerName,
                           CompanyId = managedCompanyId,
                           DefaultWareId = managedWareId,
                           ContactName = "张采购-更新",
                           ContactPhone = "13800138999",
                           Address = "上海市浦东新区联调路 99 号食堂",
                           UnifiedSocialCreditCode = "91310000MA1T4TEST1",
                           LegalRepresentative = "李法人",
                           RegisteredCapital = "800 万元人民币",
                           RegistrationStatus = "存续",
                           RegistrationAuthority = "上海市市场监督管理局",
                           RegisteredAddress = "上海市浦东新区注册路 1 号",
                           BusinessScope = "餐饮服务；农副产品批发与零售",
                           InvoiceTitle = targetCustomerName,
                           TaxpayerIdentificationNumber = "91310000MA1T4TEST1",
                           InvoiceAddress = "上海市浦东新区开票路 2 号",
                           InvoicePhone = "021-58880001",
                           BankName = "中国工商银行上海分行",
                           BankAccount = "1001234567890123456",
                           InvoiceReceiverName = "王开票",
                           InvoiceReceiverPhone = "13800138002",
                           InvoiceReceiverAddress = "上海市浦东新区收票路 3 号",
                           InvoiceEmail = $"{batch.Id}-invoice@skyroc-autotest.example",
                           TagIds = [managedTagId],
                           Remark = "T4客户CRUD切片目标客户-已更新",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
                var updated = await ReadApiDataAsync<CustomerDto>(updateResponse);
                Assert.Equal("张采购-更新", updated.ContactName);
                Assert.Equal("13800138999", updated.ContactPhone);
                Assert.Equal("T4客户CRUD切片目标客户-已更新", updated.Remark);
                Assert.NotNull(updated.TagIds);
                Assert.Single(updated.TagIds!);
                Assert.Contains(managedTagId, updated.TagIds!);
            }

            await using (var afterUpdate = fixture.CreateDbContext())
            {
                var relations = await afterUpdate.CustomerTagRelations.AsNoTracking()
                    .Where(relation => relation.CustomerId == targetCustomer.Id)
                    .ToListAsync();
                Assert.Single(relations);
                Assert.Equal(managedTagId, relations[0].CustomerTagId);
            }

            // 停用与启用
            using (var disableResponse = await adminClient.PatchAsync(
                       $"/api/customers/{targetCustomer.Id}/status?status={(int)Status.Disable}",
                       null))
            {
                Assert.Equal(HttpStatusCode.OK, disableResponse.StatusCode);
                var disabled = await ReadApiDataAsync<CustomerDto>(disableResponse);
                Assert.Equal(Status.Disable, disabled.Status);
            }

            using (var enableResponse = await adminClient.PatchAsync(
                       $"/api/customers/{targetCustomer.Id}/status?status={(int)Status.Enable}",
                       null))
            {
                Assert.Equal(HttpStatusCode.OK, enableResponse.StatusCode);
                var enabled = await ReadApiDataAsync<CustomerDto>(enableResponse);
                Assert.Equal(Status.Enable, enabled.Status);
            }

            // 操作员批量删除
            using (var batchDeleteResponse = await adminClient.SendAsync(
                       new HttpRequestMessage(HttpMethod.Delete, "/api/customers/batchDelete")
                       {
                           Content = JsonContent.Create(new List<Guid> { batchCustomer.Id })
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, batchDeleteResponse.StatusCode);
            }

            await using (var afterBatchDelete = fixture.CreateDbContext())
            {
                Assert.False(await afterBatchDelete.Customers.AnyAsync(item =>
                    item.Id == batchCustomer.Id || item.Code == batchCustomerCode));
                Assert.False(await afterBatchDelete.CustomerTagRelations.AnyAsync(relation =>
                    relation.CustomerId == batchCustomer.Id));
                batchCustomerId = null;
            }

            // 最小权限用户登录：仅客户读
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

            using (var limitedInfoResponse = await limitedClient.GetAsync("/api/auth/getUserInfo"))
            {
                Assert.Equal(HttpStatusCode.OK, limitedInfoResponse.StatusCode);
                var info = await ReadApiDataAsync<UserInfoDto>(limitedInfoResponse);
                Assert.Equal(limitedUserId, info.UserId);
                Assert.Contains(limitedRoleCode, info.Roles);
                Assert.DoesNotContain(PermissionCodes.All, info.Permissions);
                Assert.Contains(customerReadPermission, info.Permissions);
                Assert.DoesNotContain(customerCreatePermission, info.Permissions);
                Assert.DoesNotContain(customerUpdatePermission, info.Permissions);
                Assert.DoesNotContain(customerDeletePermission, info.Permissions);
            }

            using (var routesResponse = await limitedClient.GetAsync("/api/auth/getRoutes"))
            {
                Assert.Equal(HttpStatusCode.OK, routesResponse.StatusCode);
                var routes = await ReadApiDataAsync<GetRoutesResDto>(routesResponse);
                var paths = FlattenRoutePaths(routes.Routes ?? []).ToList();
                Assert.Contains($"/{batch.Id}s", paths);
                Assert.DoesNotContain($"/{batch.Id}w", paths);
            }

            using (var allowedAll = await limitedClient.GetAsync("/api/customers"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedAll.StatusCode);
            }

            using (var allowedDetail = await limitedClient.GetAsync($"/api/customers/{targetCustomer.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedDetail.StatusCode);
            }

            using (var deniedCreate = await limitedClient.PostAsJsonAsync(
                       "/api/customers",
                       new CreateCustomerDto
                       {
                           Code = deniedCustomerCode,
                           Name = deniedCustomerName,
                           Status = Status.Enable
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedCreate, ResponseCode.Forbidden);
            }

            using (var deniedUpdate = await limitedClient.PutAsJsonAsync(
                       "/api/customers",
                       new UpdateCustomerDto
                       {
                           Id = targetCustomer.Id,
                           Code = targetCustomerCode,
                           Name = targetCustomerName,
                           Status = Status.Enable
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedUpdate, ResponseCode.Forbidden);
            }

            using (var deniedStatus = await limitedClient.PatchAsync(
                       $"/api/customers/{targetCustomer.Id}/status?status={(int)Status.Disable}",
                       null))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedStatus, ResponseCode.Forbidden);
            }

            using (var deniedDelete = await limitedClient.DeleteAsync($"/api/customers/{targetCustomer.Id}"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedDelete, ResponseCode.Forbidden);
            }

            // 扩权：分配写权限菜单后重新登录
            using (var expandMenus = await adminClient.PostAsJsonAsync(
                       "/api/roles/assignMenus",
                       new AssignMenusDto
                       {
                           RoleId = limitedRoleId,
                           MenuIds = [seedMenuId, writeMenuId]
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, expandMenus.StatusCode);
            }

            LoginResDto limitedExpandedLogin;
            using (var expandLoginResponse = await anonymousClient.PostAsJsonAsync("/api/auth/login", new LoginReqDto
            {
                Username = limitedUsername,
                Password = password
            }))
            {
                Assert.Equal(HttpStatusCode.OK, expandLoginResponse.StatusCode);
                limitedExpandedLogin = await ReadApiDataAsync<LoginResDto>(expandLoginResponse);
            }

            using var limitedWriteClient = factory.CreateClient();
            limitedWriteClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.BearerScheme, limitedExpandedLogin.Token);
            limitedWriteClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            using (var infoAfterExpand = await limitedWriteClient.GetAsync("/api/auth/getUserInfo"))
            {
                Assert.Equal(HttpStatusCode.OK, infoAfterExpand.StatusCode);
                var info = await ReadApiDataAsync<UserInfoDto>(infoAfterExpand);
                Assert.Contains(customerReadPermission, info.Permissions);
                Assert.Contains(customerCreatePermission, info.Permissions);
                Assert.Contains(customerUpdatePermission, info.Permissions);
                Assert.Contains(customerDeletePermission, info.Permissions);
            }

            CustomerDto expandedCustomer;
            using (var createExpanded = await limitedWriteClient.PostAsJsonAsync(
                       "/api/customers",
                       new CreateCustomerDto
                       {
                           Code = expandedCustomerCode,
                           Name = expandedCustomerName,
                           CompanyId = managedCompanyId,
                           DefaultWareId = managedWareId,
                           ContactName = "扩权联系人",
                           ContactPhone = "13800138111",
                           Address = "上海市徐汇区扩权路 1 号",
                           TagIds = [managedSecondaryTagId],
                           Remark = "T4扩权客户",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createExpanded.StatusCode);
                expandedCustomer = await ReadApiDataAsync<CustomerDto>(createExpanded);
                Assert.Equal(expandedCustomerCode, expandedCustomer.Code);
                expandedCustomerId = expandedCustomer.Id;
                registry.Register<Customer>(expandedCustomer.Id, nameof(Customer.Code), expandedCustomerCode);
            }

            using (var updateExpanded = await limitedWriteClient.PutAsJsonAsync(
                       "/api/customers",
                       new UpdateCustomerDto
                       {
                           Id = expandedCustomer.Id,
                           Code = expandedCustomerCode,
                           Name = expandedCustomerName,
                           CompanyId = managedCompanyId,
                           DefaultWareId = managedWareId,
                           ContactName = "扩权联系人-已更新",
                           ContactPhone = "13800138112",
                           Address = "上海市徐汇区扩权路 2 号",
                           TagIds = [managedSecondaryTagId],
                           Remark = "T4扩权客户-已更新",
                           Status = Status.Enable
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, updateExpanded.StatusCode);
            }

            using (var deleteExpanded = await limitedWriteClient.DeleteAsync($"/api/customers/{expandedCustomer.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, deleteExpanded.StatusCode);
            }

            await using (var afterExpandedDelete = fixture.CreateDbContext())
            {
                Assert.False(await afterExpandedDelete.Customers.AnyAsync(item =>
                    item.Id == expandedCustomer.Id || item.Code == expandedCustomerCode));
                Assert.False(await afterExpandedDelete.CustomerTagRelations.AnyAsync(relation =>
                    relation.CustomerId == expandedCustomer.Id));
                expandedCustomerId = null;
            }

            // 缩权后写权限与写菜单路径收口
            using (var shrinkMenus = await adminClient.PostAsJsonAsync(
                       "/api/roles/assignMenus",
                       new AssignMenusDto
                       {
                           RoleId = limitedRoleId,
                           MenuIds = [seedMenuId]
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, shrinkMenus.StatusCode);
            }

            LoginResDto limitedRelogin;
            using (var reloginResponse = await anonymousClient.PostAsJsonAsync("/api/auth/login", new LoginReqDto
            {
                Username = limitedUsername,
                Password = password
            }))
            {
                Assert.Equal(HttpStatusCode.OK, reloginResponse.StatusCode);
                limitedRelogin = await ReadApiDataAsync<LoginResDto>(reloginResponse);
            }

            using var limitedReloginClient = factory.CreateClient();
            limitedReloginClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.BearerScheme, limitedRelogin.Token);
            limitedReloginClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            using (var infoAfterShrink = await limitedReloginClient.GetAsync("/api/auth/getUserInfo"))
            {
                Assert.Equal(HttpStatusCode.OK, infoAfterShrink.StatusCode);
                var info = await ReadApiDataAsync<UserInfoDto>(infoAfterShrink);
                Assert.Contains(customerReadPermission, info.Permissions);
                Assert.DoesNotContain(customerCreatePermission, info.Permissions);
                Assert.DoesNotContain(customerUpdatePermission, info.Permissions);
                Assert.DoesNotContain(customerDeletePermission, info.Permissions);
            }

            using (var routesAfterShrink = await limitedReloginClient.GetAsync("/api/auth/getRoutes"))
            {
                Assert.Equal(HttpStatusCode.OK, routesAfterShrink.StatusCode);
                var routes = await ReadApiDataAsync<GetRoutesResDto>(routesAfterShrink);
                var paths = FlattenRoutePaths(routes.Routes ?? []).ToList();
                Assert.Contains($"/{batch.Id}s", paths);
                Assert.DoesNotContain($"/{batch.Id}w", paths);
            }

            using (var deniedCreateAfterShrink = await limitedReloginClient.PostAsJsonAsync(
                       "/api/customers",
                       new CreateCustomerDto
                       {
                           Code = $"{batch.Id}F",
                           Name = $"{batch.Id}-缩权拒绝客户",
                           Status = Status.Enable
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedCreateAfterShrink, ResponseCode.Forbidden);
            }

            // 删除目标客户（标签关系随客户清理）
            using (var deleteTarget = await adminClient.DeleteAsync($"/api/customers/{targetCustomer.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, deleteTarget.StatusCode);
            }

            await using (var afterDeleteTarget = fixture.CreateDbContext())
            {
                Assert.False(await afterDeleteTarget.Customers.AnyAsync(item =>
                    item.Id == targetCustomer.Id || item.Code == targetCustomerCode));
                Assert.False(await afterDeleteTarget.CustomerTagRelations.AnyAsync(relation =>
                    relation.CustomerId == targetCustomer.Id));
                targetCustomerId = null;
            }

            await using var auditContext = fixture.CreateDbContext();
            var loginLogs = await auditContext.LoginLogs.AsNoTracking()
                .Where(log => log.Username == adminUsername || log.Username == limitedUsername)
                .ToListAsync();
            Assert.Contains(loginLogs, log => log.Username == adminUsername && log.IsSuccess);
            Assert.Contains(loginLogs, log => log.Username == limitedUsername && log.IsSuccess);
            Assert.All(loginLogs, log =>
            {
                Assert.DoesNotContain(password, log.FailureReason ?? string.Empty, StringComparison.Ordinal);
            });
            RegisterLoginLogs(registry, loginLogs);
            await RegisterBatchOperationLogsAsync(fixture, registry, adminUsername, limitedUsername);
        }
        finally
        {
            await RegisterResidualLoginLogsAsync(
                fixture,
                registry,
                adminUsername,
                limitedUsername);
            await RegisterBatchOperationLogsAsync(
                fixture,
                registry,
                adminUsername,
                limitedUsername);

            // 先清本轮批次登记实体；UserRole 随用户级联，RoleMenu 随角色/菜单级联；标签关系随客户清理
            await fixture.CleanupBatchAsync(registry);
            await fixture.CleanupBatchAsync(registry);

            await using (var cleanupContext = fixture.CreateDbContext())
            {
                var residualUsernames = new[]
                {
                    adminUsername,
                    limitedUsername
                };

                var residualUserIds = new List<Guid> { adminUserId, limitedUserId };

                // 兜底清理可能残留的客户与标签关系
                var residualCustomerCodes = new List<string>
                {
                    targetCustomerCode,
                    batchCustomerCode,
                    expandedCustomerCode,
                    deniedCustomerCode,
                    $"{batch.Id}X",
                    $"{batch.Id}F"
                };
                var residualCustomerIds = new List<Guid>();
                if (targetCustomerId.HasValue)
                    residualCustomerIds.Add(targetCustomerId.Value);
                if (batchCustomerId.HasValue)
                    residualCustomerIds.Add(batchCustomerId.Value);
                if (expandedCustomerId.HasValue)
                    residualCustomerIds.Add(expandedCustomerId.Value);

                var residualCustomers = await cleanupContext.Customers
                    .Where(item => residualCustomerIds.Contains(item.Id)
                                   || residualCustomerCodes.Contains(item.Code)
                                   || (item.Code != null && item.Code.StartsWith(batch.Id)))
                    .ToListAsync();
                if (residualCustomers.Count > 0)
                {
                    residualCustomerIds = residualCustomers.Select(item => item.Id).Distinct().ToList();
                    var residualRelations = await cleanupContext.CustomerTagRelations
                        .Where(relation => residualCustomerIds.Contains(relation.CustomerId))
                        .ToListAsync();
                    if (residualRelations.Count > 0)
                    {
                        cleanupContext.CustomerTagRelations.RemoveRange(residualRelations);
                        await cleanupContext.SaveChangesAsync();
                    }

                    cleanupContext.Customers.RemoveRange(residualCustomers);
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

                var residualRoleCodes = new List<string>
                {
                    limitedRoleCode
                };
                var residualRoleIds = new List<Guid> { limitedRoleId };

                var residualRoleMenus = await cleanupContext.RoleMenus
                    .Where(relation => residualRoleIds.Contains(relation.RoleId)
                                       || relation.MenuId == seedMenuId
                                       || relation.MenuId == writeMenuId)
                    .ToListAsync();
                if (residualRoleMenus.Count > 0)
                {
                    cleanupContext.RoleMenus.RemoveRange(residualRoleMenus);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualRoles = await cleanupContext.Roles
                    .Where(role => residualRoleIds.Contains(role.Id)
                                   || residualRoleCodes.Contains(role.Code)
                                   || (role.Code != null && role.Code.StartsWith(batch.Id)))
                    .ToListAsync();
                if (residualRoles.Count > 0)
                {
                    cleanupContext.Roles.RemoveRange(residualRoles);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualButtonIds = new List<Guid>
                {
                    seedCustomerReadButtonId,
                    writeCreateButtonId,
                    writeUpdateButtonId,
                    writeDeleteButtonId
                };
                var residualButtons = await cleanupContext.MenuButtons
                    .Where(button => residualButtonIds.Contains(button.Id)
                                     || button.MenuId == seedMenuId
                                     || button.MenuId == writeMenuId
                                     || button.CreateName == createName)
                    .ToListAsync();
                if (residualButtons.Count > 0)
                {
                    cleanupContext.MenuButtons.RemoveRange(residualButtons);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualMenus = await cleanupContext.Menus
                    .Where(menu => menu.Id == seedMenuId
                                   || menu.Id == writeMenuId
                                   || menu.Name == seedMenuName
                                   || menu.Name == writeMenuName)
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
                || (user.Username != null && user.Username.StartsWith(batch.Id))));
            Assert.False(await residualContext.Roles.AnyAsync(role =>
                role.Id == limitedRoleId
                || role.Code == limitedRoleCode
                || (role.Code != null && role.Code.StartsWith(batch.Id))));
            Assert.False(await residualContext.Menus.AnyAsync(menu =>
                menu.Id == seedMenuId
                || menu.Id == writeMenuId
                || menu.Name == seedMenuName
                || menu.Name == writeMenuName));
            Assert.False(await residualContext.MenuButtons.AnyAsync(button =>
                button.Id == seedCustomerReadButtonId
                || button.Id == writeCreateButtonId
                || button.Id == writeUpdateButtonId
                || button.Id == writeDeleteButtonId
                || button.CreateName == createName));
            Assert.False(await residualContext.UserRoles.AnyAsync(relation =>
                relation.UserId == adminUserId || relation.UserId == limitedUserId));
            Assert.False(await residualContext.RoleMenus.AnyAsync(relation =>
                relation.RoleId == limitedRoleId
                || relation.MenuId == seedMenuId
                || relation.MenuId == writeMenuId));
            Assert.False(await residualContext.Customers.AnyAsync(item =>
                item.Code == targetCustomerCode
                || item.Code == batchCustomerCode
                || item.Code == expandedCustomerCode
                || (item.Code != null && item.Code.StartsWith(batch.Id))));
            Assert.False(await residualContext.LoginLogs.AnyAsync(log =>
                log.Username == adminUsername
                || log.Username == limitedUsername
                || (log.Username != null && log.Username.StartsWith(batch.Id))));
            Assert.False(await residualContext.OperationLogs.AnyAsync(log =>
                log.CreateName == adminUsername
                || log.CreateName == limitedUsername
                || (log.CreateName != null && log.CreateName.StartsWith(batch.Id))));
            // 既有 Admin 角色与受管主数据必须保留
            Assert.True(await residualContext.Roles.AnyAsync(role =>
                role.Id == adminRoleId && role.Code == SeedConstants.AdminRoleCode));
            Assert.True(await residualContext.Companies.AnyAsync(item => item.Id == managedCompanyId));
            Assert.True(await residualContext.Wares.AnyAsync(item => item.Id == managedWareId));
            Assert.True(await residualContext.CustomerTags.AnyAsync(item => item.Id == managedTagId));
            Assert.True(await residualContext.CustomerTags.AnyAsync(item => item.Id == managedSecondaryTagId));
        }
    }

    private static IEnumerable<string> FlattenRoutePaths(IEnumerable<RoutesDto> routes)
    {
        foreach (var route in routes)
        {
            if (!string.IsNullOrWhiteSpace(route.Path))
                yield return route.Path!;
            if (route.Children is null)
                continue;
            foreach (var childPath in FlattenRoutePaths(route.Children))
                yield return childPath;
        }
    }

    private static void RegisterLoginLogs(BatchCleanupRegistry registry, IEnumerable<LoginLog> loginLogs)
    {
        foreach (var log in loginLogs)
        {
            try
            {
                registry.Register<LoginLog>(log.Id, nameof(LoginLog.Username), log.Username);
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
