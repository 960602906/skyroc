using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application.DTOs.Auth;
using Application.DTOs.Orders;
using Application.DTOs.Purchases;
using Application.DTOs.Role;
using Domain.Entities;
using Domain.Entities.Orders;
using Domain.Entities.Purchases;
using Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
using Shared.Common;
using Shared.Constants;
using SkyRoc.Tests.Common;
using Shared.Utils;
using SkyRoc.Tests.Testing.PostgreSql;
using Xunit;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace SkyRoc.Tests.Purchases;

/// <summary>
///     T6 切片：在专用 PostgreSQL 与真实 HTTP 宿主下验证采购计划生成/分配/合并拆分守恒、
///     采购单完成与取消回写，以及采购权限矩阵。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class PurchasePlanFlowAndPermissionMatrixPostgreSqlTests(PostgreSqlTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     操作员可从已审核订单生成计划、分配供采、合并拆分数量守恒、生成采购单并完成/取消回写；
    ///     非法状态业务拒绝；最小权限仅读；写拒绝；扩权/缩权后重新登录收口；临时数据精确清理。
    /// </summary>
    [Fact]
    public async Task PurchasePlan_GenerateAssignMergeSplitGenerateOrderCompleteCancelAndPermissionMatrix_OnPostgreSql()
    {
        var batch = TestBatchContext.Create();
        var registry = new BatchCleanupRegistry(batch);

        var limitedRoleId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var limitedUserId = Guid.NewGuid();
        var seedMenuId = Guid.NewGuid();
        var seedPurchaseReadButtonId = Guid.NewGuid();
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
        var order1InnerRemark = $"{batch.Id}O1";
        var order2InnerRemark = $"{batch.Id}O2";
        var planGenerateRemark = $"{batch.Id}PG";
        var planMergeRemark = $"{batch.Id}PM";
        var planSplitRemark = $"{batch.Id}PS";
        var poCompleteRemark = $"{batch.Id}PC";
        var poCancelRemark = $"{batch.Id}PX";
        var expandedPlanRemark = $"{batch.Id}PE";
        var password = "SkyRocPurchaseFlowPerm!2026";
        var userAgent = $"SkyRoc-T6-PurchaseFlow/{batch.Id}";
        var createName = "T6-PurchasePlanFlow";

        var purchaseReadPermission = PermissionCodes.Business.Purchases.Read;
        var purchaseCreatePermission = PermissionCodes.Business.Purchases.Create;
        var purchaseUpdatePermission = PermissionCodes.Business.Purchases.Update;
        var purchaseDeletePermission = PermissionCodes.Business.Purchases.Delete;

        var orderQuantity = NumericPrecision.RoundQuantity(2m);
        var orderFixedPrice = NumericPrecision.RoundMoney(12.5m);

        Guid adminRoleId;
        Guid managedCustomerId;
        Guid managedGoodsId;
        string managedGoodsCode = null!;
        string managedGoodsName = null!;
        Guid managedGoodsUnitId;
        decimal managedUnitConversion;
        Guid managedWareId;
        Guid managedSupplierId;
        string managedSupplierName = null!;
        Guid managedPurchaserId;
        string managedPurchaserName = null!;
        decimal expectedBaseQuantity;

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
            var supplierCode = DemoDataStableKeyCatalog.Create("SUPPLIER", 1);
            var purchaserCode = DemoDataStableKeyCatalog.Create("PURCHASER", 1);

            var customer = await seedContext.Customers.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == customerCode);
            Assert.NotNull(customer);
            managedCustomerId = customer.Id;

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
            managedUnitConversion = goodsUnit.ConversionRate;
            Assert.True(managedUnitConversion > 0);
            expectedBaseQuantity = NumericPrecision.RoundQuantity(orderQuantity * managedUnitConversion);

            var ware = await seedContext.Wares.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == wareCode);
            Assert.NotNull(ware);
            managedWareId = ware.Id;

            var supplier = await seedContext.Suppliers.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == supplierCode);
            Assert.NotNull(supplier);
            managedSupplierId = supplier.Id;
            managedSupplierName = supplier.Name;

            var purchaser = await seedContext.Purchasers.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == purchaserCode);
            Assert.NotNull(purchaser);
            managedPurchaserId = purchaser.Id;
            managedPurchaserName = purchaser.Name;

            await seedContext.Roles.AddAsync(new Role
            {
                Id = limitedRoleId,
                Code = limitedRoleCode,
                Name = limitedRoleName,
                Desc = "T6 采购计划权限最小权限临时角色，仅本轮自动测试使用",
                Status = Status.Enable,
                CreateName = createName
            });

            await seedContext.Users.AddRangeAsync(
                new User
                {
                    Id = adminUserId,
                    Username = adminUsername,
                    NickName = "T6采购操作员",
                    Gender = GenderType.Male,
                    Phone = "13900009601",
                    Email = $"{batch.Id}-a@skyroc-autotest.example",
                    PasswordHash = PasswordHasher.Hash(password),
                    Status = Status.Enable,
                    CreateName = createName
                },
                new User
                {
                    Id = limitedUserId,
                    Username = limitedUsername,
                    NickName = "T6采购只读用户",
                    Gender = GenderType.Female,
                    Phone = "13900009602",
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
                    Title = "T6采购只读菜单",
                    Component = "page.t6.purchase.seed",
                    MenuType = MenuType.Menu,
                    Order = 9601,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new Menu
                {
                    Id = writeMenuId,
                    Name = writeMenuName,
                    Path = $"/{batch.Id}w",
                    Title = "T6采购写权限菜单",
                    Component = "page.t6.purchase.write",
                    MenuType = MenuType.Menu,
                    Order = 9602,
                    Status = Status.Enable,
                    CreateName = createName
                });

            await seedContext.MenuButtons.AddRangeAsync(
                new MenuButton
                {
                    Id = seedPurchaseReadButtonId,
                    Code = purchaseReadPermission,
                    Desc = "T6 采购读取权限按钮",
                    MenuId = seedMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeCreateButtonId,
                    Code = purchaseCreatePermission,
                    Desc = "T6 采购创建权限按钮",
                    MenuId = writeMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeUpdateButtonId,
                    Code = purchaseUpdatePermission,
                    Desc = "T6 采购更新权限按钮",
                    MenuId = writeMenuId,
                    Menu = null!,
                    Status = Status.Enable,
                    CreateName = createName
                },
                new MenuButton
                {
                    Id = writeDeleteButtonId,
                    Code = purchaseDeletePermission,
                    Desc = "T6 采购删除权限按钮",
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

        Guid? order1Id = null;
        Guid? order2Id = null;
        Guid? mergedPlanId = null;
        Guid? splitPlanId = null;
        Guid? remainingPlanId = null;
        Guid? completePoId = null;
        Guid? cancelPoId = null;
        Guid? expandedPlanId = null;

        try
        {
            using var factory = fixture.CreateWebApplicationFactory();
            using var anonymousClient = factory.CreateClient();
            anonymousClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            // 未认证访问采购计划/采购单接口
            using (var anonymousPlanList = await anonymousClient.GetAsync("/api/purchase-plans/list?current=1&size=10"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousPlanList, ResponseCode.Unauthorized);
            }

            using (var anonymousOrderList = await anonymousClient.GetAsync("/api/purchase-orders/list?current=1&size=10"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousOrderList, ResponseCode.Unauthorized);
            }

            using (var anonymousGenerate = await anonymousClient.PostAsJsonAsync(
                       "/api/purchase-plans/generate",
                       new GeneratePurchasePlanFromOrdersDto { OrderIds = [Guid.NewGuid()] }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(anonymousGenerate, ResponseCode.Unauthorized);
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

            // 创建两张已审核销售订单作为计划来源（日期带 Z 规避 timestamptz 写入失败）
            var order1 = await CreateAndApproveSaleOrderAsync(
                adminClient,
                managedCustomerId,
                managedWareId,
                managedGoodsId,
                managedGoodsUnitId,
                orderQuantity,
                orderFixedPrice,
                order1InnerRemark,
                "2026-07-17T08:00:00Z",
                "T6采购计划切片订单甲");
            order1Id = order1.Id;
            registry.Register<SaleOrder>(order1.Id, nameof(SaleOrder.InnerRemark), order1InnerRemark);
            Assert.Equal(expectedBaseQuantity, Assert.Single(order1.Details).BaseQuantity);

            var order2 = await CreateAndApproveSaleOrderAsync(
                adminClient,
                managedCustomerId,
                managedWareId,
                managedGoodsId,
                managedGoodsUnitId,
                orderQuantity,
                orderFixedPrice,
                order2InnerRemark,
                "2026-07-17T08:10:00Z",
                "T6采购计划切片订单乙");
            order2Id = order2.Id;
            registry.Register<SaleOrder>(order2.Id, nameof(SaleOrder.InnerRemark), order2InnerRemark);

            // 非法：未审核订单生成计划
            SaleOrderDto draftOrder;
            using (var createDraft = await adminClient.PostAsJsonAsync(
                       "/api/orders",
                       new
                       {
                           customerId = managedCustomerId,
                           wareId = managedWareId,
                           orderDate = "2026-07-17T08:20:00Z",
                           contactName = "草稿客户",
                           contactPhone = "13800139603",
                           deliveryAddress = "上海市草稿路 1 号",
                           remark = "T6未审核拒绝生成计划",
                           innerRemark = $"{batch.Id}OD",
                           details = new[]
                           {
                               new
                               {
                                   goodsId = managedGoodsId,
                                   goodsUnitId = managedGoodsUnitId,
                                   quantity = orderQuantity,
                                   fixedPrice = orderFixedPrice,
                                   fixedGoodsUnitId = managedGoodsUnitId
                               }
                           }
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createDraft.StatusCode);
                draftOrder = await ReadApiDataAsync<SaleOrderDto>(createDraft);
                registry.Register<SaleOrder>(draftOrder.Id, nameof(SaleOrder.InnerRemark), $"{batch.Id}OD");
            }

            using (var illegalUnapproved = await adminClient.PostAsJsonAsync(
                       "/api/purchase-plans/generate",
                       new GeneratePurchasePlanFromOrdersDto
                       {
                           OrderIds = [draftOrder.Id],
                           Remark = planGenerateRemark
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(illegalUnapproved, ResponseCode.DatabaseError);
            }

            // 从两张已审核订单生成采购计划
            List<PurchasePlanDto> generatedPlans;
            using (var generateResponse = await adminClient.PostAsJsonAsync(
                       "/api/purchase-plans/generate",
                       new GeneratePurchasePlanFromOrdersDto
                       {
                           OrderIds = [order1.Id, order2.Id],
                           Remark = planGenerateRemark
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, generateResponse.StatusCode);
                generatedPlans = await ReadApiDataAsync<List<PurchasePlanDto>>(generateResponse);
                Assert.Equal(2, generatedPlans.Count);
            }

            foreach (var plan in generatedPlans)
            {
                registry.Register<PurchasePlan>(plan.Id, nameof(PurchasePlan.Remark), planGenerateRemark);
                Assert.StartsWith("PP", plan.PlanNo);
                Assert.Equal(PurchasePlanStatus.Unpublished, plan.PurchaseStatus);
                Assert.Equal(planGenerateRemark, plan.Remark);
                var detail = Assert.Single(plan.Details);
                Assert.Equal(managedGoodsId, detail.GoodsId);
                Assert.Equal(managedGoodsName, detail.GoodsName);
                Assert.Equal(managedGoodsCode, detail.GoodsCode);
                Assert.Equal(expectedBaseQuantity, detail.RequiredQuantity);
                Assert.Equal(expectedBaseQuantity, detail.PlannedQuantity);
                Assert.Equal(0m, detail.PurchasedQuantity);
                var relation = Assert.Single(detail.OrderRelations);
                Assert.True(relation.SaleOrderId == order1.Id || relation.SaleOrderId == order2.Id);
                Assert.Equal(expectedBaseQuantity, relation.RequiredQuantity);
            }

            // 订单侧回写采购计划标记
            await using (var afterGenerate = fixture.CreateDbContext())
            {
                var reloadedOrder1 = await afterGenerate.SaleOrders.AsNoTracking()
                    .Include(item => item.Details)
                    .SingleAsync(item => item.Id == order1.Id);
                Assert.True(reloadedOrder1.HasPurchasePlan);
                Assert.All(reloadedOrder1.Details, detail => Assert.True(detail.HasPurchasePlan));

                var reloadedOrder2 = await afterGenerate.SaleOrders.AsNoTracking()
                    .Include(item => item.Details)
                    .SingleAsync(item => item.Id == order2.Id);
                Assert.True(reloadedOrder2.HasPurchasePlan);
                Assert.All(reloadedOrder2.Details, detail => Assert.True(detail.HasPurchasePlan));

                Assert.Equal(2, await afterGenerate.PurchasePlans.CountAsync(item =>
                    item.Remark == planGenerateRemark));
                Assert.Equal(2, await afterGenerate.PurchasePlanOrderRelations.CountAsync(relation =>
                    relation.SaleOrderId == order1.Id || relation.SaleOrderId == order2.Id));
            }

            // 非法：同一订单重复生成计划
            using (var illegalDuplicate = await adminClient.PostAsJsonAsync(
                       "/api/purchase-plans/generate",
                       new GeneratePurchasePlanFromOrdersDto
                       {
                           OrderIds = [order1.Id],
                           Remark = planGenerateRemark
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(illegalDuplicate, ResponseCode.DatabaseError);
            }

            // 分页筛选 + 详情
            var firstPlan = generatedPlans[0];
            using (var listResponse = await adminClient.GetAsync(
                       $"/api/purchase-plans/list?current=1&size=20&keyword={Uri.EscapeDataString(firstPlan.PlanNo)}"))
            {
                Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<PurchasePlanDto>>(listResponse);
                Assert.NotNull(page.Records);
                Assert.Contains(page.Records!, item => item.Id == firstPlan.Id);
            }

            using (var detailResponse = await adminClient.GetAsync($"/api/purchase-plans/{firstPlan.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
                var detail = await ReadApiDataAsync<PurchasePlanDto>(detailResponse);
                Assert.Equal(firstPlan.PlanNo, detail.PlanNo);
                Assert.Single(detail.Details);
            }

            var planIds = generatedPlans.Select(plan => plan.Id).ToList();

            // 分配供应商与采购员
            using (var assignSupplier = await adminClient.PutAsJsonAsync(
                       "/api/purchase-plans/supplier",
                       new AssignPurchasePlanSupplierDto
                       {
                           PlanIds = planIds,
                           SupplierId = managedSupplierId
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, assignSupplier.StatusCode);
                var assigned = await ReadApiDataAsync<List<PurchasePlanDto>>(assignSupplier);
                Assert.Equal(2, assigned.Count);
                Assert.All(assigned, plan =>
                {
                    Assert.Equal(managedSupplierId, plan.SupplierId);
                    Assert.Equal(managedSupplierName, plan.SupplierName);
                });
            }

            using (var assignPurchaser = await adminClient.PutAsJsonAsync(
                       "/api/purchase-plans/purchaser",
                       new AssignPurchasePlanPurchaserDto
                       {
                           PlanIds = planIds,
                           PurchaserId = managedPurchaserId
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, assignPurchaser.StatusCode);
                var assigned = await ReadApiDataAsync<List<PurchasePlanDto>>(assignPurchaser);
                Assert.All(assigned, plan =>
                {
                    Assert.Equal(managedPurchaserId, plan.PurchaserId);
                    Assert.Equal(managedPurchaserName, plan.PurchaserName);
                });
            }

            // 合并：数量守恒 = 两单基础数量之和
            PurchasePlanDto mergedPlan;
            using (var mergeResponse = await adminClient.PostAsJsonAsync(
                       "/api/purchase-plans/merge",
                       new MergePurchasePlansDto
                       {
                           PlanIds = planIds,
                           Remark = planMergeRemark
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, mergeResponse.StatusCode);
                mergedPlan = await ReadApiDataAsync<PurchasePlanDto>(mergeResponse);
                Assert.DoesNotContain(mergedPlan.Id, planIds);
                Assert.Equal(planMergeRemark, mergedPlan.Remark);
                Assert.Equal(managedSupplierId, mergedPlan.SupplierId);
                Assert.Equal(managedPurchaserId, mergedPlan.PurchaserId);
                Assert.Equal(PurchasePlanStatus.Unpublished, mergedPlan.PurchaseStatus);
                var mergedDetail = Assert.Single(mergedPlan.Details);
                var expectedMergedQuantity = NumericPrecision.RoundQuantity(expectedBaseQuantity * 2m);
                Assert.Equal(expectedMergedQuantity, mergedDetail.RequiredQuantity);
                Assert.Equal(expectedMergedQuantity, mergedDetail.PlannedQuantity);
                Assert.Equal(0m, mergedDetail.PurchasedQuantity);
                Assert.Equal(2, mergedDetail.OrderRelations.Count);
                Assert.Contains(mergedDetail.OrderRelations, relation => relation.SaleOrderId == order1.Id);
                Assert.Contains(mergedDetail.OrderRelations, relation => relation.SaleOrderId == order2.Id);
                mergedPlanId = mergedPlan.Id;
                registry.Register<PurchasePlan>(mergedPlan.Id, nameof(PurchasePlan.Remark), planMergeRemark);
            }

            await using (var afterMerge = fixture.CreateDbContext())
            {
                // 来源计划已删除，合并计划存在
                Assert.False(await afterMerge.PurchasePlans.AnyAsync(item => planIds.Contains(item.Id)));
                Assert.True(await afterMerge.PurchasePlans.AnyAsync(item => item.Id == mergedPlan.Id));
                var relationCount = await afterMerge.PurchasePlanOrderRelations.CountAsync(relation =>
                    relation.SaleOrderId == order1.Id || relation.SaleOrderId == order2.Id);
                Assert.Equal(2, relationCount);
            }

            // 按订单拆分：守恒拆出订单甲
            PurchasePlanDto splitPlan;
            using (var splitResponse = await adminClient.PostAsJsonAsync(
                       "/api/purchase-plans/split/orders",
                       new SplitPurchasePlanByOrdersDto
                       {
                           PlanId = mergedPlan.Id,
                           SaleOrderIds = [order1.Id],
                           Remark = planSplitRemark
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, splitResponse.StatusCode);
                splitPlan = await ReadApiDataAsync<PurchasePlanDto>(splitResponse);
                Assert.NotEqual(mergedPlan.Id, splitPlan.Id);
                Assert.Equal(planSplitRemark, splitPlan.Remark);
                Assert.Equal(managedSupplierId, splitPlan.SupplierId);
                Assert.Equal(managedPurchaserId, splitPlan.PurchaserId);
                var splitDetail = Assert.Single(splitPlan.Details);
                Assert.Equal(expectedBaseQuantity, splitDetail.RequiredQuantity);
                Assert.Equal(expectedBaseQuantity, splitDetail.PlannedQuantity);
                Assert.Equal(order1.Id, Assert.Single(splitDetail.OrderRelations).SaleOrderId);
                splitPlanId = splitPlan.Id;
                registry.Register<PurchasePlan>(splitPlan.Id, nameof(PurchasePlan.Remark), planSplitRemark);
            }

            PurchasePlanDto remainingPlan;
            using (var remainingResponse = await adminClient.GetAsync($"/api/purchase-plans/{mergedPlan.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, remainingResponse.StatusCode);
                remainingPlan = await ReadApiDataAsync<PurchasePlanDto>(remainingResponse);
                var remainingDetail = Assert.Single(remainingPlan.Details);
                Assert.Equal(expectedBaseQuantity, remainingDetail.RequiredQuantity);
                Assert.Equal(expectedBaseQuantity, remainingDetail.PlannedQuantity);
                Assert.Equal(order2.Id, Assert.Single(remainingDetail.OrderRelations).SaleOrderId);
                remainingPlanId = remainingPlan.Id;
            }

            // 总数量守恒：拆出 + 剩余 = 合并前总量
            var conservedTotal = NumericPrecision.RoundQuantity(
                Assert.Single(splitPlan.Details).PlannedQuantity
                + Assert.Single(remainingPlan.Details).PlannedQuantity);
            Assert.Equal(NumericPrecision.RoundQuantity(expectedBaseQuantity * 2m), conservedTotal);

            // 从拆出计划生成采购单并完成
            PurchaseOrderDto completePo;
            using (var generatePoResponse = await adminClient.PostAsJsonAsync(
                       "/api/purchase-orders/generate-from-plans",
                       new
                       {
                           planIds = new[] { splitPlan.Id },
                           receiveTime = "2026-07-18T08:00:00Z",
                           remark = poCompleteRemark
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, generatePoResponse.StatusCode);
                var purchaseOrders = await ReadApiDataAsync<List<PurchaseOrderDto>>(generatePoResponse);
                completePo = Assert.Single(purchaseOrders);
                Assert.StartsWith("PO", completePo.PurchaseNo);
                Assert.Equal(PurchaseOrderStatus.Draft, completePo.BusinessStatus);
                Assert.Equal(managedSupplierId, completePo.SupplierId);
                Assert.Equal(managedSupplierName, completePo.SupplierName);
                Assert.Equal(managedPurchaserId, completePo.PurchaserId);
                Assert.Equal(managedPurchaserName, completePo.PurchaserName);
                Assert.Equal(poCompleteRemark, completePo.Remark);
                var poDetail = Assert.Single(completePo.Details);
                Assert.Equal(managedGoodsId, poDetail.GoodsId);
                Assert.Equal(managedGoodsName, poDetail.GoodsName);
                Assert.Equal(expectedBaseQuantity, poDetail.PurchaseQuantity);
                var planRelation = Assert.Single(poDetail.PlanRelations);
                Assert.Equal(splitPlan.Id, planRelation.PurchasePlanId);
                Assert.Equal(expectedBaseQuantity, planRelation.AllocatedQuantity);
                completePoId = completePo.Id;
                registry.Register<PurchaseOrder>(completePo.Id, nameof(PurchaseOrder.Remark), poCompleteRemark);
            }

            // 计划回写已生成
            using (var planAfterPo = await adminClient.GetAsync($"/api/purchase-plans/{splitPlan.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, planAfterPo.StatusCode);
                var plan = await ReadApiDataAsync<PurchasePlanDto>(planAfterPo);
                Assert.Equal(PurchasePlanStatus.Generated, plan.PurchaseStatus);
                Assert.Equal(expectedBaseQuantity, Assert.Single(plan.Details).PurchasedQuantity);
            }

            // 非法：无剩余数量再次生成
            using (var illegalRegen = await adminClient.PostAsJsonAsync(
                       "/api/purchase-orders/generate-from-plans",
                       new
                       {
                           planIds = new[] { splitPlan.Id },
                           receiveTime = "2026-07-18T09:00:00Z",
                           remark = poCompleteRemark
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(illegalRegen, ResponseCode.DatabaseError);
            }

            // 非法：已生成采购单后不可再分配
            using (var illegalAssign = await adminClient.PutAsJsonAsync(
                       "/api/purchase-plans/supplier",
                       new AssignPurchasePlanSupplierDto
                       {
                           PlanIds = [splitPlan.Id],
                           SupplierId = managedSupplierId
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(illegalAssign, ResponseCode.DatabaseError);
            }

            // 完成采购单
            using (var completeResponse = await adminClient.PostAsync(
                       $"/api/purchase-orders/{completePo.Id}/complete",
                       null))
            {
                Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
                var completed = await ReadApiDataAsync<PurchaseOrderDto>(completeResponse);
                Assert.Equal(PurchaseOrderStatus.Completed, completed.BusinessStatus);
                completePo = completed;
            }

            // 非法：重复完成 / 完成后取消 / 完成后删除
            using (var doubleComplete = await adminClient.PostAsync(
                       $"/api/purchase-orders/{completePo.Id}/complete",
                       null))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(doubleComplete, ResponseCode.DatabaseError);
            }

            using (var cancelAfterComplete = await adminClient.PostAsync(
                       $"/api/purchase-orders/{completePo.Id}/cancel",
                       null))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(cancelAfterComplete, ResponseCode.DatabaseError);
            }

            using (var deleteAfterComplete = await adminClient.DeleteAsync(
                       $"/api/purchase-orders/{completePo.Id}"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deleteAfterComplete, ResponseCode.DatabaseError);
            }

            // 剩余计划生成采购单后取消，回写计划为未发布
            PurchaseOrderDto cancelPo;
            using (var generateCancelPo = await adminClient.PostAsJsonAsync(
                       "/api/purchase-orders/generate-from-plans",
                       new
                       {
                           planIds = new[] { remainingPlan.Id },
                           receiveTime = "2026-07-18T10:00:00Z",
                           remark = poCancelRemark
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, generateCancelPo.StatusCode);
                cancelPo = Assert.Single(await ReadApiDataAsync<List<PurchaseOrderDto>>(generateCancelPo));
                cancelPoId = cancelPo.Id;
                registry.Register<PurchaseOrder>(cancelPo.Id, nameof(PurchaseOrder.Remark), poCancelRemark);
            }

            using (var planAfterCancelSource = await adminClient.GetAsync($"/api/purchase-plans/{remainingPlan.Id}"))
            {
                var plan = await ReadApiDataAsync<PurchasePlanDto>(planAfterCancelSource);
                Assert.Equal(PurchasePlanStatus.Generated, plan.PurchaseStatus);
                Assert.Equal(expectedBaseQuantity, Assert.Single(plan.Details).PurchasedQuantity);
            }

            using (var cancelResponse = await adminClient.PostAsync(
                       $"/api/purchase-orders/{cancelPo.Id}/cancel",
                       null))
            {
                Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);
                var cancelled = await ReadApiDataAsync<PurchaseOrderDto>(cancelResponse);
                Assert.Equal(PurchaseOrderStatus.Cancelled, cancelled.BusinessStatus);
            }

            using (var planAfterCancel = await adminClient.GetAsync($"/api/purchase-plans/{remainingPlan.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, planAfterCancel.StatusCode);
                var plan = await ReadApiDataAsync<PurchasePlanDto>(planAfterCancel);
                Assert.Equal(PurchasePlanStatus.Unpublished, plan.PurchaseStatus);
                Assert.Equal(0m, Assert.Single(plan.Details).PurchasedQuantity);
            }

            // 采购单分页/详情
            using (var poList = await adminClient.GetAsync(
                       $"/api/purchase-orders/list?current=1&size=20&keyword={Uri.EscapeDataString(completePo.PurchaseNo)}"))
            {
                Assert.Equal(HttpStatusCode.OK, poList.StatusCode);
                var page = await ReadApiDataAsync<PagedResult<PurchaseOrderDto>>(poList);
                Assert.Contains(page.Records!, item => item.Id == completePo.Id);
            }

            using (var poDetail = await adminClient.GetAsync($"/api/purchase-orders/{completePo.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, poDetail.StatusCode);
                var detail = await ReadApiDataAsync<PurchaseOrderDto>(poDetail);
                Assert.Equal(PurchaseOrderStatus.Completed, detail.BusinessStatus);
                Assert.Single(detail.Details);
            }

            // 受管主数据未被改写
            await using (var managedCheck = fixture.CreateDbContext())
            {
                var supplier = await managedCheck.Suppliers.AsNoTracking()
                    .SingleAsync(item => item.Id == managedSupplierId);
                Assert.Equal(managedSupplierName, supplier.Name);
                var purchaser = await managedCheck.Purchasers.AsNoTracking()
                    .SingleAsync(item => item.Id == managedPurchaserId);
                Assert.Equal(managedPurchaserName, purchaser.Name);
                var goods = await managedCheck.Set<GoodsEntity>().AsNoTracking()
                    .SingleAsync(item => item.Id == managedGoodsId);
                Assert.Equal(managedGoodsCode, goods.Code);
            }

            // 最小权限用户：仅采购读
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
                Assert.Equal(HttpStatusCode.OK, limitedInfo.StatusCode);
                var info = await ReadApiDataAsync<UserInfoDto>(limitedInfo);
                Assert.Equal(limitedUserId, info.UserId);
                Assert.Contains(limitedRoleCode, info.Roles);
                Assert.DoesNotContain(PermissionCodes.All, info.Permissions);
                Assert.Contains(purchaseReadPermission, info.Permissions);
                Assert.DoesNotContain(purchaseCreatePermission, info.Permissions);
                Assert.DoesNotContain(purchaseUpdatePermission, info.Permissions);
                Assert.DoesNotContain(purchaseDeletePermission, info.Permissions);
            }

            using (var routesResponse = await limitedClient.GetAsync("/api/auth/getRoutes"))
            {
                Assert.Equal(HttpStatusCode.OK, routesResponse.StatusCode);
                var routes = await ReadApiDataAsync<GetRoutesResDto>(routesResponse);
                var paths = FlattenRoutePaths(routes.Routes ?? []).ToList();
                Assert.Contains($"/{batch.Id}s", paths);
                Assert.DoesNotContain($"/{batch.Id}w", paths);
            }

            using (var allowedPlanList = await limitedClient.GetAsync(
                       $"/api/purchase-plans/list?current=1&size=10&keyword={Uri.EscapeDataString(splitPlan.PlanNo)}"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedPlanList.StatusCode);
            }

            using (var allowedPlanDetail = await limitedClient.GetAsync($"/api/purchase-plans/{splitPlan.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedPlanDetail.StatusCode);
            }

            using (var allowedPoList = await limitedClient.GetAsync(
                       $"/api/purchase-orders/list?current=1&size=10&keyword={Uri.EscapeDataString(completePo.PurchaseNo)}"))
            {
                Assert.Equal(HttpStatusCode.OK, allowedPoList.StatusCode);
            }

            using (var deniedGenerate = await limitedClient.PostAsJsonAsync(
                       "/api/purchase-plans/generate",
                       new GeneratePurchasePlanFromOrdersDto
                       {
                           OrderIds = [draftOrder.Id],
                           Remark = $"{batch.Id}DX"
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedGenerate, ResponseCode.Forbidden);
            }

            using (var deniedCreatePlan = await limitedClient.PostAsJsonAsync(
                       "/api/purchase-plans",
                       new
                       {
                           planDate = "2026-07-17T12:00:00Z",
                           purchasePattern = PurchasePattern.SupplierDirect,
                           remark = expandedPlanRemark,
                           details = new[]
                           {
                               new
                               {
                                   goodsId = managedGoodsId,
                                   purchaseUnitId = managedGoodsUnitId,
                                   plannedQuantity = expectedBaseQuantity
                               }
                           }
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedCreatePlan, ResponseCode.Forbidden);
            }

            using (var deniedAssign = await limitedClient.PutAsJsonAsync(
                       "/api/purchase-plans/supplier",
                       new AssignPurchasePlanSupplierDto
                       {
                           PlanIds = [remainingPlan.Id],
                           SupplierId = managedSupplierId
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedAssign, ResponseCode.Forbidden);
            }

            using (var deniedComplete = await limitedClient.PostAsync(
                       $"/api/purchase-orders/{completePo.Id}/complete",
                       null))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedComplete, ResponseCode.Forbidden);
            }

            using (var deniedDelete = await limitedClient.DeleteAsync($"/api/purchase-orders/{cancelPo.Id}"))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedDelete, ResponseCode.Forbidden);
            }

            // 扩权写：分配写菜单后重新登录
            using (var expandWriteMenus = await adminClient.PostAsJsonAsync(
                       "/api/roles/assignMenus",
                       new AssignMenusDto
                       {
                           RoleId = limitedRoleId,
                           MenuIds = [seedMenuId, writeMenuId]
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, expandWriteMenus.StatusCode);
            }

            LoginResDto limitedWriteLogin;
            using (var expandWriteLoginResponse = await anonymousClient.PostAsJsonAsync("/api/auth/login", new LoginReqDto
            {
                Username = limitedUsername,
                Password = password
            }))
            {
                Assert.Equal(HttpStatusCode.OK, expandWriteLoginResponse.StatusCode);
                limitedWriteLogin = await ReadApiDataAsync<LoginResDto>(expandWriteLoginResponse);
            }

            using var limitedWriteClient = factory.CreateClient();
            limitedWriteClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(AuthConstants.BearerScheme, limitedWriteLogin.Token);
            limitedWriteClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

            using (var infoAfterWriteExpand = await limitedWriteClient.GetAsync("/api/auth/getUserInfo"))
            {
                Assert.Equal(HttpStatusCode.OK, infoAfterWriteExpand.StatusCode);
                var info = await ReadApiDataAsync<UserInfoDto>(infoAfterWriteExpand);
                Assert.Contains(purchaseReadPermission, info.Permissions);
                Assert.Contains(purchaseCreatePermission, info.Permissions);
                Assert.Contains(purchaseUpdatePermission, info.Permissions);
                Assert.Contains(purchaseDeletePermission, info.Permissions);
            }

            using (var routesAfterExpand = await limitedWriteClient.GetAsync("/api/auth/getRoutes"))
            {
                Assert.Equal(HttpStatusCode.OK, routesAfterExpand.StatusCode);
                var routes = await ReadApiDataAsync<GetRoutesResDto>(routesAfterExpand);
                var paths = FlattenRoutePaths(routes.Routes ?? []).ToList();
                Assert.Contains($"/{batch.Id}s", paths);
                Assert.Contains($"/{batch.Id}w", paths);
            }

            // 扩权后可手工创建计划、分配并删除草稿采购单（取消后的采购单不可删，另建草稿）
            PurchasePlanDto expandedPlan;
            using (var createExpanded = await limitedWriteClient.PostAsJsonAsync(
                       "/api/purchase-plans",
                       new
                       {
                           planDate = "2026-07-17T13:00:00Z",
                           purchasePattern = PurchasePattern.SupplierDirect,
                           supplierId = managedSupplierId,
                           purchaserId = managedPurchaserId,
                           remark = expandedPlanRemark,
                           details = new[]
                           {
                               new
                               {
                                   goodsId = managedGoodsId,
                                   purchaseUnitId = managedGoodsUnitId,
                                   plannedQuantity = expectedBaseQuantity
                               }
                           }
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, createExpanded.StatusCode);
                expandedPlan = await ReadApiDataAsync<PurchasePlanDto>(createExpanded);
                Assert.Equal(expandedPlanRemark, expandedPlan.Remark);
                Assert.Equal(PurchasePlanStatus.Unpublished, expandedPlan.PurchaseStatus);
                expandedPlanId = expandedPlan.Id;
                registry.Register<PurchasePlan>(expandedPlan.Id, nameof(PurchasePlan.Remark), expandedPlanRemark);
            }

            using (var reassignExpanded = await limitedWriteClient.PutAsJsonAsync(
                       "/api/purchase-plans/supplier",
                       new AssignPurchasePlanSupplierDto
                       {
                           PlanIds = [expandedPlan.Id],
                           SupplierId = managedSupplierId
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, reassignExpanded.StatusCode);
            }

            // 从扩权计划生成草稿采购单后删除，验证删除权限与计划占用释放
            PurchaseOrderDto expandedPo;
            using (var generateExpandedPo = await limitedWriteClient.PostAsJsonAsync(
                       "/api/purchase-orders/generate-from-plans",
                       new
                       {
                           planIds = new[] { expandedPlan.Id },
                           receiveTime = "2026-07-18T11:00:00Z",
                           remark = $"{batch.Id}PW"
                       }))
            {
                Assert.Equal(HttpStatusCode.OK, generateExpandedPo.StatusCode);
                expandedPo = Assert.Single(await ReadApiDataAsync<List<PurchaseOrderDto>>(generateExpandedPo));
                registry.Register<PurchaseOrder>(expandedPo.Id, nameof(PurchaseOrder.Remark), $"{batch.Id}PW");
            }

            using (var deleteExpandedPo = await limitedWriteClient.DeleteAsync($"/api/purchase-orders/{expandedPo.Id}"))
            {
                Assert.Equal(HttpStatusCode.OK, deleteExpandedPo.StatusCode);
                Assert.True(await ReadApiDataAsync<bool>(deleteExpandedPo));
            }

            using (var planAfterExpandedDelete = await limitedWriteClient.GetAsync(
                       $"/api/purchase-plans/{expandedPlan.Id}"))
            {
                var plan = await ReadApiDataAsync<PurchasePlanDto>(planAfterExpandedDelete);
                Assert.Equal(PurchasePlanStatus.Unpublished, plan.PurchaseStatus);
                Assert.Equal(0m, Assert.Single(plan.Details).PurchasedQuantity);
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
                Assert.Contains(purchaseReadPermission, info.Permissions);
                Assert.DoesNotContain(purchaseCreatePermission, info.Permissions);
                Assert.DoesNotContain(purchaseUpdatePermission, info.Permissions);
                Assert.DoesNotContain(purchaseDeletePermission, info.Permissions);
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
                       "/api/purchase-plans",
                       new
                       {
                           planDate = "2026-07-17T14:00:00Z",
                           remark = $"{batch.Id}PF",
                           details = new[]
                           {
                               new
                               {
                                   goodsId = managedGoodsId,
                                   purchaseUnitId = managedGoodsUnitId,
                                   plannedQuantity = 1m
                               }
                           }
                       }))
            {
                await ApiHttpAssert.AssertBusinessCodeAsync(deniedCreateAfterShrink, ResponseCode.Forbidden);
            }

            // 操作员清理业务临时单据：已完成采购单无法删除，保留至 finally 精确清理
            // 已取消采购单也无法删除；未发布扩权计划无删除 API，保留至 finally
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

            // 先清本轮批次登记实体；UserRole 随用户级联，RoleMenu 随角色/菜单级联
            // 明细/计划关系随主单级联；采购单须先于计划（plan_rel Restrict 计划明细）
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

                // 兜底：采购单 → 计划 → 销售订单（外键逆序）
                var residualPoRemarks = new List<string>
                {
                    poCompleteRemark,
                    poCancelRemark,
                    $"{batch.Id}PW"
                };
                var residualPoIds = new List<Guid>();
                if (completePoId.HasValue)
                    residualPoIds.Add(completePoId.Value);
                if (cancelPoId.HasValue)
                    residualPoIds.Add(cancelPoId.Value);

                var residualPurchaseOrders = await cleanupContext.PurchaseOrders
                    .Where(item => residualPoIds.Contains(item.Id)
                                   || (item.Remark != null
                                       && (residualPoRemarks.Contains(item.Remark)
                                           || item.Remark.StartsWith(batch.Id))))
                    .ToListAsync();
                if (residualPurchaseOrders.Count > 0)
                {
                    cleanupContext.PurchaseOrders.RemoveRange(residualPurchaseOrders);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualPlanRemarks = new List<string>
                {
                    planGenerateRemark,
                    planMergeRemark,
                    planSplitRemark,
                    expandedPlanRemark
                };
                var residualPlanIds = new List<Guid>();
                if (mergedPlanId.HasValue)
                    residualPlanIds.Add(mergedPlanId.Value);
                if (splitPlanId.HasValue)
                    residualPlanIds.Add(splitPlanId.Value);
                if (remainingPlanId.HasValue)
                    residualPlanIds.Add(remainingPlanId.Value);
                if (expandedPlanId.HasValue)
                    residualPlanIds.Add(expandedPlanId.Value);

                var residualPlans = await cleanupContext.PurchasePlans
                    .Where(item => residualPlanIds.Contains(item.Id)
                                   || (item.Remark != null
                                       && (residualPlanRemarks.Contains(item.Remark)
                                           || item.Remark.StartsWith(batch.Id))))
                    .ToListAsync();
                if (residualPlans.Count > 0)
                {
                    cleanupContext.PurchasePlans.RemoveRange(residualPlans);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualInnerRemarks = new List<string>
                {
                    order1InnerRemark,
                    order2InnerRemark,
                    $"{batch.Id}OD"
                };
                var residualOrderIds = new List<Guid>();
                if (order1Id.HasValue)
                    residualOrderIds.Add(order1Id.Value);
                if (order2Id.HasValue)
                    residualOrderIds.Add(order2Id.Value);

                var residualOrders = await cleanupContext.SaleOrders
                    .Where(item => residualOrderIds.Contains(item.Id)
                                   || (item.InnerRemark != null
                                       && (residualInnerRemarks.Contains(item.InnerRemark)
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
                                   || role.Code == limitedRoleCode
                                   || (role.Code != null && role.Code.StartsWith(batch.Id)))
                    .ToListAsync();
                if (residualRoles.Count > 0)
                {
                    cleanupContext.Roles.RemoveRange(residualRoles);
                    await cleanupContext.SaveChangesAsync();
                }

                var residualButtonIds = new List<Guid>
                {
                    seedPurchaseReadButtonId,
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
                button.Id == seedPurchaseReadButtonId
                || button.Id == writeCreateButtonId
                || button.Id == writeUpdateButtonId
                || button.Id == writeDeleteButtonId
                || button.CreateName == createName));
            Assert.False(await residualContext.PurchaseOrders.AnyAsync(item =>
                item.Remark == poCompleteRemark
                || item.Remark == poCancelRemark
                || (item.Remark != null && item.Remark.StartsWith(batch.Id))));
            Assert.False(await residualContext.PurchasePlans.AnyAsync(item =>
                item.Remark == planGenerateRemark
                || item.Remark == planMergeRemark
                || item.Remark == planSplitRemark
                || item.Remark == expandedPlanRemark
                || (item.Remark != null && item.Remark.StartsWith(batch.Id))));
            Assert.False(await residualContext.SaleOrders.AnyAsync(item =>
                item.InnerRemark == order1InnerRemark
                || item.InnerRemark == order2InnerRemark
                || (item.InnerRemark != null && item.InnerRemark.StartsWith(batch.Id))));
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
            Assert.True(await residualContext.Customers.AnyAsync(item => item.Id == managedCustomerId));
            Assert.True(await residualContext.Set<GoodsEntity>().AnyAsync(item => item.Id == managedGoodsId));
            Assert.True(await residualContext.GoodsUnits.AnyAsync(item => item.Id == managedGoodsUnitId));
            Assert.True(await residualContext.Wares.AnyAsync(item => item.Id == managedWareId));
            Assert.True(await residualContext.Suppliers.AnyAsync(item => item.Id == managedSupplierId));
            Assert.True(await residualContext.Purchasers.AnyAsync(item => item.Id == managedPurchaserId));
        }
    }

    private static async Task<SaleOrderDto> CreateAndApproveSaleOrderAsync(
        HttpClient client,
        Guid customerId,
        Guid wareId,
        Guid goodsId,
        Guid goodsUnitId,
        decimal quantity,
        decimal fixedPrice,
        string innerRemark,
        string orderDate,
        string remark)
    {
        SaleOrderDto order;
        using (var createResponse = await client.PostAsJsonAsync(
                   "/api/orders",
                   new
                   {
                       customerId,
                       wareId,
                       orderDate,
                       receiveDate = "2026-07-18T06:00:00Z",
                       contactName = "联调食堂",
                       contactPhone = "13800139601",
                       deliveryAddress = "上海市浦东新区采购联调路 6 号",
                       remark,
                       innerRemark,
                       details = new[]
                       {
                           new
                           {
                               goodsId,
                               goodsUnitId,
                               quantity,
                               fixedPrice,
                               fixedGoodsUnitId = goodsUnitId,
                               remark = "T6采购来源明细",
                               innerRemark = $"{innerRemark}D"
                           }
                       }
                   }))
        {
            Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
            order = await ReadApiDataAsync<SaleOrderDto>(createResponse);
            Assert.Equal(SaleOrderStatus.PendingAudit, order.OrderStatus);
        }

        using (var approveResponse = await client.PostAsJsonAsync(
                   $"/api/orders/{order.Id}/approve",
                   new SaleOrderAuditDto { Remark = $"{innerRemark}-审核通过" }))
        {
            Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);
            order = await ReadApiDataAsync<SaleOrderDto>(approveResponse);
            Assert.Equal(SaleOrderStatus.SortingPending, order.OrderStatus);
        }

        return order;
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
