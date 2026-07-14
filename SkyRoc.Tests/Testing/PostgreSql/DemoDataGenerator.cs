using Application.DTOs.AfterSales;
using Application.DTOs.Customers;
using Application.DTOs.Department;
using Application.DTOs.Delivery;
using Application.DTOs.Goods;
using Application.DTOs.Orders;
using Application.DTOs.Printing;
using Application.DTOs.Purchases;
using Application.DTOs.Pricing;
using Application.DTOs.Role;
using Application.DTOs.Storage;
using Application.DTOs.System;
using Application.DTOs.User;
using Application.interfaces;
using Application.interfaces.System;
using Domain.Entities;
using Domain.Entities.AfterSales;
using Domain.Entities.Delivery;
using Domain.Entities.Finance;
using Domain.Entities.Orders;
using Domain.Entities.Printing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using Domain.Entities.System;
using Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Shared.Constants;

namespace SkyRoc.Tests.Testing.PostgreSql;

/// <summary>
///     按完整稳定业务键补齐长期前端联调数据，并通过真实应用服务形成订单、库存、配送、售后和财务业务链路。
/// </summary>
public sealed class DemoDataGenerator(PostgreSqlTestFixture fixture)
{
    private const string CompaniesLayer = "companies";
    private const string CustomerTagsLayer = "customer-tags";
    private const string CustomerProtocolGoodsLayer = "customer-protocol-goods";
    private const string CustomerProtocolsLayer = "customer-protocols";
    private const string CustomerSubAccountsLayer = "customer-sub-accounts";
    private const string CustomersLayer = "customers";
    private const string AfterSaleAuditLogsLayer = "after-sale-audit-logs";
    private const string AfterSaleGoodsLayer = "after-sale-goods";
    private const string AfterSalesLayer = "after-sales";
    private const string CustomerBillDetailsLayer = "customer-bill-details";
    private const string CustomerBillsLayer = "customer-bills";
    private const string CustomerSettlementDetailsLayer = "customer-settlement-details";
    private const string CustomerSettlementsLayer = "customer-settlements";
    private const string DepartmentsLayer = "departments";
    private const string CarriersLayer = "carriers";
    private const string DeliveryTasksLayer = "delivery-tasks";
    private const string DeliveryRoutesLayer = "delivery-routes";
    private const string DriversLayer = "drivers";
    private const string GoodsLayer = "goods";
    private const string GoodsImagesLayer = "goods-images";
    private const string GoodsUnitsLayer = "goods-units";
    private const string GoodsTypesLayer = "goods-types";
    private const string InspectionAttachmentsLayer = "inspection-attachments";
    private const string InspectionReportGoodsLayer = "inspection-report-goods";
    private const string InspectionReportsLayer = "inspection-reports";
    private const string ImportExportJobsLayer = "import-export-jobs";
    private const string PickupTasksLayer = "pickup-tasks";
    private const string PurchasersLayer = "purchasers";
    private const string PurchasePlanDetailsLayer = "purchase-plan-details";
    private const string PurchasePlanOrderRelationsLayer = "purchase-plan-order-relations";
    private const string PurchasePlansLayer = "purchase-plans";
    private const string PurchaseOrderDetailsLayer = "purchase-order-details";
    private const string PurchaseOrderPlanRelationsLayer = "purchase-order-plan-relations";
    private const string PurchaseOrdersLayer = "purchase-orders";
    private const string PurchaseStockInDetailsLayer = "purchase-stock-in-details";
    private const string PurchaseStockInsLayer = "purchase-stock-ins";
    private const string PurchaseRulesLayer = "purchase-rules";
    private const string SaleStockOutDetailsLayer = "sale-stock-out-details";
    private const string SaleStockOutLedgersLayer = "sale-stock-out-ledgers";
    private const string SaleStockOutsLayer = "sale-stock-outs";
    private const string SaleStockSupportBatchesLayer = "sale-stock-support-batches";
    private const string SaleStockSupportInDetailsLayer = "sale-stock-support-in-details";
    private const string SaleStockSupportInsLayer = "sale-stock-support-ins";
    private const string SaleStockSupportLedgersLayer = "sale-stock-support-ledgers";
    private const string SalesReturnStockInDetailsLayer = "sales-return-stock-in-details";
    private const string SalesReturnStockInsLayer = "sales-return-stock-ins";
    private const string SaleOrderDetailsLayer = "sale-order-details";
    private const string SaleOrdersLayer = "sale-orders";
    private const string OrderCheckDetailsLayer = "order-check-details";
    private const string OrderReceiptsLayer = "order-receipts";
    private const string ServicePeriodsLayer = "service-periods";
    private const string NoticesLayer = "notices";
    private const string PrintTemplatesLayer = "print-templates";
    private const string OperationLogsLayer = "operation-logs";
    private const string LoginLogsLayer = "login-logs";
    private const string QuotationGoodsLayer = "quotation-goods";
    private const string QuotationsLayer = "quotations";
    private const string SuppliersLayer = "suppliers";
    private const string SupplierBillDetailsLayer = "supplier-bill-details";
    private const string SupplierBillsLayer = "supplier-bills";
    private const string SupplierSettlementDetailsLayer = "supplier-settlement-details";
    private const string SupplierSettlementsLayer = "supplier-settlements";
    private const string StockBatchesLayer = "stock-batches";
    private const string StockLedgersLayer = "stock-ledgers";
    private const string StoredFilesLayer = "stored-files";
    private const string SystemRolesLayer = "system-roles";
    private const string SystemUsersLayer = "system-users";
    private const string TraceRecordsLayer = "trace-records";
    private const string TraceSaleOrderAuditLogsLayer = "trace-sale-order-audit-logs";
    private const string TraceSaleOrderDetailsLayer = "trace-sale-order-details";
    private const string TraceSaleOrdersLayer = "trace-sale-orders";
    private const string TraceStockOutDetailsLayer = "trace-stock-out-details";
    private const string TraceStockOutLedgersLayer = "trace-stock-out-ledgers";
    private const string TraceStockOutsLayer = "trace-stock-outs";
    private const string WaresLayer = "wares";

    /// <summary>
    ///     在经白名单验证的真实 PostgreSQL 中幂等生成已实现的基础资料与业务场景层。
    /// </summary>
    /// <param name="cancellationToken">取消生成的令牌。</param>
    /// <returns>按资料层汇总的新增与复用数量。</returns>
    public async Task<DemoDataGenerationResult> GenerateAsync(CancellationToken cancellationToken = default)
    {
        DatabaseSafetyGuard.Validate(fixture.Settings);

        using var factory = fixture.CreateWebApplicationFactory();
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var afterSaleService = scope.ServiceProvider.GetRequiredService<IAfterSaleService>();
        var pickupTaskService = scope.ServiceProvider.GetRequiredService<IPickupTaskService>();
        var companyService = scope.ServiceProvider.GetRequiredService<ICompanyService>();
        var carrierService = scope.ServiceProvider.GetRequiredService<ICarrierService>();
        var customerTagService = scope.ServiceProvider.GetRequiredService<ICustomerTagService>();
        var customerService = scope.ServiceProvider.GetRequiredService<ICustomerService>();
        var customerSettlementService = scope.ServiceProvider.GetRequiredService<ICustomerSettlementService>();
        var supplierSettlementService = scope.ServiceProvider.GetRequiredService<ISupplierSettlementService>();
        var traceabilityService = scope.ServiceProvider.GetRequiredService<ITraceabilityService>();
        var fileStorageService = scope.ServiceProvider.GetRequiredService<IFileStorageService>();
        var deliveryRouteService = scope.ServiceProvider.GetRequiredService<IDeliveryRouteService>();
        var deliveryTaskService = scope.ServiceProvider.GetRequiredService<IDeliveryTaskService>();
        var driverService = scope.ServiceProvider.GetRequiredService<IDriverService>();
        var customerProtocolGoodsService = scope.ServiceProvider.GetRequiredService<ICustomerProtocolGoodsService>();
        var customerProtocolService = scope.ServiceProvider.GetRequiredService<ICustomerProtocolService>();
        var customerSubAccountService = scope.ServiceProvider.GetRequiredService<ICustomerSubAccountService>();
        var departmentService = scope.ServiceProvider.GetRequiredService<IDepartmentService>();
        var goodsService = scope.ServiceProvider.GetRequiredService<IGoodsService>();
        var goodsUnitService = scope.ServiceProvider.GetRequiredService<IGoodsUnitService>();
        var goodsTypeService = scope.ServiceProvider.GetRequiredService<IGoodsTypeService>();
        var purchaserService = scope.ServiceProvider.GetRequiredService<IPurchaserService>();
        var purchasePlanService = scope.ServiceProvider.GetRequiredService<IPurchasePlanService>();
        var purchaseOrderService = scope.ServiceProvider.GetRequiredService<IPurchaseOrderService>();
        var purchaseRuleService = scope.ServiceProvider.GetRequiredService<IPurchaseRuleService>();
        var saleOrderService = scope.ServiceProvider.GetRequiredService<ISaleOrderService>();
        var stockInService = scope.ServiceProvider.GetRequiredService<IStockInService>();
        var stockOutService = scope.ServiceProvider.GetRequiredService<IStockOutService>();
        var systemSupportService = scope.ServiceProvider.GetRequiredService<ISystemSupportService>();
        var printService = scope.ServiceProvider.GetRequiredService<IPrintService>();
        var quotationGoodsService = scope.ServiceProvider.GetRequiredService<IQuotationGoodsService>();
        var quotationService = scope.ServiceProvider.GetRequiredService<IQuotationService>();
        var supplierService = scope.ServiceProvider.GetRequiredService<ISupplierService>();
        var roleService = scope.ServiceProvider.GetRequiredService<IRoleService>();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        var wareService = scope.ServiceProvider.GetRequiredService<IWareService>();
        var companySeeds = CreateCompanySeeds();
        var carrierSeeds = CreateCarrierSeeds();
        var customerTagSeeds = CreateCustomerTagSeeds();
        var customerSeeds = CreateCustomerSeeds();
        var customerSubAccountSeeds = CreateCustomerSubAccountSeeds();
        var departmentSeeds = CreateDepartmentSeeds();
        var deliveryRouteSeeds = CreateDeliveryRouteSeeds();
        var driverSeeds = CreateDriverSeeds();
        var customerProtocolSeeds = CreateCustomerProtocolSeeds();
        var goodsSeeds = CreateGoodsSeeds();
        var goodsTypeSeeds = CreateGoodsTypeSeeds();
        var purchaserSeeds = CreatePurchaserSeeds();
        var purchaseRuleSeeds = CreatePurchaseRuleSeeds();
        var saleOrderSeeds = CreateSaleOrderSeeds();
        var servicePeriodSeeds = CreateServicePeriodSeeds();
        var noticeSeeds = CreateNoticeSeeds();
        var printTemplateSeeds = CreatePrintTemplateSeeds();
        var operationLogSeeds = CreateOperationLogSeeds();
        var loginLogSeeds = CreateLoginLogSeeds();
        var quotationSeeds = CreateQuotationSeeds();
        var supplierSeeds = CreateSupplierSeeds();
        var systemRoleSeeds = CreateSystemRoleSeeds();
        var systemUserSeeds = CreateSystemUserSeeds();
        var wareSeeds = CreateWareSeeds();
        var companyCodes = companySeeds.Select(seed => seed.Code).ToArray();
        var carrierCodes = carrierSeeds.Select(seed => seed.Code).ToArray();
        var customerTagCodes = customerTagSeeds.Select(seed => seed.Code).ToArray();
        var customerCodes = customerSeeds.Select(seed => seed.Code).ToArray();
        var customerSubAccountUsernames = customerSubAccountSeeds.Select(seed => seed.Username).ToArray();
        var departmentCodes = departmentSeeds.Select(seed => seed.Code).ToArray();
        var deliveryRouteCodes = deliveryRouteSeeds.Select(seed => seed.Code).ToArray();
        var driverCodes = driverSeeds.Select(seed => seed.Code).ToArray();
        var customerProtocolCodes = customerProtocolSeeds.Select(seed => seed.Code).ToArray();
        var goodsCodes = goodsSeeds.Select(seed => seed.Code).ToArray();
        var goodsUnitCodes = goodsSeeds.Select(seed => seed.UnitCode).ToArray();
        var goodsTypeCodes = goodsTypeSeeds.Select(seed => seed.Code).ToArray();
        var purchaserCodes = purchaserSeeds.Select(seed => seed.Code).ToArray();
        var purchaseRuleCodes = purchaseRuleSeeds.Select(seed => seed.Code).ToArray();
        var servicePeriodNames = servicePeriodSeeds.Select(seed => seed.Name).ToArray();
        var noticeTitles = noticeSeeds.Select(seed => seed.Title).ToArray();
        var printTemplateCodes = printTemplateSeeds.Select(seed => seed.TemplateCode).ToArray();
        var operationLogDescriptions = operationLogSeeds.Select(seed => seed.Description).ToArray();
        var loginLogUsernames = loginLogSeeds.Select(seed => seed.Username).ToArray();
        var quotationCodes = quotationSeeds.Select(seed => seed.Code).ToArray();
        var supplierCodes = supplierSeeds.Select(seed => seed.Code).ToArray();
        var systemRoleCodes = systemRoleSeeds.Select(seed => seed.Code).ToArray();
        var systemUsernames = systemUserSeeds.Select(seed => seed.Username).ToArray();
        var wareCodes = wareSeeds.Select(seed => seed.Code).ToArray();
        var auditUser = await context.Users
            .OrderBy(user => user.Username)
            .Select(user => new DemoAuditUser(user.Id, user.Username))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("长期联调数据生成需要已存在的可审计系统用户。");
        SetAuditUser(scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>(), auditUser);
        var existingCompanies = await context.Companies
            .Where(company => companyCodes.Contains(company.Code))
            .ToDictionaryAsync(company => company.Code, StringComparer.Ordinal, cancellationToken);
        var existingCarriers = await context.Carriers
            .Where(carrier => carrierCodes.Contains(carrier.Code))
            .ToDictionaryAsync(carrier => carrier.Code, StringComparer.Ordinal, cancellationToken);
        var existingCustomerTags = await context.CustomerTags
            .Where(tag => customerTagCodes.Contains(tag.Code))
            .ToDictionaryAsync(tag => tag.Code, StringComparer.Ordinal, cancellationToken);
        var existingCustomerSubAccounts = await context.CustomerSubAccounts
            .Where(subAccount => customerSubAccountUsernames.Contains(subAccount.Username))
            .ToDictionaryAsync(subAccount => subAccount.Username, StringComparer.Ordinal, cancellationToken);
        var existingDepartments = await context.Departments
            .Where(department => departmentCodes.Contains(department.Code))
            .ToDictionaryAsync(department => department.Code, StringComparer.Ordinal, cancellationToken);
        var existingDeliveryRoutes = await context.DeliveryRoutes
            .Include(route => route.CustomerRoutes)
            .Where(route => deliveryRouteCodes.Contains(route.Code))
            .ToDictionaryAsync(route => route.Code, StringComparer.Ordinal, cancellationToken);
        var existingDrivers = await context.Drivers
            .Where(driver => driverCodes.Contains(driver.Code))
            .ToDictionaryAsync(driver => driver.Code, StringComparer.Ordinal, cancellationToken);
        var existingWares = await context.Wares
            .Where(ware => wareCodes.Contains(ware.Code))
            .ToDictionaryAsync(ware => ware.Code, StringComparer.Ordinal, cancellationToken);
        var existingGoods = await context.Goods
            .Where(goods => goodsCodes.Contains(goods.Code))
            .ToDictionaryAsync(goods => goods.Code, StringComparer.Ordinal, cancellationToken);
        var existingGoodsUnits = await context.GoodsUnits
            .Where(unit => goodsUnitCodes.Contains(unit.Code))
            .ToDictionaryAsync(unit => unit.Code!, StringComparer.Ordinal, cancellationToken);
        var existingSuppliers = await context.Suppliers
            .Where(supplier => supplierCodes.Contains(supplier.Code))
            .ToDictionaryAsync(supplier => supplier.Code, StringComparer.Ordinal, cancellationToken);
        var existingSystemRoles = await context.Roles
            .Where(role => systemRoleCodes.Contains(role.Code))
            .ToDictionaryAsync(role => role.Code, StringComparer.Ordinal, cancellationToken);
        var existingSystemUsers = await context.Users
            .Where(user => systemUsernames.Contains(user.Username))
            .ToDictionaryAsync(user => user.Username, StringComparer.Ordinal, cancellationToken);
        var existingPurchasers = await context.Purchasers
            .Where(purchaser => purchaserCodes.Contains(purchaser.Code))
            .ToDictionaryAsync(purchaser => purchaser.Code, StringComparer.Ordinal, cancellationToken);
        var existingPurchaseRules = await context.PurchaseRules
            .Include(rule => rule.Goods)
            .Include(rule => rule.Customers)
            .Where(rule => purchaseRuleCodes.Contains(rule.Code))
            .ToDictionaryAsync(rule => rule.Code, StringComparer.Ordinal, cancellationToken);
        var existingServicePeriods = await context.ServicePeriods
            .Where(period => servicePeriodNames.Contains(period.Name))
            .ToDictionaryAsync(period => period.Name, StringComparer.Ordinal, cancellationToken);
        var existingNotices = await context.Notices
            .Where(notice => noticeTitles.Contains(notice.Title))
            .ToDictionaryAsync(notice => notice.Title, StringComparer.Ordinal, cancellationToken);
        var existingPrintTemplates = await context.PrintTemplates
            .Include(template => template.Fields)
            .Where(template => printTemplateCodes.Contains(template.TemplateCode))
            .ToDictionaryAsync(template => template.TemplateCode, StringComparer.Ordinal, cancellationToken);
        var existingOperationLogs = await context.OperationLogs
            .Where(log => operationLogDescriptions.Contains(log.Desc))
            .ToDictionaryAsync(log => log.Desc, StringComparer.Ordinal, cancellationToken);
        var existingLoginLogs = await context.LoginLogs
            .Where(log => loginLogUsernames.Contains(log.Username))
            .ToDictionaryAsync(log => log.Username, StringComparer.Ordinal, cancellationToken);
        var existingQuotations = await context.Quotations
            .Where(quotation => quotationCodes.Contains(quotation.Code))
            .ToDictionaryAsync(quotation => quotation.Code, StringComparer.Ordinal, cancellationToken);
        var existingCustomerProtocols = await context.CustomerProtocols
            .Include(protocol => protocol.Customers)
            .Where(protocol => customerProtocolCodes.Contains(protocol.Code))
            .ToDictionaryAsync(protocol => protocol.Code, StringComparer.Ordinal, cancellationToken);
        var organizationalUsers = await context.Users
            .OrderBy(user => user.Username)
            .Select(user => new DemoOrganizationalUser(user.Id, user.Username))
            .ToListAsync(cancellationToken);
        if (organizationalUsers.Count == 0)
        {
            throw new InvalidOperationException("长期联调采购员生成需要至少一条已存在的系统用户资料。");
        }

        var createdCompanies = 0;
        var reusedCompanies = 0;
        var createdAfterSaleAuditLogs = 0;
        var reusedAfterSaleAuditLogs = 0;
        var createdAfterSaleGoods = 0;
        var reusedAfterSaleGoods = 0;
        var createdAfterSales = 0;
        var reusedAfterSales = 0;
        var createdCarriers = 0;
        var reusedCarriers = 0;
        var createdCustomerTags = 0;
        var reusedCustomerTags = 0;
        var createdCustomers = 0;
        var reusedCustomers = 0;
        var createdDepartments = 0;
        var reusedDepartments = 0;
        var createdCustomerSubAccounts = 0;
        var reusedCustomerSubAccounts = 0;
        var createdCustomerBillDetails = 0;
        var reusedCustomerBillDetails = 0;
        var createdCustomerBills = 0;
        var reusedCustomerBills = 0;
        var createdCustomerSettlementDetails = 0;
        var reusedCustomerSettlementDetails = 0;
        var createdCustomerSettlements = 0;
        var reusedCustomerSettlements = 0;
        var createdDeliveryRoutes = 0;
        var reusedDeliveryRoutes = 0;
        var createdDeliveryTasks = 0;
        var reusedDeliveryTasks = 0;
        var createdDrivers = 0;
        var reusedDrivers = 0;
        var createdCustomerProtocolGoods = 0;
        var reusedCustomerProtocolGoods = 0;
        var createdCustomerProtocols = 0;
        var reusedCustomerProtocols = 0;
        var createdGoods = 0;
        var reusedGoods = 0;
        var createdGoodsImages = 0;
        var reusedGoodsImages = 0;
        var createdGoodsUnits = 0;
        var reusedGoodsUnits = 0;
        var createdGoodsTypes = 0;
        var reusedGoodsTypes = 0;
        var createdInspectionAttachments = 0;
        var reusedInspectionAttachments = 0;
        var createdInspectionReportGoods = 0;
        var reusedInspectionReportGoods = 0;
        var createdInspectionReports = 0;
        var reusedInspectionReports = 0;
        var createdImportExportJobs = 0;
        var reusedImportExportJobs = 0;
        var createdPickupTasks = 0;
        var reusedPickupTasks = 0;
        var createdPurchasers = 0;
        var reusedPurchasers = 0;
        var createdPurchasePlanDetails = 0;
        var reusedPurchasePlanDetails = 0;
        var createdPurchasePlanOrderRelations = 0;
        var reusedPurchasePlanOrderRelations = 0;
        var createdPurchasePlans = 0;
        var reusedPurchasePlans = 0;
        var createdPurchaseOrderDetails = 0;
        var reusedPurchaseOrderDetails = 0;
        var createdPurchaseOrderPlanRelations = 0;
        var reusedPurchaseOrderPlanRelations = 0;
        var createdPurchaseOrders = 0;
        var reusedPurchaseOrders = 0;
        var createdPurchaseStockInDetails = 0;
        var reusedPurchaseStockInDetails = 0;
        var createdPurchaseStockIns = 0;
        var reusedPurchaseStockIns = 0;
        var createdPurchaseRules = 0;
        var reusedPurchaseRules = 0;
        var createdSaleStockOutDetails = 0;
        var reusedSaleStockOutDetails = 0;
        var createdSaleStockOutLedgers = 0;
        var reusedSaleStockOutLedgers = 0;
        var createdSaleStockOuts = 0;
        var reusedSaleStockOuts = 0;
        var createdSaleStockSupportBatches = 0;
        var reusedSaleStockSupportBatches = 0;
        var createdSaleStockSupportInDetails = 0;
        var reusedSaleStockSupportInDetails = 0;
        var createdSaleStockSupportIns = 0;
        var reusedSaleStockSupportIns = 0;
        var createdSaleStockSupportLedgers = 0;
        var reusedSaleStockSupportLedgers = 0;
        var createdSalesReturnStockInDetails = 0;
        var reusedSalesReturnStockInDetails = 0;
        var createdSalesReturnStockIns = 0;
        var reusedSalesReturnStockIns = 0;
        var createdSaleOrderDetails = 0;
        var reusedSaleOrderDetails = 0;
        var createdSaleOrders = 0;
        var reusedSaleOrders = 0;
        var createdOrderCheckDetails = 0;
        var reusedOrderCheckDetails = 0;
        var createdOrderReceipts = 0;
        var reusedOrderReceipts = 0;
        var createdServicePeriods = 0;
        var reusedServicePeriods = 0;
        var createdNotices = 0;
        var reusedNotices = 0;
        var createdPrintTemplates = 0;
        var reusedPrintTemplates = 0;
        var createdOperationLogs = 0;
        var reusedOperationLogs = 0;
        var createdLoginLogs = 0;
        var reusedLoginLogs = 0;
        var createdQuotationGoods = 0;
        var reusedQuotationGoods = 0;
        var createdQuotations = 0;
        var reusedQuotations = 0;
        var createdSuppliers = 0;
        var reusedSuppliers = 0;
        var createdSupplierBillDetails = 0;
        var reusedSupplierBillDetails = 0;
        var createdSupplierBills = 0;
        var reusedSupplierBills = 0;
        var createdSupplierSettlementDetails = 0;
        var reusedSupplierSettlementDetails = 0;
        var createdSupplierSettlements = 0;
        var reusedSupplierSettlements = 0;
        var createdStockBatches = 0;
        var reusedStockBatches = 0;
        var createdStockLedgers = 0;
        var reusedStockLedgers = 0;
        var createdStoredFiles = 0;
        var reusedStoredFiles = 0;
        var createdSystemRoles = 0;
        var reusedSystemRoles = 0;
        var createdSystemUsers = 0;
        var reusedSystemUsers = 0;
        var createdTraceRecords = 0;
        var reusedTraceRecords = 0;
        var createdTraceSaleOrderAuditLogs = 0;
        var reusedTraceSaleOrderAuditLogs = 0;
        var createdTraceSaleOrderDetails = 0;
        var reusedTraceSaleOrderDetails = 0;
        var createdTraceSaleOrders = 0;
        var reusedTraceSaleOrders = 0;
        var createdTraceStockOutDetails = 0;
        var reusedTraceStockOutDetails = 0;
        var createdTraceStockOutLedgers = 0;
        var reusedTraceStockOutLedgers = 0;
        var createdTraceStockOuts = 0;
        var reusedTraceStockOuts = 0;
        var createdWares = 0;
        var reusedWares = 0;
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var seed in departmentSeeds)
            {
                Guid? parentId = seed.ParentCode is null
                    ? null
                    : GetManagedReferenceId(existingDepartments, seed.ParentCode, "上级部门");
                if (!existingDepartments.TryGetValue(seed.Code, out var department))
                {
                    await departmentService.CreateAsync(seed.ToCreateDto(parentId, auditUser));
                    department = await context.Departments.SingleAsync(
                        item => item.Code == seed.Code,
                        cancellationToken);
                    existingDepartments.Add(seed.Code, department);
                    createdDepartments++;
                    continue;
                }

                if (!seed.Matches(department, parentId, auditUser))
                    await departmentService.UpdateAsync(department.Id, seed.ToUpdateDto(department.Id, parentId, auditUser));

                if (department.CreateBy != auditUser.Id || department.CreateName != auditUser.Username)
                {
                    // 部门创建审计没有公开补写入口，仅修复完整稳定编码对应的受管记录。
                    department.CreateBy = auditUser.Id;
                    department.CreateName = auditUser.Username;
                }

                reusedDepartments++;
            }

            var organizationalDepartments = existingDepartments.Values
                .OrderBy(department => department.Code)
                .Select(department => new DemoOrganizationalDepartment(department.Id, department.Code))
                .ToList();

            foreach (var seed in systemRoleSeeds)
            {
                if (!existingSystemRoles.TryGetValue(seed.Code, out var role))
                {
                    await roleService.CreateRoleAsync(seed.ToCreateDto());
                    createdSystemRoles++;
                    continue;
                }

                if (!seed.Matches(role))
                    await roleService.UpdateRoleAsync(role.Id, seed.ToUpdateDto(role.Id));

                if (role.CreateBy != auditUser.Id || role.CreateName != auditUser.Username)
                {
                    // 角色创建审计没有公开补写入口，仅修复完整稳定编码对应的受管记录。
                    role.CreateBy = auditUser.Id;
                    role.CreateName = auditUser.Username;
                }

                reusedSystemRoles++;
            }

            var managedSystemRoles = await context.Roles
                .Where(role => systemRoleCodes.Contains(role.Code))
                .ToDictionaryAsync(role => role.Code, StringComparer.Ordinal, cancellationToken);
            var permissionMenuNames = new[] { "home", "manage", "manage_role", "manage_user" };
            var permissionMenuIds = await context.Menus
                .Where(menu => permissionMenuNames.Contains(menu.Name))
                .OrderBy(menu => menu.Name)
                .Select(menu => menu.Id)
                .ToArrayAsync(cancellationToken);
            if (permissionMenuIds.Length != permissionMenuNames.Length)
            {
                throw new InvalidOperationException("长期联调系统角色生成需要已存在的首页、管理、角色和用户菜单。 ");
            }

            var managedRoleIds = managedSystemRoles.Values.Select(role => role.Id).ToArray();
            var managedRoleMenus = await context.RoleMenus
                .Where(relation => managedRoleIds.Contains(relation.RoleId))
                .ToListAsync(cancellationToken);
            context.RoleMenus.RemoveRange(managedRoleMenus);
            foreach (var role in managedSystemRoles.Values)
            {
                foreach (var menuId in permissionMenuIds)
                    context.RoleMenus.Add(new Domain.Entities.RoleMenu { RoleId = role.Id, MenuId = menuId });
            }

            foreach (var seed in systemUserSeeds)
            {
                if (!existingSystemUsers.TryGetValue(seed.Username, out var user))
                {
                    await userService.CreateUserAsync(seed.ToCreateDto());
                    createdSystemUsers++;
                    continue;
                }

                if (!seed.Matches(user))
                    await userService.UpdateUserAsync(user.Id, seed.ToUpdateDto(user.Id));

                if (user.CreateBy != auditUser.Id || user.CreateName != auditUser.Username)
                {
                    // 用户创建审计没有公开补写入口，仅修复完整稳定用户名对应的受管记录。
                    user.CreateBy = auditUser.Id;
                    user.CreateName = auditUser.Username;
                }

                reusedSystemUsers++;
            }

            var managedSystemUsers = await context.Users
                .Where(user => systemUsernames.Contains(user.Username))
                .ToDictionaryAsync(user => user.Username, StringComparer.Ordinal, cancellationToken);
            var managedUserRoleRelations = await context.UserRoles
                .Where(relation => managedSystemUsers.Values.Select(user => user.Id).Contains(relation.UserId))
                .ToListAsync(cancellationToken);
            context.UserRoles.RemoveRange(managedUserRoleRelations);
            foreach (var seed in systemUserSeeds)
            {
                var user = GetManagedReference(managedSystemUsers, seed.Username, "系统用户");
                var role = GetManagedReference(managedSystemRoles, seed.RoleCode, "系统角色");
                var department = organizationalDepartments[(seed.Sequence - 1) % organizationalDepartments.Count];
                // 用户部门未包含在公开创建/更新 DTO 中，故仅对完整稳定用户名命中的受管用户受控补齐。
                user.DepartmentId = department.Id;
                user.UpdateBy = auditUser.Id;
                user.UpdateName = auditUser.Username;
                context.UserRoles.Add(new Domain.Entities.UserRole { UserId = user.Id, RoleId = role.Id });
            }

            foreach (var seed in companySeeds)
            {
                if (!existingCompanies.TryGetValue(seed.Code, out var company))
                {
                    await companyService.CreateAsync(seed.ToCreateDto());
                    createdCompanies++;
                    continue;
                }

                if (!seed.Matches(company))
                    await companyService.UpdateAsync(company.Id, seed.ToUpdateDto(company.Id));

                if (company.CreateBy != auditUser.Id || company.CreateName != auditUser.Username)
                {
                    // 创建审计字段没有公开补写接口，只对完整稳定键命中的受管记录受控修复。
                    company.CreateBy = auditUser.Id;
                    company.CreateName = auditUser.Username;
                }

                reusedCompanies++;
            }

            foreach (var seed in customerTagSeeds)
            {
                if (!existingCustomerTags.TryGetValue(seed.Code, out var tag))
                {
                    await customerTagService.CreateAsync(seed.ToCreateDto());
                    createdCustomerTags++;
                    continue;
                }

                if (!seed.Matches(tag))
                    await customerTagService.UpdateAsync(tag.Id, seed.ToUpdateDto(tag.Id));

                if (tag.CreateBy != auditUser.Id || tag.CreateName != auditUser.Username)
                {
                    // 标签没有补写创建审计的公开入口，只修复完整稳定键命中的受管记录。
                    tag.CreateBy = auditUser.Id;
                    tag.CreateName = auditUser.Username;
                }

                reusedCustomerTags++;
            }

            var managedCompanies = await context.Companies
                .Where(company => companyCodes.Contains(company.Code))
                .ToDictionaryAsync(company => company.Code, StringComparer.Ordinal, cancellationToken);
            var managedCustomerTags = await context.CustomerTags
                .Where(tag => customerTagCodes.Contains(tag.Code))
                .ToDictionaryAsync(tag => tag.Code, StringComparer.Ordinal, cancellationToken);
            var existingCustomers = await context.Customers
                .Include(customer => customer.TagRelations)
                .Where(customer => customerCodes.Contains(customer.Code))
                .ToDictionaryAsync(customer => customer.Code, StringComparer.Ordinal, cancellationToken);

            foreach (var seed in customerSeeds)
            {
                var companyId = GetManagedReferenceId(managedCompanies, seed.CompanyCode, "公司");
                var customerTagId = GetManagedReferenceId(managedCustomerTags, seed.CustomerTagCode, "客户标签");
                if (!existingCustomers.TryGetValue(seed.Code, out var customer))
                {
                    await customerService.CreateAsync(seed.ToCreateDto(companyId, customerTagId));
                    createdCustomers++;
                    continue;
                }

                if (!seed.Matches(customer, companyId, customerTagId))
                    await customerService.UpdateAsync(customer.Id, seed.ToUpdateDto(customer.Id, companyId, customerTagId));

                if (customer.CreateBy != auditUser.Id || customer.CreateName != auditUser.Username)
                {
                    // 客户创建审计没有补写入口，仅修复完整稳定编码对应的受管记录。
                    customer.CreateBy = auditUser.Id;
                    customer.CreateName = auditUser.Username;
                }

                reusedCustomers++;
            }

            var managedCustomers = await context.Customers
                .Include(customer => customer.TagRelations)
                .Where(customer => customerCodes.Contains(customer.Code))
                .ToDictionaryAsync(customer => customer.Code, StringComparer.Ordinal, cancellationToken);

            foreach (var seed in customerSubAccountSeeds)
            {
                var companyId = GetManagedReferenceId(managedCompanies, seed.CompanyCode, "公司");
                var customerId = GetManagedReferenceId(managedCustomers, seed.CustomerCode, "客户");
                if (!existingCustomerSubAccounts.TryGetValue(seed.Username, out var subAccount))
                {
                    await customerSubAccountService.CreateAsync(seed.ToCreateDto(companyId, customerId));
                    createdCustomerSubAccounts++;
                    continue;
                }

                if (!seed.Matches(subAccount, companyId, customerId))
                {
                    await customerSubAccountService.UpdateAsync(
                        subAccount.Id,
                        seed.ToUpdateDto(subAccount.Id, companyId, customerId));
                }

                if (subAccount.CreateBy != auditUser.Id || subAccount.CreateName != auditUser.Username)
                {
                    // 客户子账号没有补写创建审计的公开入口，仅修复完整稳定账号对应的受管记录。
                    subAccount.CreateBy = auditUser.Id;
                    subAccount.CreateName = auditUser.Username;
                }

                reusedCustomerSubAccounts++;
            }

            foreach (var seed in carrierSeeds)
            {
                if (!existingCarriers.TryGetValue(seed.Code, out var carrier))
                {
                    await carrierService.CreateAsync(seed.ToCreateDto());
                    createdCarriers++;
                    continue;
                }

                if (!seed.Matches(carrier))
                    await carrierService.UpdateAsync(carrier.Id, seed.ToUpdateDto(carrier.Id));

                if (carrier.CreateBy != auditUser.Id || carrier.CreateName != auditUser.Username)
                {
                    // 承运商创建审计没有公开补写入口，仅修复完整稳定编码命中的受管记录。
                    carrier.CreateBy = auditUser.Id;
                    carrier.CreateName = auditUser.Username;
                }

                reusedCarriers++;
            }

            var managedCarriers = await context.Carriers
                .Where(carrier => carrierCodes.Contains(carrier.Code))
                .ToDictionaryAsync(carrier => carrier.Code, StringComparer.Ordinal, cancellationToken);
            foreach (var seed in driverSeeds)
            {
                var carrierId = GetManagedReferenceId(managedCarriers, seed.CarrierCode, "承运商");
                if (!existingDrivers.TryGetValue(seed.Code, out var driver))
                {
                    await driverService.CreateAsync(seed.ToCreateDto(carrierId));
                    createdDrivers++;
                    continue;
                }

                if (!seed.Matches(driver, carrierId))
                    await driverService.UpdateAsync(driver.Id, seed.ToUpdateDto(driver.Id, carrierId));

                if (driver.CreateBy != auditUser.Id || driver.CreateName != auditUser.Username)
                {
                    // 司机创建审计没有公开补写入口，仅修复完整稳定编码命中的受管记录。
                    driver.CreateBy = auditUser.Id;
                    driver.CreateName = auditUser.Username;
                }

                reusedDrivers++;
            }

            foreach (var seed in deliveryRouteSeeds)
            {
                var customerId = GetManagedReferenceId(managedCustomers, seed.CustomerCode, "客户");
                if (!existingDeliveryRoutes.TryGetValue(seed.Code, out var route))
                {
                    await deliveryRouteService.CreateAsync(seed.ToCreateDto(customerId));
                    createdDeliveryRoutes++;
                    continue;
                }

                if (!seed.Matches(route, customerId))
                    await deliveryRouteService.UpdateAsync(route.Id, seed.ToUpdateDto(route.Id, customerId));

                if (route.CreateBy != auditUser.Id || route.CreateName != auditUser.Username)
                {
                    // 路线创建审计没有公开补写入口，仅修复完整稳定编码命中的受管记录。
                    route.CreateBy = auditUser.Id;
                    route.CreateName = auditUser.Username;
                }

                reusedDeliveryRoutes++;
            }

            var managedDeliveryRoutes = await context.DeliveryRoutes
                .Where(route => deliveryRouteCodes.Contains(route.Code))
                .ToDictionaryAsync(route => route.Code, StringComparer.Ordinal, cancellationToken);
            var managedDeliveryRouteIds = managedDeliveryRoutes.Values.Select(route => route.Id).ToArray();
            var managedCustomerRoutes = await context.CustomerRoutes
                .Where(relation => managedDeliveryRouteIds.Contains(relation.RouteId))
                .ToListAsync(cancellationToken);
            foreach (var seed in deliveryRouteSeeds)
            {
                var route = GetManagedReference(managedDeliveryRoutes, seed.Code, "配送路线");
                var customerId = GetManagedReferenceId(managedCustomers, seed.CustomerCode, "客户");
                var relation = managedCustomerRoutes.Single(item => item.RouteId == route.Id && item.CustomerId == customerId);

                // 路线服务的客户集合接口没有暴露关系排序和创建审计；仅对受管路线的精确关系补齐这两个业务字段。
                relation.Sort = seed.Sort;
                relation.CreateBy = auditUser.Id;
                relation.CreateName = auditUser.Username;
            }

            var existingGoodsTypes = await context.GoodsTypes
                .Where(goodsType => goodsTypeCodes.Contains(goodsType.Code))
                .ToDictionaryAsync(goodsType => goodsType.Code, StringComparer.Ordinal, cancellationToken);
            foreach (var seed in goodsTypeSeeds)
            {
                if (!existingGoodsTypes.TryGetValue(seed.Code, out var goodsType))
                {
                    await goodsTypeService.CreateAsync(seed.ToCreateDto());
                    createdGoodsTypes++;
                    continue;
                }

                if (!seed.Matches(goodsType))
                    await goodsTypeService.UpdateAsync(goodsType.Id, seed.ToUpdateDto(goodsType.Id));

                if (goodsType.CreateBy != auditUser.Id || goodsType.CreateName != auditUser.Username)
                {
                    // 商品分类的创建审计没有公开补写入口，仅修复完整稳定编码对应的受管记录。
                    goodsType.CreateBy = auditUser.Id;
                    goodsType.CreateName = auditUser.Username;
                }

                reusedGoodsTypes++;
            }

            var managedGoodsTypes = await context.GoodsTypes
                .Where(goodsType => goodsTypeCodes.Contains(goodsType.Code))
                .ToDictionaryAsync(goodsType => goodsType.Code, StringComparer.Ordinal, cancellationToken);

            foreach (var seed in supplierSeeds)
            {
                if (!existingSuppliers.TryGetValue(seed.Code, out var supplier))
                {
                    await supplierService.CreateAsync(seed.ToCreateDto());
                    createdSuppliers++;
                    continue;
                }

                if (!seed.Matches(supplier))
                    await supplierService.UpdateAsync(supplier.Id, seed.ToUpdateDto(supplier.Id));

                if (supplier.CreateBy != auditUser.Id || supplier.CreateName != auditUser.Username)
                {
                    // 供应商创建审计没有公开补写入口，仅修复完整稳定编码对应的受管记录。
                    supplier.CreateBy = auditUser.Id;
                    supplier.CreateName = auditUser.Username;
                }

                reusedSuppliers++;
            }

            var managedSuppliers = await context.Suppliers
                .Where(supplier => supplierCodes.Contains(supplier.Code))
                .ToDictionaryAsync(supplier => supplier.Code, StringComparer.Ordinal, cancellationToken);

            foreach (var seed in purchaserSeeds)
            {
                var user = organizationalUsers[(seed.Sequence - 1) % organizationalUsers.Count];
                var department = organizationalDepartments[(seed.Sequence - 1) % organizationalDepartments.Count];
                if (!existingPurchasers.TryGetValue(seed.Code, out var purchaser))
                {
                    await purchaserService.CreateAsync(seed.ToCreateDto(user.Id, department.Id));
                    createdPurchasers++;
                    continue;
                }

                if (!seed.Matches(purchaser, user.Id, department.Id))
                    await purchaserService.UpdateAsync(purchaser.Id, seed.ToUpdateDto(purchaser.Id, user.Id, department.Id));

                if (purchaser.CreateBy != auditUser.Id || purchaser.CreateName != auditUser.Username)
                {
                    // 采购员创建审计没有公开补写入口，仅修复完整稳定编码对应的受管记录。
                    purchaser.CreateBy = auditUser.Id;
                    purchaser.CreateName = auditUser.Username;
                }

                reusedPurchasers++;
            }

            foreach (var seed in wareSeeds)
            {
                if (!existingWares.TryGetValue(seed.Code, out var ware))
                {
                    await wareService.CreateAsync(seed.ToCreateDto());
                    createdWares++;
                    continue;
                }

                if (!seed.Matches(ware))
                    await wareService.UpdateAsync(ware.Id, seed.ToUpdateDto(ware.Id));

                if (ware.CreateBy != auditUser.Id || ware.CreateName != auditUser.Username)
                {
                    // 仓库创建审计没有公开补写入口，仅修复完整稳定编码对应的受管记录。
                    ware.CreateBy = auditUser.Id;
                    ware.CreateName = auditUser.Username;
                }

                reusedWares++;
            }

            var managedPurchasers = await context.Purchasers
                .Where(purchaser => purchaserCodes.Contains(purchaser.Code))
                .ToDictionaryAsync(purchaser => purchaser.Code, StringComparer.Ordinal, cancellationToken);

            var managedWares = await context.Wares
                .Where(ware => wareCodes.Contains(ware.Code))
                .ToDictionaryAsync(ware => ware.Code, StringComparer.Ordinal, cancellationToken);
            foreach (var seed in goodsSeeds)
            {
                var goodsTypeId = GetManagedReferenceId(managedGoodsTypes, seed.GoodsTypeCode, "商品分类");
                var supplierId = GetManagedReferenceId(managedSuppliers, seed.SupplierCode, "供应商");
                var wareId = GetManagedReferenceId(managedWares, seed.WareCode, "仓库");
                if (!existingGoods.TryGetValue(seed.Code, out var goods))
                {
                    await goodsService.CreateAsync(seed.ToCreateDto(goodsTypeId, supplierId, wareId));
                    createdGoods++;
                    continue;
                }

                if (!seed.Matches(goods, goodsTypeId, supplierId, wareId))
                    await goodsService.UpdateAsync(goods.Id, seed.ToUpdateDto(goods.Id, goodsTypeId, supplierId, wareId));

                if (goods.CreateBy != auditUser.Id || goods.CreateName != auditUser.Username)
                {
                    // 商品创建审计没有公开补写入口，仅修复完整稳定编码对应的受管记录。
                    goods.CreateBy = auditUser.Id;
                    goods.CreateName = auditUser.Username;
                }

                reusedGoods++;
            }

            var managedGoods = await context.Goods
                .Where(goods => goodsCodes.Contains(goods.Code))
                .ToDictionaryAsync(goods => goods.Code, StringComparer.Ordinal, cancellationToken);
            foreach (var seed in goodsSeeds)
            {
                var goodsId = GetManagedReferenceId(managedGoods, seed.Code, "商品");
                if (!existingGoodsUnits.TryGetValue(seed.UnitCode, out var unit))
                {
                    await goodsUnitService.CreateAsync(seed.ToCreateGoodsUnitDto(goodsId));
                    createdGoodsUnits++;
                    continue;
                }

                if (!seed.Matches(unit, goodsId))
                    await goodsUnitService.UpdateAsync(unit.Id, seed.ToUpdateGoodsUnitDto(unit.Id, goodsId));

                if (unit.CreateBy != auditUser.Id || unit.CreateName != auditUser.Username)
                {
                    // 商品单位创建审计没有公开补写入口，仅修复完整稳定编码对应的受管记录。
                    unit.CreateBy = auditUser.Id;
                    unit.CreateName = auditUser.Username;
                }

                reusedGoodsUnits++;
            }

            var managedGoodsUnits = await context.GoodsUnits
                .Where(unit => goodsUnitCodes.Contains(unit.Code))
                .ToDictionaryAsync(unit => unit.Code!, StringComparer.Ordinal, cancellationToken);
            foreach (var seed in quotationSeeds)
            {
                var customer = GetManagedReference(managedCustomers, seed.CustomerCode, "客户");
                var goods = GetManagedReference(managedGoods, seed.GoodsCode, "商品");
                var goodsUnit = GetManagedReference(managedGoodsUnits, seed.GoodsUnitCode, "商品单位");
                Guid quotationId;
                if (!existingQuotations.TryGetValue(seed.Code, out var quotation))
                {
                    quotationId = (await quotationService.CreateAsync(seed.ToCreateDto(customer.Id))).Id;
                    createdQuotations++;
                }
                else
                {
                    if (!seed.Matches(quotation))
                    {
                        await quotationService.UpdateAsync(quotation.Id, seed.ToUpdateDto(quotation.Id, customer.Id));
                    }

                    if (quotation.CreateBy != auditUser.Id || quotation.CreateName != auditUser.Username)
                    {
                        // 报价创建审计没有公开补写入口，仅修复完整稳定编码对应的受管记录。
                        quotation.CreateBy = auditUser.Id;
                        quotation.CreateName = auditUser.Username;
                    }

                    quotationId = quotation.Id;
                    reusedQuotations++;
                }

                var customerQuotation = await context.CustomerQuotations.SingleAsync(
                    relation => relation.CustomerId == customer.Id && relation.QuotationId == quotationId,
                    cancellationToken);
                // 当前报价维护接口只维护客户绑定；稳定受管关系的默认标志和有效期由生成器精确补齐。
                customerQuotation.IsDefault = seed.IsAudited;
                customerQuotation.EffectiveStart = seed.EffectiveStart;
                customerQuotation.EffectiveEnd = seed.EffectiveEnd;

                // 客户服务未提供默认报价和仓库的批量写入入口；仅对完整稳定编码命中的已加载受管客户在同一事务中精确补齐。
                customer.QuotationId = seed.IsAudited ? quotationId : null;
                customer.DefaultWareId = goods.DefaultWareId;
                customer.UpdateBy = auditUser.Id;
                customer.UpdateName = auditUser.Username;

                var quotationGoods = await context.QuotationGoods.SingleOrDefaultAsync(
                    item => item.QuotationId == quotationId
                            && item.GoodsId == goods.Id
                            && item.GoodsUnitId == goodsUnit.Id,
                    cancellationToken);
                if (quotationGoods is null)
                {
                    await quotationGoodsService.CreateAsync(seed.ToCreateGoodsDto(quotationId, goods.Id, goodsUnit.Id));
                    createdQuotationGoods++;
                    continue;
                }

                if (!seed.Matches(quotationGoods))
                {
                    await quotationGoodsService.UpdateAsync(
                        quotationGoods.Id,
                        seed.ToUpdateGoodsDto(quotationGoods.Id, quotationId, goods.Id, goodsUnit.Id));
                }

                if (quotationGoods.CreateBy != auditUser.Id || quotationGoods.CreateName != auditUser.Username)
                {
                    // 报价商品创建审计没有公开补写入口，仅修复完整稳定编码对应的受管记录。
                    quotationGoods.CreateBy = auditUser.Id;
                    quotationGoods.CreateName = auditUser.Username;
                }

                reusedQuotationGoods++;
            }

            var managedQuotations = await context.Quotations
                .Where(quotation => quotationCodes.Contains(quotation.Code))
                .ToDictionaryAsync(quotation => quotation.Code, StringComparer.Ordinal, cancellationToken);
            foreach (var seed in customerProtocolSeeds)
            {
                var quotation = GetManagedReference(managedQuotations, seed.QuotationCode, "报价单");
                var customer = GetManagedReference(managedCustomers, seed.CustomerCode, "客户");
                var goods = GetManagedReference(managedGoods, seed.GoodsCode, "商品");
                var goodsUnit = GetManagedReference(managedGoodsUnits, seed.GoodsUnitCode, "商品单位");
                Guid customerProtocolId;
                if (!existingCustomerProtocols.TryGetValue(seed.Code, out var customerProtocol))
                {
                    customerProtocolId = (await customerProtocolService.CreateAsync(
                        seed.ToCreateDto(quotation.Id, customer.Id))).Id;
                    createdCustomerProtocols++;
                }
                else
                {
                    if (!seed.Matches(customerProtocol, quotation.Id, customer.Id))
                    {
                        await customerProtocolService.UpdateAsync(
                            customerProtocol.Id,
                            seed.ToUpdateDto(customerProtocol.Id, quotation.Id, customer.Id));
                    }

                    if (customerProtocol.CreateBy != auditUser.Id || customerProtocol.CreateName != auditUser.Username)
                    {
                        // 客户协议价创建审计没有公开补写入口，仅修复完整稳定编码对应的受管记录。
                        customerProtocol.CreateBy = auditUser.Id;
                        customerProtocol.CreateName = auditUser.Username;
                    }

                    customerProtocolId = customerProtocol.Id;
                    reusedCustomerProtocols++;
                }

                var customerProtocolGoods = await context.CustomerProtocolGoods.SingleOrDefaultAsync(
                    item => item.CustomerProtocolId == customerProtocolId
                            && item.GoodsId == goods.Id
                            && item.GoodsUnitId == goodsUnit.Id,
                    cancellationToken);
                if (customerProtocolGoods is null)
                {
                    await customerProtocolGoodsService.CreateAsync(
                        seed.ToCreateGoodsDto(customerProtocolId, goods.Id, goodsUnit.Id));
                    createdCustomerProtocolGoods++;
                    continue;
                }

                if (!seed.Matches(customerProtocolGoods))
                {
                    await customerProtocolGoodsService.UpdateAsync(
                        customerProtocolGoods.Id,
                        seed.ToUpdateGoodsDto(customerProtocolGoods.Id, customerProtocolId, goods.Id, goodsUnit.Id));
                }

                if (customerProtocolGoods.CreateBy != auditUser.Id || customerProtocolGoods.CreateName != auditUser.Username)
                {
                    // 客户协议价商品创建审计没有公开补写入口，仅修复完整稳定编码对应的受管记录。
                    customerProtocolGoods.CreateBy = auditUser.Id;
                    customerProtocolGoods.CreateName = auditUser.Username;
                }

                reusedCustomerProtocolGoods++;
            }

            foreach (var seed in purchaseRuleSeeds)
            {
                var supplierId = GetManagedReferenceId(managedSuppliers, seed.SupplierCode, "供应商");
                var purchaserId = GetManagedReferenceId(managedPurchasers, seed.PurchaserCode, "采购员");
                var wareId = GetManagedReferenceId(managedWares, seed.WareCode, "仓库");
                var goodsTypeId = GetManagedReferenceId(managedGoodsTypes, seed.GoodsTypeCode, "商品分类");
                var goodsId = GetManagedReferenceId(managedGoods, seed.GoodsCode, "商品");
                var customerId = GetManagedReferenceId(managedCustomers, seed.CustomerCode, "客户");
                if (!existingPurchaseRules.TryGetValue(seed.Code, out var purchaseRule))
                {
                    await purchaseRuleService.CreateAsync(
                        seed.ToCreateDto(supplierId, purchaserId, wareId, goodsTypeId, goodsId, customerId));
                    createdPurchaseRules++;
                    continue;
                }

                if (!seed.Matches(purchaseRule, supplierId, purchaserId, wareId, goodsTypeId, goodsId, customerId))
                {
                    await purchaseRuleService.UpdateAsync(
                        purchaseRule.Id,
                        seed.ToUpdateDto(purchaseRule.Id, supplierId, purchaserId, wareId, goodsTypeId, goodsId, customerId));
                }

                if (purchaseRule.CreateBy != auditUser.Id || purchaseRule.CreateName != auditUser.Username)
                {
                    // 采购规则没有补写创建审计的公开入口，仅修复完整稳定编码对应的受管记录。
                    purchaseRule.CreateBy = auditUser.Id;
                    purchaseRule.CreateName = auditUser.Username;
                }

                reusedPurchaseRules++;
            }

            await systemSupportService.SaveMiniProgramOrderSettingsAsync(new MiniProgramOrderSettingsDto
            {
                IsEnabled = true,
                MaxAdvanceOrderDays = 7
            });
            await systemSupportService.SaveSortingWeightSettingsAsync(new SortingWeightSettingsDto
            {
                OrderTimeWeight = 1.2500m,
                RouteWeight = 0.7500m,
                CustomerWeight = 0.5000m
            });

            foreach (var seed in servicePeriodSeeds)
            {
                if (!existingServicePeriods.TryGetValue(seed.Name, out var servicePeriod))
                {
                    await systemSupportService.CreateServicePeriodAsync(seed.ToUpsertDto());
                    createdServicePeriods++;
                    continue;
                }

                if (!seed.Matches(servicePeriod))
                    await systemSupportService.UpdateServicePeriodAsync(servicePeriod.Id, seed.ToUpsertDto());

                if (servicePeriod.CreateBy != auditUser.Id || servicePeriod.CreateName != auditUser.Username)
                {
                    servicePeriod.CreateBy = auditUser.Id;
                    servicePeriod.CreateName = auditUser.Username;
                }

                reusedServicePeriods++;
            }

            foreach (var seed in noticeSeeds)
            {
                Guid noticeId;
                if (!existingNotices.TryGetValue(seed.Title, out var notice))
                {
                    noticeId = (await systemSupportService.CreateNoticeAsync(seed.ToUpsertDto())).Id;
                    createdNotices++;
                }
                else
                {
                    if (!seed.Matches(notice))
                        await systemSupportService.UpdateNoticeAsync(notice.Id, seed.ToUpsertDto());

                    if (notice.CreateBy != auditUser.Id || notice.CreateName != auditUser.Username)
                    {
                        notice.CreateBy = auditUser.Id;
                        notice.CreateName = auditUser.Username;
                    }

                    noticeId = notice.Id;
                    reusedNotices++;
                }

                var refreshedNotice = await context.Notices.SingleAsync(item => item.Id == noticeId, cancellationToken);
                if (refreshedNotice.NoticeStatus != seed.NoticeStatus)
                {
                    await systemSupportService.UpdateNoticeStatusAsync(
                        refreshedNotice.Id,
                        new UpdateNoticeStatusDto { NoticeStatus = seed.NoticeStatus });
                }

                refreshedNotice.PublishedTime = seed.NoticeStatus == NoticeStatus.Published
                    ? seed.PublishedTime
                    : null;
            }

            foreach (var seed in printTemplateSeeds)
            {
                if (!existingPrintTemplates.TryGetValue(seed.TemplateCode, out var printTemplate))
                {
                    await printService.CreateTemplateAsync(seed.ToCreateDto());
                    createdPrintTemplates++;
                    continue;
                }

                var replacedPrintTemplateFields = false;
                if (!seed.Matches(printTemplate))
                {
                    printTemplate.TemplateCode = seed.TemplateCode;
                    printTemplate.Name = seed.Name;
                    printTemplate.BusinessType = seed.BusinessType;
                    printTemplate.DesignJson = seed.DesignJson;
                    printTemplate.IsEnabled = seed.IsEnabled;
                    printTemplate.UpdateBy = auditUser.Id;
                    printTemplate.UpdateName = auditUser.Username;
                    context.PrintTemplateFields.RemoveRange(printTemplate.Fields);
                    await context.SaveChangesAsync(cancellationToken);
                    foreach (var field in seed.Fields.OrderBy(field => field.DisplayOrder))
                    {
                        context.PrintTemplateFields.Add(field.ToEntity(printTemplate.Id, auditUser));
                    }

                    replacedPrintTemplateFields = true;
                }

                if (printTemplate.CreateBy != auditUser.Id || printTemplate.CreateName != auditUser.Username)
                {
                    printTemplate.CreateBy = auditUser.Id;
                    printTemplate.CreateName = auditUser.Username;
                }

                if (!replacedPrintTemplateFields)
                {
                    foreach (var field in printTemplate.Fields)
                    {
                        if (field.CreateBy != auditUser.Id || field.CreateName != auditUser.Username)
                        {
                            field.CreateBy = auditUser.Id;
                            field.CreateName = auditUser.Username;
                        }
                    }
                }

                reusedPrintTemplates++;
            }

            var managedSystemUsersBySequence = await context.Users
                .Where(user => systemUsernames.Contains(user.Username))
                .OrderBy(user => user.Username)
                .Select(user => new DemoOrganizationalUser(user.Id, user.Username))
                .ToListAsync(cancellationToken);
            foreach (var seed in operationLogSeeds)
            {
                var user = managedSystemUsersBySequence[(seed.Sequence - 1) % managedSystemUsersBySequence.Count];
                if (!existingOperationLogs.TryGetValue(seed.Description, out var operationLog))
                {
                    context.OperationLogs.Add(seed.ToEntity(user, auditUser));
                    createdOperationLogs++;
                    continue;
                }

                seed.Apply(operationLog, user, auditUser);
                reusedOperationLogs++;
            }

            foreach (var seed in loginLogSeeds)
            {
                var user = managedSystemUsersBySequence[(seed.Sequence - 1) % managedSystemUsersBySequence.Count];
                if (!existingLoginLogs.TryGetValue(seed.Username, out var loginLog))
                {
                    context.LoginLogs.Add(seed.ToEntity(user, auditUser));
                    createdLoginLogs++;
                    continue;
                }

                seed.Apply(loginLog, user, auditUser);
                reusedLoginLogs++;
            }

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        (
            createdSaleOrders,
            reusedSaleOrders,
            createdSaleOrderDetails,
            reusedSaleOrderDetails) = await GenerateSaleOrdersAsync(
            context,
            saleOrderService,
            saleOrderSeeds,
            auditUser,
            cancellationToken);

        (
            createdPurchasePlans,
            reusedPurchasePlans,
            createdPurchasePlanDetails,
            reusedPurchasePlanDetails,
            createdPurchasePlanOrderRelations,
            reusedPurchasePlanOrderRelations) = await GeneratePurchasePlansAsync(
            context,
            purchasePlanService,
            auditUser,
            cancellationToken);

        (
            createdPurchaseOrders,
            reusedPurchaseOrders,
            createdPurchaseOrderDetails,
            reusedPurchaseOrderDetails,
            createdPurchaseOrderPlanRelations,
            reusedPurchaseOrderPlanRelations) = await GeneratePurchaseOrdersAsync(
            context,
            purchaseOrderService,
            auditUser,
            cancellationToken);

        (
            createdPurchaseStockIns,
            reusedPurchaseStockIns,
            createdPurchaseStockInDetails,
            reusedPurchaseStockInDetails,
            createdStockBatches,
            reusedStockBatches,
            createdStockLedgers,
            reusedStockLedgers,
            createdSupplierBills,
            reusedSupplierBills,
            createdSupplierBillDetails,
            reusedSupplierBillDetails) = await GeneratePurchaseStockInsAsync(
            context,
            stockInService,
            auditUser,
            cancellationToken);

        (
            createdSaleStockSupportIns,
            reusedSaleStockSupportIns,
            createdSaleStockSupportInDetails,
            reusedSaleStockSupportInDetails,
            createdSaleStockSupportBatches,
            reusedSaleStockSupportBatches,
            createdSaleStockSupportLedgers,
            reusedSaleStockSupportLedgers,
            createdSaleStockOuts,
            reusedSaleStockOuts,
            createdSaleStockOutDetails,
            reusedSaleStockOutDetails,
            createdSaleStockOutLedgers,
            reusedSaleStockOutLedgers) = await GenerateSaleStockOutsAsync(
            context,
            stockInService,
            stockOutService,
            auditUser,
            cancellationToken);

        (
            createdDeliveryTasks,
            reusedDeliveryTasks,
            createdOrderReceipts,
            reusedOrderReceipts,
            createdOrderCheckDetails,
            reusedOrderCheckDetails,
            createdCustomerBills,
            reusedCustomerBills,
            createdCustomerBillDetails,
            reusedCustomerBillDetails) = await GenerateDeliveryTasksAsync(
            context,
            deliveryTaskService,
            auditUser,
            cancellationToken);

        var afterSaleResult = await GenerateAfterSalesAsync(
            context,
            afterSaleService,
            pickupTaskService,
            stockInService,
            auditUser,
            createdCustomerBillDetails,
            reusedCustomerBillDetails,
            cancellationToken);
        createdAfterSales = afterSaleResult.AfterSales.Created;
        reusedAfterSales = afterSaleResult.AfterSales.Reused;
        createdAfterSaleGoods = afterSaleResult.AfterSaleGoods.Created;
        reusedAfterSaleGoods = afterSaleResult.AfterSaleGoods.Reused;
        createdAfterSaleAuditLogs = afterSaleResult.AfterSaleAuditLogs.Created;
        reusedAfterSaleAuditLogs = afterSaleResult.AfterSaleAuditLogs.Reused;
        createdPickupTasks = afterSaleResult.PickupTasks.Created;
        reusedPickupTasks = afterSaleResult.PickupTasks.Reused;
        createdSalesReturnStockIns = afterSaleResult.SalesReturnStockIns.Created;
        reusedSalesReturnStockIns = afterSaleResult.SalesReturnStockIns.Reused;
        createdSalesReturnStockInDetails = afterSaleResult.SalesReturnStockInDetails.Created;
        reusedSalesReturnStockInDetails = afterSaleResult.SalesReturnStockInDetails.Reused;
        createdCustomerBillDetails = afterSaleResult.CustomerBillDetails.Created;
        reusedCustomerBillDetails = afterSaleResult.CustomerBillDetails.Reused;
        createdStockBatches += afterSaleResult.SalesReturnBatches.Created;
        reusedStockBatches += afterSaleResult.SalesReturnBatches.Reused;
        createdStockLedgers += afterSaleResult.SalesReturnLedgers.Created;
        reusedStockLedgers += afterSaleResult.SalesReturnLedgers.Reused;

        var customerSettlementResult = await new DemoDataCustomerSettlementBuilder(
                context,
                customerSettlementService)
            .GenerateAsync(cancellationToken);
        createdCustomerSettlements = customerSettlementResult.CreatedSettlements;
        reusedCustomerSettlements = customerSettlementResult.ReusedSettlements;
        createdCustomerSettlementDetails = customerSettlementResult.CreatedDetails;
        reusedCustomerSettlementDetails = customerSettlementResult.ReusedDetails;

        var supplierSettlementResult = await new DemoDataSupplierSettlementBuilder(
                context,
                supplierSettlementService)
            .GenerateAsync(cancellationToken);
        createdSupplierSettlements = supplierSettlementResult.CreatedSettlements;
        reusedSupplierSettlements = supplierSettlementResult.ReusedSettlements;
        createdSupplierSettlementDetails = supplierSettlementResult.CreatedDetails;
        reusedSupplierSettlementDetails = supplierSettlementResult.ReusedDetails;

        var traceabilityResult = await new DemoDataTraceabilityBuilder(
                context,
                traceabilityService,
                saleOrderService,
                stockOutService,
                auditUser.Id,
                auditUser.Username)
            .GenerateAsync(cancellationToken);
        createdInspectionReports = traceabilityResult.CreatedInspectionReports;
        reusedInspectionReports = traceabilityResult.ReusedInspectionReports;
        createdInspectionReportGoods = traceabilityResult.CreatedInspectionReportGoods;
        reusedInspectionReportGoods = traceabilityResult.ReusedInspectionReportGoods;
        createdInspectionAttachments = traceabilityResult.CreatedInspectionAttachments;
        reusedInspectionAttachments = traceabilityResult.ReusedInspectionAttachments;
        createdTraceSaleOrders = traceabilityResult.CreatedTraceSaleOrders;
        reusedTraceSaleOrders = traceabilityResult.ReusedTraceSaleOrders;
        createdTraceSaleOrderDetails = traceabilityResult.CreatedTraceSaleOrderDetails;
        reusedTraceSaleOrderDetails = traceabilityResult.ReusedTraceSaleOrderDetails;
        createdTraceSaleOrderAuditLogs = traceabilityResult.CreatedTraceSaleOrderAuditLogs;
        reusedTraceSaleOrderAuditLogs = traceabilityResult.ReusedTraceSaleOrderAuditLogs;
        createdTraceStockOuts = traceabilityResult.CreatedTraceStockOuts;
        reusedTraceStockOuts = traceabilityResult.ReusedTraceStockOuts;
        createdTraceStockOutDetails = traceabilityResult.CreatedTraceStockOutDetails;
        reusedTraceStockOutDetails = traceabilityResult.ReusedTraceStockOutDetails;
        createdTraceStockOutLedgers = traceabilityResult.CreatedTraceStockOutLedgers;
        reusedTraceStockOutLedgers = traceabilityResult.ReusedTraceStockOutLedgers;
        createdTraceRecords = traceabilityResult.CreatedTraceRecords;
        reusedTraceRecords = traceabilityResult.ReusedTraceRecords;

        var fileResult = await new DemoDataFileBuilder(
                context,
                fileStorageService,
                auditUser.Id,
                auditUser.Username)
            .GenerateAsync(cancellationToken);
        createdStoredFiles = fileResult.CreatedStoredFiles;
        reusedStoredFiles = fileResult.ReusedStoredFiles;
        createdGoodsImages = fileResult.CreatedGoodsImages;
        reusedGoodsImages = fileResult.ReusedGoodsImages;

        var importExportJobResult = await new DemoDataImportExportJobBuilder(
                context,
                auditUser.Id,
                auditUser.Username)
            .GenerateAsync(cancellationToken);
        createdImportExportJobs = importExportJobResult.CreatedJobs;
        reusedImportExportJobs = importExportJobResult.ReusedJobs;

        return new DemoDataGenerationResult(
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                [CompaniesLayer] = createdCompanies,
                [AfterSaleAuditLogsLayer] = createdAfterSaleAuditLogs,
                [AfterSaleGoodsLayer] = createdAfterSaleGoods,
                [AfterSalesLayer] = createdAfterSales,
                [CarriersLayer] = createdCarriers,
                [CustomerTagsLayer] = createdCustomerTags,
                [CustomerProtocolGoodsLayer] = createdCustomerProtocolGoods,
                [CustomerProtocolsLayer] = createdCustomerProtocols,
                [CustomerSubAccountsLayer] = createdCustomerSubAccounts,
                [CustomerBillDetailsLayer] = createdCustomerBillDetails,
                [CustomerBillsLayer] = createdCustomerBills,
                [CustomerSettlementDetailsLayer] = createdCustomerSettlementDetails,
                [CustomerSettlementsLayer] = createdCustomerSettlements,
                [CustomersLayer] = createdCustomers,
                [DepartmentsLayer] = createdDepartments,
                [DeliveryTasksLayer] = createdDeliveryTasks,
                [DeliveryRoutesLayer] = createdDeliveryRoutes,
                [DriversLayer] = createdDrivers,
                [GoodsLayer] = createdGoods,
                [GoodsImagesLayer] = createdGoodsImages,
                [GoodsUnitsLayer] = createdGoodsUnits,
                [GoodsTypesLayer] = createdGoodsTypes,
                [InspectionAttachmentsLayer] = createdInspectionAttachments,
                [InspectionReportGoodsLayer] = createdInspectionReportGoods,
                [InspectionReportsLayer] = createdInspectionReports,
                [ImportExportJobsLayer] = createdImportExportJobs,
                [PickupTasksLayer] = createdPickupTasks,
                [PurchasersLayer] = createdPurchasers,
                [PurchasePlanDetailsLayer] = createdPurchasePlanDetails,
                [PurchasePlanOrderRelationsLayer] = createdPurchasePlanOrderRelations,
                [PurchasePlansLayer] = createdPurchasePlans,
                [PurchaseOrderDetailsLayer] = createdPurchaseOrderDetails,
                [PurchaseOrderPlanRelationsLayer] = createdPurchaseOrderPlanRelations,
                [PurchaseOrdersLayer] = createdPurchaseOrders,
                [PurchaseStockInDetailsLayer] = createdPurchaseStockInDetails,
                [PurchaseStockInsLayer] = createdPurchaseStockIns,
                [PurchaseRulesLayer] = createdPurchaseRules,
                [SaleStockOutDetailsLayer] = createdSaleStockOutDetails,
                [SaleStockOutLedgersLayer] = createdSaleStockOutLedgers,
                [SaleStockOutsLayer] = createdSaleStockOuts,
                [SaleStockSupportBatchesLayer] = createdSaleStockSupportBatches,
                [SaleStockSupportInDetailsLayer] = createdSaleStockSupportInDetails,
                [SaleStockSupportInsLayer] = createdSaleStockSupportIns,
                [SaleStockSupportLedgersLayer] = createdSaleStockSupportLedgers,
                [SalesReturnStockInDetailsLayer] = createdSalesReturnStockInDetails,
                [SalesReturnStockInsLayer] = createdSalesReturnStockIns,
                [SaleOrderDetailsLayer] = createdSaleOrderDetails,
                [SaleOrdersLayer] = createdSaleOrders,
                [OrderCheckDetailsLayer] = createdOrderCheckDetails,
                [OrderReceiptsLayer] = createdOrderReceipts,
                [ServicePeriodsLayer] = createdServicePeriods,
                [NoticesLayer] = createdNotices,
                [PrintTemplatesLayer] = createdPrintTemplates,
                [OperationLogsLayer] = createdOperationLogs,
                [LoginLogsLayer] = createdLoginLogs,
                [QuotationGoodsLayer] = createdQuotationGoods,
                [QuotationsLayer] = createdQuotations,
                [SuppliersLayer] = createdSuppliers,
                [SupplierBillDetailsLayer] = createdSupplierBillDetails,
                [SupplierBillsLayer] = createdSupplierBills,
                [SupplierSettlementDetailsLayer] = createdSupplierSettlementDetails,
                [SupplierSettlementsLayer] = createdSupplierSettlements,
                [StockBatchesLayer] = createdStockBatches,
                [StockLedgersLayer] = createdStockLedgers,
                [StoredFilesLayer] = createdStoredFiles,
                [SystemRolesLayer] = createdSystemRoles,
                [SystemUsersLayer] = createdSystemUsers,
                [TraceRecordsLayer] = createdTraceRecords,
                [TraceSaleOrderAuditLogsLayer] = createdTraceSaleOrderAuditLogs,
                [TraceSaleOrderDetailsLayer] = createdTraceSaleOrderDetails,
                [TraceSaleOrdersLayer] = createdTraceSaleOrders,
                [TraceStockOutDetailsLayer] = createdTraceStockOutDetails,
                [TraceStockOutLedgersLayer] = createdTraceStockOutLedgers,
                [TraceStockOutsLayer] = createdTraceStockOuts,
                [WaresLayer] = createdWares
            },
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                [CompaniesLayer] = reusedCompanies,
                [AfterSaleAuditLogsLayer] = reusedAfterSaleAuditLogs,
                [AfterSaleGoodsLayer] = reusedAfterSaleGoods,
                [AfterSalesLayer] = reusedAfterSales,
                [CarriersLayer] = reusedCarriers,
                [CustomerTagsLayer] = reusedCustomerTags,
                [CustomerProtocolGoodsLayer] = reusedCustomerProtocolGoods,
                [CustomerProtocolsLayer] = reusedCustomerProtocols,
                [CustomerSubAccountsLayer] = reusedCustomerSubAccounts,
                [CustomerBillDetailsLayer] = reusedCustomerBillDetails,
                [CustomerBillsLayer] = reusedCustomerBills,
                [CustomerSettlementDetailsLayer] = reusedCustomerSettlementDetails,
                [CustomerSettlementsLayer] = reusedCustomerSettlements,
                [CustomersLayer] = reusedCustomers,
                [DepartmentsLayer] = reusedDepartments,
                [DeliveryTasksLayer] = reusedDeliveryTasks,
                [DeliveryRoutesLayer] = reusedDeliveryRoutes,
                [DriversLayer] = reusedDrivers,
                [GoodsLayer] = reusedGoods,
                [GoodsImagesLayer] = reusedGoodsImages,
                [GoodsUnitsLayer] = reusedGoodsUnits,
                [GoodsTypesLayer] = reusedGoodsTypes,
                [InspectionAttachmentsLayer] = reusedInspectionAttachments,
                [InspectionReportGoodsLayer] = reusedInspectionReportGoods,
                [InspectionReportsLayer] = reusedInspectionReports,
                [ImportExportJobsLayer] = reusedImportExportJobs,
                [PickupTasksLayer] = reusedPickupTasks,
                [PurchasersLayer] = reusedPurchasers,
                [PurchasePlanDetailsLayer] = reusedPurchasePlanDetails,
                [PurchasePlanOrderRelationsLayer] = reusedPurchasePlanOrderRelations,
                [PurchasePlansLayer] = reusedPurchasePlans,
                [PurchaseOrderDetailsLayer] = reusedPurchaseOrderDetails,
                [PurchaseOrderPlanRelationsLayer] = reusedPurchaseOrderPlanRelations,
                [PurchaseOrdersLayer] = reusedPurchaseOrders,
                [PurchaseStockInDetailsLayer] = reusedPurchaseStockInDetails,
                [PurchaseStockInsLayer] = reusedPurchaseStockIns,
                [PurchaseRulesLayer] = reusedPurchaseRules,
                [SaleStockOutDetailsLayer] = reusedSaleStockOutDetails,
                [SaleStockOutLedgersLayer] = reusedSaleStockOutLedgers,
                [SaleStockOutsLayer] = reusedSaleStockOuts,
                [SaleStockSupportBatchesLayer] = reusedSaleStockSupportBatches,
                [SaleStockSupportInDetailsLayer] = reusedSaleStockSupportInDetails,
                [SaleStockSupportInsLayer] = reusedSaleStockSupportIns,
                [SaleStockSupportLedgersLayer] = reusedSaleStockSupportLedgers,
                [SalesReturnStockInDetailsLayer] = reusedSalesReturnStockInDetails,
                [SalesReturnStockInsLayer] = reusedSalesReturnStockIns,
                [SaleOrderDetailsLayer] = reusedSaleOrderDetails,
                [SaleOrdersLayer] = reusedSaleOrders,
                [OrderCheckDetailsLayer] = reusedOrderCheckDetails,
                [OrderReceiptsLayer] = reusedOrderReceipts,
                [ServicePeriodsLayer] = reusedServicePeriods,
                [NoticesLayer] = reusedNotices,
                [PrintTemplatesLayer] = reusedPrintTemplates,
                [OperationLogsLayer] = reusedOperationLogs,
                [LoginLogsLayer] = reusedLoginLogs,
                [QuotationGoodsLayer] = reusedQuotationGoods,
                [QuotationsLayer] = reusedQuotations,
                [SuppliersLayer] = reusedSuppliers,
                [SupplierBillDetailsLayer] = reusedSupplierBillDetails,
                [SupplierBillsLayer] = reusedSupplierBills,
                [SupplierSettlementDetailsLayer] = reusedSupplierSettlementDetails,
                [SupplierSettlementsLayer] = reusedSupplierSettlements,
                [StockBatchesLayer] = reusedStockBatches,
                [StockLedgersLayer] = reusedStockLedgers,
                [StoredFilesLayer] = reusedStoredFiles,
                [SystemRolesLayer] = reusedSystemRoles,
                [SystemUsersLayer] = reusedSystemUsers,
                [TraceRecordsLayer] = reusedTraceRecords,
                [TraceSaleOrderAuditLogsLayer] = reusedTraceSaleOrderAuditLogs,
                [TraceSaleOrderDetailsLayer] = reusedTraceSaleOrderDetails,
                [TraceSaleOrdersLayer] = reusedTraceSaleOrders,
                [TraceStockOutDetailsLayer] = reusedTraceStockOutDetails,
                [TraceStockOutLedgersLayer] = reusedTraceStockOutLedgers,
                [TraceStockOutsLayer] = reusedTraceStockOuts,
                [WaresLayer] = reusedWares
            });
    }

    private static async Task<(int CreatedOrders, int ReusedOrders, int CreatedDetails, int ReusedDetails)> GenerateSaleOrdersAsync(
        ApplicationDbContext context,
        ISaleOrderService saleOrderService,
        IReadOnlyList<SaleOrderSeed> seeds,
        DemoAuditUser auditUser,
        CancellationToken cancellationToken)
    {
        var stableKeys = seeds.Select(seed => seed.StableKey).ToArray();
        var customerCodes = seeds.Select(seed => seed.CustomerCode).Distinct().ToArray();
        var quotationCodes = seeds.Select(seed => seed.QuotationCode).Distinct().ToArray();
        var wareCodes = seeds.Select(seed => seed.WareCode).Distinct().ToArray();
        var goodsCodes = seeds.SelectMany(seed => seed.Details.Select(detail => detail.GoodsCode)).Distinct().ToArray();
        var goodsUnitCodes = seeds.SelectMany(seed => seed.Details.Select(detail => detail.GoodsUnitCode)).Distinct().ToArray();
        var existingOrders = await context.SaleOrders
            .Include(order => order.Details)
            .Include(order => order.AuditLogs)
            .Where(order => order.InnerRemark != null && stableKeys.Contains(order.InnerRemark))
            .ToDictionaryAsync(order => order.InnerRemark!, StringComparer.Ordinal, cancellationToken);
        var managedCustomers = await context.Customers
            .Where(customer => customerCodes.Contains(customer.Code))
            .ToDictionaryAsync(customer => customer.Code, StringComparer.Ordinal, cancellationToken);
        var managedQuotations = await context.Quotations
            .Where(quotation => quotationCodes.Contains(quotation.Code))
            .ToDictionaryAsync(quotation => quotation.Code, StringComparer.Ordinal, cancellationToken);
        var managedWares = await context.Wares
            .Where(ware => wareCodes.Contains(ware.Code))
            .ToDictionaryAsync(ware => ware.Code, StringComparer.Ordinal, cancellationToken);
        var managedGoods = await context.Goods
            .Where(goods => goodsCodes.Contains(goods.Code))
            .ToDictionaryAsync(goods => goods.Code, StringComparer.Ordinal, cancellationToken);
        var managedGoodsUnits = await context.GoodsUnits
            .Where(unit => unit.Code != null && goodsUnitCodes.Contains(unit.Code))
            .ToDictionaryAsync(unit => unit.Code!, StringComparer.Ordinal, cancellationToken);

        var createdOrders = 0;
        var reusedOrders = 0;
        var createdDetails = 0;
        var reusedDetails = 0;

        foreach (var seed in seeds)
        {
            var customer = GetManagedReference(managedCustomers, seed.CustomerCode, "客户");
            var quotation = GetManagedReference(managedQuotations, seed.QuotationCode, "报价单");
            var ware = GetManagedReference(managedWares, seed.WareCode, "仓库");

            if (!existingOrders.TryGetValue(seed.StableKey, out var order))
            {
                var created = await saleOrderService.CreateAsync(seed.ToCreateDto(
                    customer.Id,
                    quotation.Id,
                    ware.Id,
                    managedGoods,
                    managedGoodsUnits));
                await ApplySaleOrderTargetStatusAsync(
                    context,
                    saleOrderService,
                    created.Id,
                    seed.TargetStatus,
                    seed.AuditRemark,
                    auditUser,
                    cancellationToken);
                createdOrders++;
                createdDetails += seed.Details.Count;
                continue;
            }

            if (!seed.Matches(order, customer.Id, quotation.Id, ware.Id))
            {
                // 销售订单服务不允许编辑已审核或已驳回订单；仅对完整稳定内部备注命中的受管订单校准可维护快照字段。
                order.CustomerId = customer.Id;
                order.QuotationId = quotation.Id;
                order.WareId = ware.Id;
                order.OrderDate = seed.OrderDate;
                order.ReceiveDate = seed.ReceiveDate;
                order.ContactNameSnapshot = seed.ContactName;
                order.ContactPhoneSnapshot = seed.ContactPhone;
                order.DeliveryAddressSnapshot = seed.DeliveryAddress;
                order.Remark = seed.Remark;
                order.UpdateBy = auditUser.Id;
                order.UpdateName = auditUser.Username;
            }

            await ApplySaleOrderTargetStatusAsync(
                context,
                saleOrderService,
                order.Id,
                seed.TargetStatus,
                seed.AuditRemark,
                auditUser,
                cancellationToken);
            reusedOrders++;
            reusedDetails += order.Details.Count;
        }

        await context.SaveChangesAsync(cancellationToken);
        return (createdOrders, reusedOrders, createdDetails, reusedDetails);
    }

    private static async Task<(
        int CreatedPlans,
        int ReusedPlans,
        int CreatedDetails,
        int ReusedDetails,
        int CreatedOrderRelations,
        int ReusedOrderRelations)> GeneratePurchasePlansAsync(
            ApplicationDbContext context,
            IPurchasePlanService purchasePlanService,
            DemoAuditUser auditUser,
            CancellationToken cancellationToken)
    {
        var saleOrderKeys = Enumerable.Range(1, 60)
            .Select(sequence => DemoDataStableKeyCatalog.Create("SALE-ORDER", sequence))
            .ToArray();
        var planRemarks = Enumerable.Range(1, 40)
            .Select(CreatePurchasePlanRemark)
            .ToArray();
        var supplierCodes = Enumerable.Range(1, 30)
            .Select(sequence => DemoDataStableKeyCatalog.Create("SUPPLIER", sequence))
            .ToArray();
        var purchaserCodes = Enumerable.Range(1, 30)
            .Select(sequence => DemoDataStableKeyCatalog.Create("PURCHASER", sequence))
            .ToArray();

        var approvedOrders = await context.SaleOrders
            .Include(order => order.Details)
            .Where(order => order.InnerRemark != null
                            && saleOrderKeys.Contains(order.InnerRemark)
                            && order.OrderStatus != SaleOrderStatus.PendingAudit
                            && order.OrderStatus != SaleOrderStatus.Rejected)
            .OrderBy(order => order.InnerRemark)
            .ToListAsync(cancellationToken);
        if (approvedOrders.Count != 40)
        {
            throw new InvalidOperationException(
                $"受管采购计划生成需要 40 张已审核销售订单，当前为 {approvedOrders.Count} 张。");
        }

        var existingPlans = await context.PurchasePlans
            .Include(plan => plan.Details)
            .ThenInclude(detail => detail.OrderRelations)
            .Where(plan => plan.Remark != null && planRemarks.Contains(plan.Remark))
            .ToDictionaryAsync(plan => plan.Remark!, StringComparer.Ordinal, cancellationToken);
        var managedSuppliers = await context.Suppliers
            .Where(supplier => supplierCodes.Contains(supplier.Code))
            .ToDictionaryAsync(supplier => supplier.Code, StringComparer.Ordinal, cancellationToken);
        var managedPurchasers = await context.Purchasers
            .Where(purchaser => purchaserCodes.Contains(purchaser.Code))
            .ToDictionaryAsync(purchaser => purchaser.Code, StringComparer.Ordinal, cancellationToken);

        var createdPlans = 0;
        var reusedPlans = 0;
        var createdDetails = 0;
        var reusedDetails = 0;
        var createdOrderRelations = 0;
        var reusedOrderRelations = 0;

        for (var index = 0; index < approvedOrders.Count; index++)
        {
            var sequence = index + 1;
            var order = approvedOrders[index];
            var planRemark = CreatePurchasePlanRemark(sequence);
            var referenceSequence = (sequence - 1) % 30 + 1;
            var supplier = GetManagedReference(
                managedSuppliers,
                DemoDataStableKeyCatalog.Create("SUPPLIER", referenceSequence),
                "供应商");
            var purchaser = GetManagedReference(
                managedPurchasers,
                DemoDataStableKeyCatalog.Create("PURCHASER", referenceSequence),
                "采购员");

            PurchasePlan plan;
            if (!existingPlans.TryGetValue(planRemark, out var existingPlan))
            {
                if (order.HasPurchasePlan)
                {
                    throw new InvalidOperationException(
                        $"受管销售订单 {order.InnerRemark} 已标记生成采购计划，但未找到受管采购计划 {planRemark}。");
                }

                var created = await purchasePlanService.GenerateFromOrdersAsync(new GeneratePurchasePlanFromOrdersDto
                {
                    OrderIds = [order.Id],
                    Remark = planRemark
                });
                var createdPlanId = created.Single().Id;
                await purchasePlanService.AssignSupplierAsync(new AssignPurchasePlanSupplierDto
                {
                    PlanIds = [createdPlanId],
                    SupplierId = supplier.Id
                });
                await purchasePlanService.AssignPurchaserAsync(new AssignPurchasePlanPurchaserDto
                {
                    PlanIds = [createdPlanId],
                    PurchaserId = purchaser.Id
                });

                plan = await GetManagedPurchasePlanAsync(context, createdPlanId, cancellationToken);
                createdPlans++;
                createdDetails += plan.Details.Count;
                createdOrderRelations += plan.Details.Sum(detail => detail.OrderRelations.Count);
            }
            else
            {
                plan = existingPlan;
                if (plan.SupplierId != supplier.Id)
                {
                    await purchasePlanService.AssignSupplierAsync(new AssignPurchasePlanSupplierDto
                    {
                        PlanIds = [plan.Id],
                        SupplierId = supplier.Id
                    });
                }

                if (plan.PurchaserId != purchaser.Id)
                {
                    await purchasePlanService.AssignPurchaserAsync(new AssignPurchasePlanPurchaserDto
                    {
                        PlanIds = [plan.Id],
                        PurchaserId = purchaser.Id
                    });
                }

                if (!order.HasPurchasePlan)
                {
                    // 只校准完整稳定键订单的生成标记，避免服务重复生成已存在的受管采购计划。
                    order.HasPurchasePlan = true;
                    order.UpdateBy = auditUser.Id;
                    order.UpdateName = auditUser.Username;
                    foreach (var detail in order.Details)
                    {
                        detail.HasPurchasePlan = true;
                    }
                }

                plan = await GetManagedPurchasePlanAsync(context, plan.Id, cancellationToken);
                reusedPlans++;
                reusedDetails += plan.Details.Count;
                reusedOrderRelations += plan.Details.Sum(detail => detail.OrderRelations.Count);
            }

            ApplyManagedPurchasePlanFields(plan, sequence, auditUser);
        }

        await context.SaveChangesAsync(cancellationToken);
        return (createdPlans, reusedPlans, createdDetails, reusedDetails, createdOrderRelations, reusedOrderRelations);
    }

    private static async Task<PurchasePlan> GetManagedPurchasePlanAsync(
        ApplicationDbContext context,
        Guid planId,
        CancellationToken cancellationToken)
    {
        return await context.PurchasePlans
            .Include(plan => plan.Details)
            .ThenInclude(detail => detail.OrderRelations)
            .SingleAsync(plan => plan.Id == planId, cancellationToken);
    }

    private static void ApplyManagedPurchasePlanFields(PurchasePlan plan, int sequence, DemoAuditUser auditUser)
    {
        plan.Remark = CreatePurchasePlanRemark(sequence);
        if (plan.CreateBy != auditUser.Id || plan.CreateName != auditUser.Username)
        {
            plan.CreateBy = auditUser.Id;
            plan.CreateName = auditUser.Username;
        }

        foreach (var detail in plan.Details.OrderBy(detail => detail.GoodsCodeSnapshot, StringComparer.Ordinal))
        {
            detail.Remark =
                $"SkyRoc 联调采购计划明细：{plan.Remark} 中商品 {detail.GoodsCodeSnapshot} 的订单需求按基础单位汇总。";
            if (detail.CreateBy != auditUser.Id || detail.CreateName != auditUser.Username)
            {
                detail.CreateBy = auditUser.Id;
                detail.CreateName = auditUser.Username;
            }

            foreach (var relation in detail.OrderRelations)
            {
                if (relation.CreateBy == auditUser.Id && relation.CreateName == auditUser.Username)
                    continue;

                relation.CreateBy = auditUser.Id;
                relation.CreateName = auditUser.Username;
            }
        }
    }

    private static string CreatePurchasePlanRemark(int sequence)
    {
        var stableKey = DemoDataStableKeyCatalog.Create("PURCHASE-PLAN", sequence);
        return $"{stableKey} 华东联调采购计划{sequence:D2}：由已审核销售订单生成，用于采购单、入库和供应商结算链路。";
    }

    private static async Task<(
        int CreatedOrders,
        int ReusedOrders,
        int CreatedDetails,
        int ReusedDetails,
        int CreatedPlanRelations,
        int ReusedPlanRelations)> GeneratePurchaseOrdersAsync(
            ApplicationDbContext context,
            IPurchaseOrderService purchaseOrderService,
            DemoAuditUser auditUser,
            CancellationToken cancellationToken)
    {
        var orderRemarks = Enumerable.Range(1, 50)
            .Select(CreatePurchaseOrderRemark)
            .ToArray();
        var planRemarks = Enumerable.Range(1, 40)
            .Select(CreatePurchasePlanRemark)
            .ToArray();
        var supplierCodes = Enumerable.Range(1, 30)
            .Select(sequence => DemoDataStableKeyCatalog.Create("SUPPLIER", sequence))
            .ToArray();
        var purchaserCodes = Enumerable.Range(1, 30)
            .Select(sequence => DemoDataStableKeyCatalog.Create("PURCHASER", sequence))
            .ToArray();
        var goodsCodes = Enumerable.Range(1, 30)
            .Select(sequence => DemoDataStableKeyCatalog.Create("GOODS", sequence))
            .ToArray();
        var goodsUnitCodes = Enumerable.Range(1, 30)
            .Select(sequence => DemoDataStableKeyCatalog.Create("GOODS-UNIT", sequence))
            .ToArray();

        var existingOrders = await context.PurchaseOrders
            .Include(order => order.Supplier)
            .Include(order => order.Purchaser)
            .Include(order => order.Details)
            .ThenInclude(detail => detail.Goods)
            .Include(order => order.Details)
            .ThenInclude(detail => detail.PurchaseUnit)
            .Include(order => order.Details)
            .ThenInclude(detail => detail.PlanRelations)
            .ThenInclude(relation => relation.PurchasePlanDetail)
            .ThenInclude(detail => detail.PurchasePlan)
            .Where(order => order.Remark != null && orderRemarks.Contains(order.Remark))
            .ToDictionaryAsync(order => order.Remark!, StringComparer.Ordinal, cancellationToken);
        var managedPlans = await context.PurchasePlans
            .Include(plan => plan.Details)
            .Where(plan => plan.Remark != null && planRemarks.Contains(plan.Remark))
            .ToDictionaryAsync(plan => plan.Remark!, StringComparer.Ordinal, cancellationToken);
        var managedSuppliers = await context.Suppliers
            .Where(supplier => supplierCodes.Contains(supplier.Code))
            .ToDictionaryAsync(supplier => supplier.Code, StringComparer.Ordinal, cancellationToken);
        var managedPurchasers = await context.Purchasers
            .Where(purchaser => purchaserCodes.Contains(purchaser.Code))
            .ToDictionaryAsync(purchaser => purchaser.Code, StringComparer.Ordinal, cancellationToken);
        var managedGoods = await context.Goods
            .Where(goods => goodsCodes.Contains(goods.Code))
            .ToDictionaryAsync(goods => goods.Code, StringComparer.Ordinal, cancellationToken);
        var managedGoodsUnits = await context.GoodsUnits
            .Where(unit => unit.Code != null && goodsUnitCodes.Contains(unit.Code))
            .ToDictionaryAsync(unit => unit.Code!, StringComparer.Ordinal, cancellationToken);

        var createdOrders = 0;
        var reusedOrders = 0;
        var createdDetails = 0;
        var reusedDetails = 0;
        var createdPlanRelations = 0;
        var reusedPlanRelations = 0;

        for (var sequence = 1; sequence <= 40; sequence++)
        {
            var orderRemark = CreatePurchaseOrderRemark(sequence);
            PurchaseOrder order;
            if (!existingOrders.TryGetValue(orderRemark, out var existingOrder))
            {
                var plan = GetManagedReference(managedPlans, CreatePurchasePlanRemark(sequence), "采购计划");
                if (plan.PurchaseStatus != PurchasePlanStatus.Unpublished)
                {
                    throw new InvalidOperationException(
                        $"受管采购计划 {plan.Remark} 已生成采购单，但未找到受管采购单 {orderRemark}。");
                }

                var created = await purchaseOrderService.GenerateFromPlansAsync(new GeneratePurchaseOrdersFromPlansDto
                {
                    PlanIds = [plan.Id],
                    ReceiveTime = CreatePurchaseOrderReceiveTime(sequence),
                    Remark = orderRemark
                });
                order = await GetManagedPurchaseOrderAsync(context, created.Single().Id, cancellationToken);
                await purchaseOrderService.UpdateAsync(CreateManagedPurchaseOrderUpdateDto(order, sequence));
                await purchaseOrderService.CompleteAsync(order.Id);
                order = await GetManagedPurchaseOrderAsync(context, order.Id, cancellationToken);
                ApplyManagedPurchaseOrderFields(order, sequence, auditUser);
                createdOrders++;
                createdDetails += order.Details.Count;
                createdPlanRelations += order.Details.Sum(detail => detail.PlanRelations.Count);
                continue;
            }

            order = existingOrder;
            if (order.BusinessStatus == PurchaseOrderStatus.Draft)
            {
                await purchaseOrderService.UpdateAsync(CreateManagedPurchaseOrderUpdateDto(order, sequence));
                await purchaseOrderService.CompleteAsync(order.Id);
                order = await GetManagedPurchaseOrderAsync(context, order.Id, cancellationToken);
            }

            ApplyManagedPurchaseOrderFields(order, sequence, auditUser);
            reusedOrders++;
            reusedDetails += order.Details.Count;
            reusedPlanRelations += order.Details.Sum(detail => detail.PlanRelations.Count);
        }

        for (var sequence = 41; sequence <= 50; sequence++)
        {
            var orderRemark = CreatePurchaseOrderRemark(sequence);
            PurchaseOrder order;
            if (!existingOrders.TryGetValue(orderRemark, out var existingOrder))
            {
                var referenceSequence = (sequence - 1) % 30 + 1;
                var supplier = GetManagedReference(
                    managedSuppliers,
                    DemoDataStableKeyCatalog.Create("SUPPLIER", referenceSequence),
                    "供应商");
                var purchaser = GetManagedReference(
                    managedPurchasers,
                    DemoDataStableKeyCatalog.Create("PURCHASER", referenceSequence),
                    "采购员");
                var created = await purchaseOrderService.CreateAsync(CreateManualPurchaseOrderDto(
                    sequence,
                    supplier.Id,
                    purchaser.Id,
                    managedGoods,
                    managedGoodsUnits));
                order = await GetManagedPurchaseOrderAsync(context, created.Id, cancellationToken);
                if (sequence > 45)
                {
                    await purchaseOrderService.CancelAsync(order.Id);
                    order = await GetManagedPurchaseOrderAsync(context, order.Id, cancellationToken);
                }

                ApplyManagedPurchaseOrderFields(order, sequence, auditUser);
                createdOrders++;
                createdDetails += order.Details.Count;
                createdPlanRelations += order.Details.Sum(detail => detail.PlanRelations.Count);
                continue;
            }

            order = existingOrder;
            if (order.BusinessStatus == PurchaseOrderStatus.Draft && sequence > 45)
            {
                await purchaseOrderService.CancelAsync(order.Id);
                order = await GetManagedPurchaseOrderAsync(context, order.Id, cancellationToken);
            }
            else if (order.BusinessStatus == PurchaseOrderStatus.Draft)
            {
                await purchaseOrderService.UpdateAsync(CreateManagedPurchaseOrderUpdateDto(order, sequence));
                order = await GetManagedPurchaseOrderAsync(context, order.Id, cancellationToken);
            }

            ApplyManagedPurchaseOrderFields(order, sequence, auditUser);
            reusedOrders++;
            reusedDetails += order.Details.Count;
            reusedPlanRelations += order.Details.Sum(detail => detail.PlanRelations.Count);
        }

        await context.SaveChangesAsync(cancellationToken);
        return (createdOrders, reusedOrders, createdDetails, reusedDetails, createdPlanRelations, reusedPlanRelations);
    }

    private static async Task<PurchaseOrder> GetManagedPurchaseOrderAsync(
        ApplicationDbContext context,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        return await context.PurchaseOrders
            .Include(order => order.Supplier)
            .Include(order => order.Purchaser)
            .Include(order => order.Details)
            .ThenInclude(detail => detail.Goods)
            .Include(order => order.Details)
            .ThenInclude(detail => detail.PurchaseUnit)
            .Include(order => order.Details)
            .ThenInclude(detail => detail.PlanRelations)
            .ThenInclude(relation => relation.PurchasePlanDetail)
            .ThenInclude(detail => detail.PurchasePlan)
            .SingleAsync(order => order.Id == orderId, cancellationToken);
    }

    private static UpdatePurchaseOrderDto CreateManagedPurchaseOrderUpdateDto(PurchaseOrder order, int sequence)
    {
        return new UpdatePurchaseOrderDto
        {
            Id = order.Id,
            SupplierId = order.SupplierId,
            PurchaserId = order.PurchaserId,
            PurchasePattern = order.PurchasePattern,
            ReceiveTime = CreatePurchaseOrderReceiveTime(sequence),
            SupplierContactName = order.SupplierContactNameSnapshot,
            SupplierContactPhone = order.SupplierContactPhoneSnapshot,
            Remark = CreatePurchaseOrderRemark(sequence),
            Details = order.Details
                .OrderBy(detail => detail.GoodsCodeSnapshot, StringComparer.Ordinal)
                .Select((detail, detailIndex) => new UpdatePurchaseOrderDetailDto
                {
                    Id = detail.Id,
                    GoodsId = detail.GoodsId,
                    PurchaseUnitId = detail.PurchaseUnitId,
                    RequiredQuantity = detail.RequiredQuantity,
                    PurchaseQuantity = detail.PurchaseQuantity,
                    PurchasePrice = CreatePurchasePrice(sequence, detailIndex),
                    ProductDate = CreatePurchaseProductDate(sequence, detailIndex),
                    Remark = CreatePurchaseOrderDetailRemark(sequence, detail.GoodsCodeSnapshot, detailIndex),
                    PlanAllocations = detail.PlanRelations
                        .Select(relation => new PurchaseOrderPlanAllocationDto
                        {
                            PurchasePlanDetailId = relation.PurchasePlanDetailId,
                            AllocatedQuantity = relation.AllocatedQuantity
                        })
                        .ToList()
                })
                .ToList()
        };
    }

    private static CreatePurchaseOrderDto CreateManualPurchaseOrderDto(
        int sequence,
        Guid supplierId,
        Guid purchaserId,
        IReadOnlyDictionary<string, Domain.Entities.Goods.Goods> managedGoods,
        IReadOnlyDictionary<string, Domain.Entities.Goods.GoodsUnit> managedGoodsUnits)
    {
        var firstGoodsSequence = (sequence - 1) % 30 + 1;
        var secondGoodsSequence = sequence % 30 + 1;
        return new CreatePurchaseOrderDto
        {
            SupplierId = supplierId,
            PurchaserId = purchaserId,
            PurchasePattern = PurchasePattern.SupplierDirect,
            ReceiveTime = CreatePurchaseOrderReceiveTime(sequence),
            SupplierContactName = $"冯采购联系人{sequence:D2}",
            SupplierContactPhone = $"021-6600{sequence:D4}",
            Remark = CreatePurchaseOrderRemark(sequence),
            Details =
            [
                CreateManualPurchaseOrderDetailDto(sequence, 0, firstGoodsSequence, managedGoods, managedGoodsUnits),
                CreateManualPurchaseOrderDetailDto(sequence, 1, secondGoodsSequence, managedGoods, managedGoodsUnits)
            ]
        };
    }

    private static CreatePurchaseOrderDetailDto CreateManualPurchaseOrderDetailDto(
        int orderSequence,
        int detailIndex,
        int goodsSequence,
        IReadOnlyDictionary<string, Domain.Entities.Goods.Goods> managedGoods,
        IReadOnlyDictionary<string, Domain.Entities.Goods.GoodsUnit> managedGoodsUnits)
    {
        var goods = GetManagedReference(
            managedGoods,
            DemoDataStableKeyCatalog.Create("GOODS", goodsSequence),
            "商品");
        var unit = GetManagedReference(
            managedGoodsUnits,
            DemoDataStableKeyCatalog.Create("GOODS-UNIT", goodsSequence),
            "商品单位");
        var quantity = NumericPrecision.RoundQuantity(8m + orderSequence % 6 + detailIndex);
        return new CreatePurchaseOrderDetailDto
        {
            GoodsId = goods.Id,
            PurchaseUnitId = unit.Id,
            RequiredQuantity = quantity,
            PurchaseQuantity = quantity,
            PurchasePrice = CreatePurchasePrice(orderSequence, detailIndex),
            ProductDate = CreatePurchaseProductDate(orderSequence, detailIndex),
            Remark = CreatePurchaseOrderDetailRemark(orderSequence, goods.Code, detailIndex)
        };
    }

    private static void ApplyManagedPurchaseOrderFields(PurchaseOrder order, int sequence, DemoAuditUser auditUser)
    {
        order.Remark = CreatePurchaseOrderRemark(sequence);
        order.ReceiveTime = CreatePurchaseOrderReceiveTime(sequence);
        if (order.CreateBy != auditUser.Id || order.CreateName != auditUser.Username)
        {
            order.CreateBy = auditUser.Id;
            order.CreateName = auditUser.Username;
        }

        var orderedDetails = order.Details
            .OrderBy(detail => detail.GoodsCodeSnapshot, StringComparer.Ordinal)
            .ToArray();
        for (var detailIndex = 0; detailIndex < orderedDetails.Length; detailIndex++)
        {
            var detail = orderedDetails[detailIndex];
            detail.GoodsInfoSnapshot = SerializeGoodsInfo(detail.Goods);
            detail.PurchasePrice = CreatePurchasePrice(sequence, detailIndex);
            detail.PurchaseTotalPrice = NumericPrecision.RoundMoney(detail.PurchaseQuantity * detail.PurchasePrice);
            detail.ProductDate = CreatePurchaseProductDate(sequence, detailIndex);
            detail.Remark = CreatePurchaseOrderDetailRemark(sequence, detail.GoodsCodeSnapshot, detailIndex);
            if (detail.CreateBy != auditUser.Id || detail.CreateName != auditUser.Username)
            {
                detail.CreateBy = auditUser.Id;
                detail.CreateName = auditUser.Username;
            }

            foreach (var relation in detail.PlanRelations)
            {
                if (relation.CreateBy == auditUser.Id && relation.CreateName == auditUser.Username)
                    continue;

                relation.CreateBy = auditUser.Id;
                relation.CreateName = auditUser.Username;
            }
        }
    }

    private static string CreatePurchaseOrderRemark(int sequence)
    {
        var stableKey = DemoDataStableKeyCatalog.Create("PURCHASE-ORDER", sequence);
        var source = sequence <= 40 ? "由受管采购计划生成" : "由手工补货场景创建";
        return $"{stableKey} 华东联调采购单{sequence:D2}：{source}，用于采购入库、库存和供应商结算链路。";
    }

    private static string CreatePurchaseOrderDetailRemark(int sequence, string goodsCode, int detailIndex)
    {
        return $"SkyRoc 联调采购单明细：采购单 {sequence:D2} 第 {detailIndex + 1} 行商品 {goodsCode}，用于后续采购入库与成本核算。";
    }

    private static DateTime CreatePurchaseOrderReceiveTime(int sequence)
    {
        return new DateTime(2026, 8, (sequence - 1) % 28 + 1, 2, 0, 0, DateTimeKind.Utc);
    }

    private static DateOnly CreatePurchaseProductDate(int sequence, int detailIndex)
    {
        return new DateOnly(2026, 7, (sequence + detailIndex - 1) % 28 + 1);
    }

    private static decimal CreatePurchasePrice(int sequence, int detailIndex)
    {
        return NumericPrecision.RoundMoney(6.35m + sequence * 0.18m + detailIndex * 0.42m);
    }

    private static async Task<(
        int CreatedStockIns,
        int ReusedStockIns,
        int CreatedDetails,
        int ReusedDetails,
        int CreatedBatches,
        int ReusedBatches,
        int CreatedLedgers,
        int ReusedLedgers,
        int CreatedSupplierBills,
        int ReusedSupplierBills,
        int CreatedSupplierBillDetails,
        int ReusedSupplierBillDetails)> GeneratePurchaseStockInsAsync(
            ApplicationDbContext context,
            IStockInService stockInService,
            DemoAuditUser auditUser,
            CancellationToken cancellationToken)
    {
        var stockInRemarks = Enumerable.Range(1, 40)
            .Select(CreatePurchaseStockInRemark)
            .ToArray();
        var purchaseOrderRemarks = Enumerable.Range(1, 40)
            .Select(CreatePurchaseOrderRemark)
            .ToArray();
        var wareCodes = Enumerable.Range(1, 30)
            .Select(sequence => DemoDataStableKeyCatalog.Create("WARE", sequence))
            .ToArray();
        var departmentCodes = Enumerable.Range(1, 30)
            .Select(sequence => DemoDataStableKeyCatalog.Create("DEPARTMENT", sequence))
            .ToArray();

        var existingStockIns = await context.StockInOrders
            .Include(order => order.Details)
            .ThenInclude(detail => detail.StockBatch)
            .Where(order => order.Remark != null && stockInRemarks.Contains(order.Remark))
            .ToDictionaryAsync(order => order.Remark!, StringComparer.Ordinal, cancellationToken);
        var managedPurchaseOrders = await context.PurchaseOrders
            .Include(order => order.Details)
            .ThenInclude(detail => detail.Goods)
            .Include(order => order.Details)
            .ThenInclude(detail => detail.PurchaseUnit)
            .Where(order => order.Remark != null && purchaseOrderRemarks.Contains(order.Remark))
            .ToDictionaryAsync(order => order.Remark!, StringComparer.Ordinal, cancellationToken);
        var managedWares = await context.Wares
            .Where(ware => wareCodes.Contains(ware.Code))
            .ToDictionaryAsync(ware => ware.Code, StringComparer.Ordinal, cancellationToken);
        var managedDepartments = await context.Departments
            .Where(department => departmentCodes.Contains(department.Code))
            .ToDictionaryAsync(department => department.Code, StringComparer.Ordinal, cancellationToken);

        var createdStockIns = 0;
        var reusedStockIns = 0;
        var createdDetails = 0;
        var reusedDetails = 0;
        var createdBatches = 0;
        var reusedBatches = 0;
        var createdLedgers = 0;
        var reusedLedgers = 0;
        var createdSupplierBills = 0;
        var reusedSupplierBills = 0;
        var createdSupplierBillDetails = 0;
        var reusedSupplierBillDetails = 0;

        for (var sequence = 1; sequence <= 40; sequence++)
        {
            var stockInRemark = CreatePurchaseStockInRemark(sequence);
            var purchaseOrder = GetManagedReference(
                managedPurchaseOrders,
                CreatePurchaseOrderRemark(sequence),
                "采购单");
            if (purchaseOrder.BusinessStatus != PurchaseOrderStatus.Completed)
            {
                throw new InvalidOperationException(
                    $"受管采购入库 {stockInRemark} 需要已完成采购单，当前采购单状态为 {purchaseOrder.BusinessStatus}。");
            }

            var referenceSequence = (sequence - 1) % 30 + 1;
            var ware = GetManagedReference(
                managedWares,
                DemoDataStableKeyCatalog.Create("WARE", referenceSequence),
                "仓库");
            var department = GetManagedReference(
                managedDepartments,
                DemoDataStableKeyCatalog.Create("DEPARTMENT", referenceSequence),
                "部门");

            StockInOrder stockIn;
            var wasCreated = false;
            if (!existingStockIns.TryGetValue(stockInRemark, out var existingStockIn))
            {
                var created = await stockInService.CreatePurchaseAsync(CreatePurchaseStockInDto(
                    purchaseOrder,
                    ware.Id,
                    department.Id,
                    sequence));
                await stockInService.AuditAsync(
                    StockInOrderType.Purchase,
                    created.Id,
                    CreatePurchaseStockInAuditRemark(sequence));
                stockIn = await GetManagedPurchaseStockInAsync(context, created.Id, cancellationToken);
                wasCreated = true;
            }
            else
            {
                stockIn = existingStockIn;
                if (stockIn.BusinessStatus is StockDocumentStatus.Draft or StockDocumentStatus.PendingAudit)
                {
                    if (stockIn.BusinessStatus == StockDocumentStatus.Draft)
                    {
                        await stockInService.UpdatePurchaseAsync(CreatePurchaseStockInUpdateDto(
                            stockIn,
                            purchaseOrder,
                            ware.Id,
                            department.Id,
                            sequence));
                    }

                    await stockInService.AuditAsync(
                        StockInOrderType.Purchase,
                        stockIn.Id,
                        CreatePurchaseStockInAuditRemark(sequence));
                }
                else if (stockIn.BusinessStatus != StockDocumentStatus.Audited)
                {
                    throw new InvalidOperationException(
                        $"受管采购入库 {stockInRemark} 当前状态为 {stockIn.BusinessStatus}，不能安全复用。");
                }

                stockIn = await GetManagedPurchaseStockInAsync(context, stockIn.Id, cancellationToken);
            }

            ApplyManagedPurchaseStockInFields(stockIn, sequence, auditUser);
            await context.SaveChangesAsync(cancellationToken);

            var stockInDetailIds = stockIn.Details.Select(detail => detail.Id).ToArray();
            var ledgerCount = await context.StockLedgers
                .CountAsync(ledger => ledger.SourceOrderId == stockIn.Id, cancellationToken);
            var supplierBill = await context.SupplierBills
                .Include(bill => bill.Details)
                .SingleOrDefaultAsync(bill => bill.StockInOrderId == stockIn.Id, cancellationToken)
                ?? throw new InvalidOperationException($"受管采购入库 {stockInRemark} 未生成供应商待结单据。");
            var batchCount = stockIn.Details.Count(detail => detail.StockBatchId.HasValue);
            if (ledgerCount != stockInDetailIds.Length)
            {
                throw new InvalidOperationException(
                    $"受管采购入库 {stockInRemark} 库存流水数量 {ledgerCount} 与明细数量 {stockInDetailIds.Length} 不一致。");
            }

            if (wasCreated)
            {
                createdStockIns++;
                createdDetails += stockIn.Details.Count;
                createdBatches += batchCount;
                createdLedgers += ledgerCount;
                createdSupplierBills++;
                createdSupplierBillDetails += supplierBill.Details.Count;
            }
            else
            {
                reusedStockIns++;
                reusedDetails += stockIn.Details.Count;
                reusedBatches += batchCount;
                reusedLedgers += ledgerCount;
                reusedSupplierBills++;
                reusedSupplierBillDetails += supplierBill.Details.Count;
            }
        }

        return (
            createdStockIns,
            reusedStockIns,
            createdDetails,
            reusedDetails,
            createdBatches,
            reusedBatches,
            createdLedgers,
            reusedLedgers,
            createdSupplierBills,
            reusedSupplierBills,
            createdSupplierBillDetails,
            reusedSupplierBillDetails);
    }

    private static async Task<StockInOrder> GetManagedPurchaseStockInAsync(
        ApplicationDbContext context,
        Guid stockInOrderId,
        CancellationToken cancellationToken)
    {
        return await context.StockInOrders
            .Include(order => order.Details)
            .ThenInclude(detail => detail.Goods)
            .Include(order => order.Details)
            .ThenInclude(detail => detail.GoodsUnit)
            .Include(order => order.Details)
            .ThenInclude(detail => detail.StockBatch)
            .SingleAsync(order => order.Id == stockInOrderId, cancellationToken);
    }

    private static CreatePurchaseStockInDto CreatePurchaseStockInDto(
        PurchaseOrder purchaseOrder,
        Guid wareId,
        Guid departmentId,
        int sequence)
    {
        return new CreatePurchaseStockInDto
        {
            WareId = wareId,
            PurchaseOrderId = purchaseOrder.Id,
            SupplierId = purchaseOrder.SupplierId,
            DepartmentId = departmentId,
            PurchaserId = purchaseOrder.PurchaserId,
            PurchasePattern = purchaseOrder.PurchasePattern,
            InTime = CreatePurchaseStockInTime(sequence),
            ExpectedArrivalTime = CreatePurchaseStockInTime(sequence).AddHours(6),
            Remark = CreatePurchaseStockInRemark(sequence),
            Details = purchaseOrder.Details
                .OrderBy(detail => detail.GoodsCodeSnapshot, StringComparer.Ordinal)
                .Select((detail, detailIndex) => CreatePurchaseStockInDetailDto(sequence, detailIndex, detail))
                .ToList()
        };
    }

    private static UpdatePurchaseStockInDto CreatePurchaseStockInUpdateDto(
        StockInOrder stockIn,
        PurchaseOrder purchaseOrder,
        Guid wareId,
        Guid departmentId,
        int sequence)
    {
        var stockInDetailsByPurchaseDetailId = stockIn.Details
            .Where(detail => detail.PurchaseOrderDetailId.HasValue)
            .ToDictionary(detail => detail.PurchaseOrderDetailId!.Value);
        return new UpdatePurchaseStockInDto
        {
            Id = stockIn.Id,
            WareId = wareId,
            PurchaseOrderId = purchaseOrder.Id,
            SupplierId = purchaseOrder.SupplierId,
            DepartmentId = departmentId,
            PurchaserId = purchaseOrder.PurchaserId,
            PurchasePattern = purchaseOrder.PurchasePattern,
            InTime = CreatePurchaseStockInTime(sequence),
            ExpectedArrivalTime = CreatePurchaseStockInTime(sequence).AddHours(6),
            Remark = CreatePurchaseStockInRemark(sequence),
            Details = purchaseOrder.Details
                .OrderBy(detail => detail.GoodsCodeSnapshot, StringComparer.Ordinal)
                .Select((detail, detailIndex) =>
                {
                    var dto = CreatePurchaseStockInDetailDto(sequence, detailIndex, detail);
                    return new UpdateStockInDetailDto
                    {
                        Id = stockInDetailsByPurchaseDetailId.GetValueOrDefault(detail.Id)?.Id,
                        PurchaseOrderDetailId = dto.PurchaseOrderDetailId,
                        GoodsId = dto.GoodsId,
                        GoodsUnitId = dto.GoodsUnitId,
                        Quantity = dto.Quantity,
                        UnitPrice = dto.UnitPrice,
                        BatchNo = dto.BatchNo,
                        ProductDate = dto.ProductDate,
                        ExpireDate = dto.ExpireDate,
                        Remark = dto.Remark
                    };
                })
                .ToList()
        };
    }

    private static CreateStockInDetailDto CreatePurchaseStockInDetailDto(
        int sequence,
        int detailIndex,
        PurchaseOrderDetail detail)
    {
        var productDate = detail.ProductDate ?? CreatePurchaseProductDate(sequence, detailIndex);
        return new CreateStockInDetailDto
        {
            PurchaseOrderDetailId = detail.Id,
            GoodsId = detail.GoodsId,
            GoodsUnitId = detail.PurchaseUnitId,
            Quantity = detail.PurchaseQuantity,
            UnitPrice = detail.PurchasePrice,
            BatchNo = CreatePurchaseStockBatchNo(sequence, detailIndex),
            ProductDate = productDate,
            ExpireDate = productDate.AddDays(10 + detailIndex),
            Remark = CreatePurchaseStockInDetailRemark(sequence, detail.GoodsCodeSnapshot, detailIndex)
        };
    }

    private static void ApplyManagedPurchaseStockInFields(
        StockInOrder stockIn,
        int sequence,
        DemoAuditUser auditUser)
    {
        stockIn.Remark = CreatePurchaseStockInRemark(sequence);
        stockIn.InTime = CreatePurchaseStockInTime(sequence);
        stockIn.ExpectedArrivalTime = CreatePurchaseStockInTime(sequence).AddHours(6);
        if (stockIn.CreateBy != auditUser.Id || stockIn.CreateName != auditUser.Username)
        {
            stockIn.CreateBy = auditUser.Id;
            stockIn.CreateName = auditUser.Username;
        }

        var orderedDetails = stockIn.Details
            .OrderBy(detail => detail.GoodsCodeSnapshot, StringComparer.Ordinal)
            .ToArray();
        for (var detailIndex = 0; detailIndex < orderedDetails.Length; detailIndex++)
        {
            var detail = orderedDetails[detailIndex];
            detail.BatchNo = CreatePurchaseStockBatchNo(sequence, detailIndex);
            detail.Remark = CreatePurchaseStockInDetailRemark(sequence, detail.GoodsCodeSnapshot, detailIndex);
            if (detail.CreateBy != auditUser.Id || detail.CreateName != auditUser.Username)
            {
                detail.CreateBy = auditUser.Id;
                detail.CreateName = auditUser.Username;
            }
        }
    }

    private static string CreatePurchaseStockInRemark(int sequence)
    {
        var stableKey = DemoDataStableKeyCatalog.Create("PURCHASE-STOCK-IN", sequence);
        return $"{stableKey} 华东联调采购入库{sequence:D2}：来源受管采购单，用于库存批次、流水和供应商待结链路。";
    }

    private static string CreatePurchaseStockInDetailRemark(int sequence, string goodsCode, int detailIndex)
    {
        return $"SkyRoc 联调采购入库明细：入库单 {sequence:D2} 第 {detailIndex + 1} 行商品 {goodsCode}，用于形成库存批次和供应商待结。";
    }

    private static string CreatePurchaseStockInAuditRemark(int sequence)
    {
        return $"SkyRoc 联调采购入库审核：确认第 {sequence:D2} 张受管采购单到货并形成库存流水。";
    }

    private static string CreatePurchaseStockBatchNo(int sequence, int detailIndex)
    {
        return $"{DemoDataStableKeyCatalog.Create("PURCHASE-BATCH", sequence)}-{detailIndex + 1:D2}";
    }

    private static DateTime CreatePurchaseStockInTime(int sequence)
    {
        return new DateTime(2026, 8, (sequence - 1) % 28 + 1, 10, 30, 0, DateTimeKind.Utc);
    }

    private static async Task<(
        int CreatedSupportIns,
        int ReusedSupportIns,
        int CreatedSupportDetails,
        int ReusedSupportDetails,
        int CreatedSupportBatches,
        int ReusedSupportBatches,
        int CreatedSupportLedgers,
        int ReusedSupportLedgers,
        int CreatedStockOuts,
        int ReusedStockOuts,
        int CreatedStockOutDetails,
        int ReusedStockOutDetails,
        int CreatedStockOutLedgers,
        int ReusedStockOutLedgers)> GenerateSaleStockOutsAsync(
            ApplicationDbContext context,
            IStockInService stockInService,
            IStockOutService stockOutService,
            DemoAuditUser auditUser,
            CancellationToken cancellationToken)
    {
        var saleOrderKeys = Enumerable.Range(1, 60)
            .Select(sequence => DemoDataStableKeyCatalog.Create("SALE-ORDER", sequence))
            .ToArray();
        var supportInRemarks = Enumerable.Range(1, 40)
            .Select(CreateSaleStockSupportInRemark)
            .ToArray();
        var stockOutRemarks = Enumerable.Range(1, 40)
            .Select(CreateSaleStockOutRemark)
            .ToArray();
        var departmentCodes = Enumerable.Range(1, 30)
            .Select(sequence => DemoDataStableKeyCatalog.Create("DEPARTMENT", sequence))
            .ToArray();

        var approvedOrders = await context.SaleOrders
            .Include(order => order.Details)
            .Where(order => order.InnerRemark != null
                            && saleOrderKeys.Contains(order.InnerRemark)
                            && order.OrderStatus != SaleOrderStatus.PendingAudit
                            && order.OrderStatus != SaleOrderStatus.Rejected)
            .OrderBy(order => order.InnerRemark)
            .ToListAsync(cancellationToken);
        if (approvedOrders.Count != 40)
        {
            throw new InvalidOperationException(
                $"受管销售出库生成需要 40 张已审核销售订单，当前为 {approvedOrders.Count} 张。");
        }

        var existingSupportIns = await context.StockInOrders
            .Include(order => order.Details)
            .ThenInclude(detail => detail.StockBatch)
            .Where(order => order.Remark != null && supportInRemarks.Contains(order.Remark))
            .ToDictionaryAsync(order => order.Remark!, StringComparer.Ordinal, cancellationToken);
        var existingStockOuts = await context.StockOutOrders
            .Include(order => order.Details)
            .Where(order => order.Remark != null && stockOutRemarks.Contains(order.Remark))
            .ToDictionaryAsync(order => order.Remark!, StringComparer.Ordinal, cancellationToken);
        var managedDepartments = await context.Departments
            .Where(department => departmentCodes.Contains(department.Code))
            .ToDictionaryAsync(department => department.Code, StringComparer.Ordinal, cancellationToken);

        var createdSupportIns = 0;
        var reusedSupportIns = 0;
        var createdSupportDetails = 0;
        var reusedSupportDetails = 0;
        var createdSupportBatches = 0;
        var reusedSupportBatches = 0;
        var createdSupportLedgers = 0;
        var reusedSupportLedgers = 0;
        var createdStockOuts = 0;
        var reusedStockOuts = 0;
        var createdStockOutDetails = 0;
        var reusedStockOutDetails = 0;
        var createdStockOutLedgers = 0;
        var reusedStockOutLedgers = 0;

        for (var index = 0; index < approvedOrders.Count; index++)
        {
            var sequence = index + 1;
            var saleOrder = approvedOrders[index];
            if (!saleOrder.WareId.HasValue)
            {
                throw new InvalidOperationException($"受管销售订单 {saleOrder.InnerRemark} 未配置出库仓库。");
            }

            var referenceSequence = (sequence - 1) % 30 + 1;
            var department = GetManagedReference(
                managedDepartments,
                DemoDataStableKeyCatalog.Create("DEPARTMENT", referenceSequence),
                "部门");
            var supportInRemark = CreateSaleStockSupportInRemark(sequence);

            StockInOrder supportIn;
            var supportWasCreated = false;
            if (!existingSupportIns.TryGetValue(supportInRemark, out var existingSupportIn))
            {
                var created = await stockInService.CreateOtherAsync(CreateSaleStockSupportInDto(
                    saleOrder,
                    department.Id,
                    sequence));
                await stockInService.AuditAsync(
                    StockInOrderType.Other,
                    created.Id,
                    CreateSaleStockSupportInAuditRemark(sequence));
                supportIn = await GetManagedSaleStockSupportInAsync(context, created.Id, cancellationToken);
                supportWasCreated = true;
            }
            else
            {
                supportIn = existingSupportIn;
                if (supportIn.BusinessStatus is StockDocumentStatus.Draft or StockDocumentStatus.PendingAudit)
                {
                    if (supportIn.BusinessStatus == StockDocumentStatus.Draft)
                    {
                        await stockInService.UpdateOtherAsync(CreateSaleStockSupportInUpdateDto(
                            supportIn,
                            saleOrder,
                            department.Id,
                            sequence));
                    }

                    await stockInService.AuditAsync(
                        StockInOrderType.Other,
                        supportIn.Id,
                        CreateSaleStockSupportInAuditRemark(sequence));
                }
                else if (supportIn.BusinessStatus != StockDocumentStatus.Audited)
                {
                    throw new InvalidOperationException(
                        $"受管销售出库支撑入库 {supportInRemark} 当前状态为 {supportIn.BusinessStatus}，不能安全复用。");
                }

                supportIn = await GetManagedSaleStockSupportInAsync(context, supportIn.Id, cancellationToken);
            }

            ApplyManagedSaleStockSupportInFields(supportIn, saleOrder, sequence, auditUser);
            await context.SaveChangesAsync(cancellationToken);

            var supportLedgerCount = await context.StockLedgers
                .CountAsync(ledger => ledger.SourceOrderId == supportIn.Id, cancellationToken);
            var supportBatchCount = supportIn.Details.Count(detail => detail.StockBatchId.HasValue);
            if (supportLedgerCount != supportIn.Details.Count)
            {
                throw new InvalidOperationException(
                    $"受管销售出库支撑入库 {supportInRemark} 库存流水数量 {supportLedgerCount} 与明细数量 {supportIn.Details.Count} 不一致。");
            }

            if (supportWasCreated)
            {
                createdSupportIns++;
                createdSupportDetails += supportIn.Details.Count;
                createdSupportBatches += supportBatchCount;
                createdSupportLedgers += supportLedgerCount;
            }
            else
            {
                reusedSupportIns++;
                reusedSupportDetails += supportIn.Details.Count;
                reusedSupportBatches += supportBatchCount;
                reusedSupportLedgers += supportLedgerCount;
            }

            var stockOutRemark = CreateSaleStockOutRemark(sequence);
            StockOutOrder stockOut;
            var stockOutWasCreated = false;
            if (!existingStockOuts.TryGetValue(stockOutRemark, out var existingStockOut))
            {
                var created = await stockOutService.CreateSaleAsync(CreateSaleStockOutDto(
                    saleOrder,
                    supportIn,
                    department.Id,
                    sequence));
                await stockOutService.AuditAsync(
                    StockOutOrderType.Sale,
                    created.Id,
                    CreateSaleStockOutAuditRemark(sequence));
                stockOut = await GetManagedSaleStockOutAsync(context, created.Id, cancellationToken);
                stockOutWasCreated = true;
            }
            else
            {
                stockOut = existingStockOut;
                if (stockOut.BusinessStatus is StockDocumentStatus.Draft or StockDocumentStatus.PendingAudit)
                {
                    if (stockOut.BusinessStatus == StockDocumentStatus.Draft)
                    {
                        await stockOutService.UpdateSaleAsync(CreateSaleStockOutUpdateDto(
                            stockOut,
                            saleOrder,
                            supportIn,
                            department.Id,
                            sequence));
                    }

                    await stockOutService.AuditAsync(
                        StockOutOrderType.Sale,
                        stockOut.Id,
                        CreateSaleStockOutAuditRemark(sequence));
                }
                else if (stockOut.BusinessStatus != StockDocumentStatus.Audited)
                {
                    throw new InvalidOperationException(
                        $"受管销售出库 {stockOutRemark} 当前状态为 {stockOut.BusinessStatus}，不能安全复用。");
                }

                stockOut = await GetManagedSaleStockOutAsync(context, stockOut.Id, cancellationToken);
            }

            ApplyManagedSaleStockOutFields(stockOut, sequence, auditUser);
            await context.SaveChangesAsync(cancellationToken);

            var stockOutLedgerCount = await context.StockLedgers
                .CountAsync(ledger => ledger.SourceOrderId == stockOut.Id, cancellationToken);
            if (stockOutLedgerCount != stockOut.Details.Count)
            {
                throw new InvalidOperationException(
                    $"受管销售出库 {stockOutRemark} 库存流水数量 {stockOutLedgerCount} 与明细数量 {stockOut.Details.Count} 不一致。");
            }

            if (stockOutWasCreated)
            {
                createdStockOuts++;
                createdStockOutDetails += stockOut.Details.Count;
                createdStockOutLedgers += stockOutLedgerCount;
            }
            else
            {
                reusedStockOuts++;
                reusedStockOutDetails += stockOut.Details.Count;
                reusedStockOutLedgers += stockOutLedgerCount;
            }
        }

        return (
            createdSupportIns,
            reusedSupportIns,
            createdSupportDetails,
            reusedSupportDetails,
            createdSupportBatches,
            reusedSupportBatches,
            createdSupportLedgers,
            reusedSupportLedgers,
            createdStockOuts,
            reusedStockOuts,
            createdStockOutDetails,
            reusedStockOutDetails,
            createdStockOutLedgers,
            reusedStockOutLedgers);
    }

    private static async Task<StockInOrder> GetManagedSaleStockSupportInAsync(
        ApplicationDbContext context,
        Guid stockInOrderId,
        CancellationToken cancellationToken)
    {
        return await context.StockInOrders
            .Include(order => order.Details)
            .ThenInclude(detail => detail.StockBatch)
            .SingleAsync(order => order.Id == stockInOrderId, cancellationToken);
    }

    private static async Task<StockOutOrder> GetManagedSaleStockOutAsync(
        ApplicationDbContext context,
        Guid stockOutOrderId,
        CancellationToken cancellationToken)
    {
        return await context.StockOutOrders
            .Include(order => order.Details)
            .ThenInclude(detail => detail.StockBatch)
            .SingleAsync(order => order.Id == stockOutOrderId, cancellationToken);
    }

    private static CreateOtherStockInDto CreateSaleStockSupportInDto(
        SaleOrder saleOrder,
        Guid departmentId,
        int sequence)
    {
        return new CreateOtherStockInDto
        {
            WareId = saleOrder.WareId!.Value,
            DepartmentId = departmentId,
            InTime = CreateSaleStockSupportInTime(sequence),
            Remark = CreateSaleStockSupportInRemark(sequence),
            Details = saleOrder.Details
                .OrderBy(detail => detail.GoodsCodeSnapshot, StringComparer.Ordinal)
                .Select((detail, detailIndex) => CreateSaleStockSupportInDetailDto(sequence, detailIndex, detail))
                .ToList()
        };
    }

    private static UpdateOtherStockInDto CreateSaleStockSupportInUpdateDto(
        StockInOrder supportIn,
        SaleOrder saleOrder,
        Guid departmentId,
        int sequence)
    {
        var supportDetailsByGoodsId = supportIn.Details.ToDictionary(detail => detail.GoodsId);
        return new UpdateOtherStockInDto
        {
            Id = supportIn.Id,
            WareId = saleOrder.WareId!.Value,
            DepartmentId = departmentId,
            InTime = CreateSaleStockSupportInTime(sequence),
            Remark = CreateSaleStockSupportInRemark(sequence),
            Details = saleOrder.Details
                .OrderBy(detail => detail.GoodsCodeSnapshot, StringComparer.Ordinal)
                .Select((detail, detailIndex) =>
                {
                    var dto = CreateSaleStockSupportInDetailDto(sequence, detailIndex, detail);
                    return new UpdateStockInDetailDto
                    {
                        Id = supportDetailsByGoodsId.GetValueOrDefault(detail.GoodsId)?.Id,
                        GoodsId = dto.GoodsId,
                        GoodsUnitId = dto.GoodsUnitId,
                        Quantity = dto.Quantity,
                        UnitPrice = dto.UnitPrice,
                        BatchNo = dto.BatchNo,
                        ProductDate = dto.ProductDate,
                        ExpireDate = dto.ExpireDate,
                        Remark = dto.Remark
                    };
                })
                .ToList()
        };
    }

    private static CreateStockInDetailDto CreateSaleStockSupportInDetailDto(
        int sequence,
        int detailIndex,
        SaleOrderDetail detail)
    {
        var productDate = new DateOnly(2026, 8, (sequence + detailIndex - 1) % 28 + 1);
        return new CreateStockInDetailDto
        {
            GoodsId = detail.GoodsId,
            GoodsUnitId = detail.GoodsUnitId,
            Quantity = detail.Quantity,
            UnitPrice = NumericPrecision.RoundMoney(detail.FixedPrice * 0.72m),
            BatchNo = CreateSaleStockSupportBatchNo(sequence, detailIndex),
            ProductDate = productDate,
            ExpireDate = productDate.AddDays(7 + detailIndex),
            Remark = CreateSaleStockSupportInDetailRemark(sequence, detail.GoodsCodeSnapshot, detailIndex)
        };
    }

    private static CreateSaleStockOutDto CreateSaleStockOutDto(
        SaleOrder saleOrder,
        StockInOrder supportIn,
        Guid departmentId,
        int sequence)
    {
        var supportDetailsByGoodsId = supportIn.Details.ToDictionary(detail => detail.GoodsId);
        return new CreateSaleStockOutDto
        {
            WareId = saleOrder.WareId!.Value,
            SaleOrderId = saleOrder.Id,
            CustomerId = saleOrder.CustomerId,
            DepartmentId = departmentId,
            OutTime = CreateSaleStockOutTime(sequence),
            Remark = CreateSaleStockOutRemark(sequence),
            Details = saleOrder.Details
                .OrderBy(detail => detail.GoodsCodeSnapshot, StringComparer.Ordinal)
                .Select((detail, detailIndex) => CreateSaleStockOutDetailDto(
                    sequence,
                    detailIndex,
                    detail,
                    GetManagedReference(supportDetailsByGoodsId, detail.GoodsId, "销售出库支撑入库明细")))
                .ToList()
        };
    }

    private static UpdateSaleStockOutDto CreateSaleStockOutUpdateDto(
        StockOutOrder stockOut,
        SaleOrder saleOrder,
        StockInOrder supportIn,
        Guid departmentId,
        int sequence)
    {
        var stockOutDetailsBySourceId = stockOut.Details
            .Where(detail => detail.SaleOrderDetailId.HasValue)
            .ToDictionary(detail => detail.SaleOrderDetailId!.Value);
        var supportDetailsByGoodsId = supportIn.Details.ToDictionary(detail => detail.GoodsId);
        return new UpdateSaleStockOutDto
        {
            Id = stockOut.Id,
            WareId = saleOrder.WareId!.Value,
            SaleOrderId = saleOrder.Id,
            CustomerId = saleOrder.CustomerId,
            DepartmentId = departmentId,
            OutTime = CreateSaleStockOutTime(sequence),
            Remark = CreateSaleStockOutRemark(sequence),
            Details = saleOrder.Details
                .OrderBy(detail => detail.GoodsCodeSnapshot, StringComparer.Ordinal)
                .Select((detail, detailIndex) =>
                {
                    var supportDetail = GetManagedReference(
                        supportDetailsByGoodsId,
                        detail.GoodsId,
                        "销售出库支撑入库明细");
                    var dto = CreateSaleStockOutDetailDto(sequence, detailIndex, detail, supportDetail);
                    return new UpdateStockOutDetailDto
                    {
                        Id = stockOutDetailsBySourceId.GetValueOrDefault(detail.Id)?.Id,
                        SaleOrderDetailId = dto.SaleOrderDetailId,
                        StockBatchId = dto.StockBatchId,
                        GoodsUnitId = dto.GoodsUnitId,
                        Quantity = dto.Quantity,
                        UnitPrice = dto.UnitPrice,
                        Remark = dto.Remark
                    };
                })
                .ToList()
        };
    }

    private static CreateStockOutDetailDto CreateSaleStockOutDetailDto(
        int sequence,
        int detailIndex,
        SaleOrderDetail saleDetail,
        StockInDetail supportDetail)
    {
        if (!supportDetail.StockBatchId.HasValue)
        {
            throw new InvalidOperationException(
                $"受管销售出库 {sequence:D2} 的支撑入库明细 {supportDetail.GoodsCodeSnapshot} 未生成库存批次。");
        }

        return new CreateStockOutDetailDto
        {
            SaleOrderDetailId = saleDetail.Id,
            StockBatchId = supportDetail.StockBatchId.Value,
            GoodsUnitId = saleDetail.GoodsUnitId,
            Quantity = saleDetail.Quantity,
            UnitPrice = saleDetail.FixedPrice,
            Remark = CreateSaleStockOutDetailRemark(sequence, saleDetail.GoodsCodeSnapshot, detailIndex)
        };
    }

    private static void ApplyManagedSaleStockSupportInFields(
        StockInOrder supportIn,
        SaleOrder saleOrder,
        int sequence,
        DemoAuditUser auditUser)
    {
        supportIn.WareId = saleOrder.WareId!.Value;
        supportIn.Remark = CreateSaleStockSupportInRemark(sequence);
        supportIn.InTime = CreateSaleStockSupportInTime(sequence);
        if (supportIn.CreateBy != auditUser.Id || supportIn.CreateName != auditUser.Username)
        {
            supportIn.CreateBy = auditUser.Id;
            supportIn.CreateName = auditUser.Username;
        }

        var orderedDetails = supportIn.Details
            .OrderBy(detail => detail.GoodsCodeSnapshot, StringComparer.Ordinal)
            .ToArray();
        for (var detailIndex = 0; detailIndex < orderedDetails.Length; detailIndex++)
        {
            var detail = orderedDetails[detailIndex];
            detail.BatchNo = CreateSaleStockSupportBatchNo(sequence, detailIndex);
            detail.Remark = CreateSaleStockSupportInDetailRemark(sequence, detail.GoodsCodeSnapshot, detailIndex);
            if (detail.CreateBy != auditUser.Id || detail.CreateName != auditUser.Username)
            {
                detail.CreateBy = auditUser.Id;
                detail.CreateName = auditUser.Username;
            }
        }
    }

    private static void ApplyManagedSaleStockOutFields(
        StockOutOrder stockOut,
        int sequence,
        DemoAuditUser auditUser)
    {
        stockOut.Remark = CreateSaleStockOutRemark(sequence);
        stockOut.OutTime = CreateSaleStockOutTime(sequence);
        if (stockOut.CreateBy != auditUser.Id || stockOut.CreateName != auditUser.Username)
        {
            stockOut.CreateBy = auditUser.Id;
            stockOut.CreateName = auditUser.Username;
        }

        var orderedDetails = stockOut.Details
            .OrderBy(detail => detail.GoodsCodeSnapshot, StringComparer.Ordinal)
            .ToArray();
        for (var detailIndex = 0; detailIndex < orderedDetails.Length; detailIndex++)
        {
            var detail = orderedDetails[detailIndex];
            detail.Remark = CreateSaleStockOutDetailRemark(sequence, detail.GoodsCodeSnapshot, detailIndex);
            if (detail.CreateBy != auditUser.Id || detail.CreateName != auditUser.Username)
            {
                detail.CreateBy = auditUser.Id;
                detail.CreateName = auditUser.Username;
            }
        }
    }

    private static string CreateSaleStockSupportInRemark(int sequence)
    {
        var stableKey = DemoDataStableKeyCatalog.Create("SALE-STOCK-SUPPORT-IN", sequence);
        return $"{stableKey} 华东联调销售出库支撑入库{sequence:D2}：按销售订单仓库补充可出库库存，用于销售出库扣减。";
    }

    private static string CreateSaleStockSupportInDetailRemark(int sequence, string goodsCode, int detailIndex)
    {
        return $"SkyRoc 联调销售出库支撑入库明细：第 {sequence:D2} 张支撑入库第 {detailIndex + 1} 行商品 {goodsCode}，用于同仓销售出库。";
    }

    private static string CreateSaleStockSupportInAuditRemark(int sequence)
    {
        return $"SkyRoc 联调销售出库支撑入库审核：确认第 {sequence:D2} 张销售订单同仓可出库库存。";
    }

    private static string CreateSaleStockOutRemark(int sequence)
    {
        var stableKey = DemoDataStableKeyCatalog.Create("SALE-STOCK-OUT", sequence);
        return $"{stableKey} 华东联调销售出库{sequence:D2}：来源受管销售订单，用于配送、签收和客户账单链路。";
    }

    private static string CreateSaleStockOutDetailRemark(int sequence, string goodsCode, int detailIndex)
    {
        return $"SkyRoc 联调销售出库明细：销售出库 {sequence:D2} 第 {detailIndex + 1} 行商品 {goodsCode}，扣减同仓稳定批次库存。";
    }

    private static string CreateSaleStockOutAuditRemark(int sequence)
    {
        return $"SkyRoc 联调销售出库审核：确认第 {sequence:D2} 张销售订单完成出库并追加库存流水。";
    }

    private static string CreateSaleStockSupportBatchNo(int sequence, int detailIndex)
    {
        return $"{DemoDataStableKeyCatalog.Create("SALE-STOCK-BATCH", sequence)}-{detailIndex + 1:D2}";
    }

    private static DateTime CreateSaleStockSupportInTime(int sequence)
    {
        return new DateTime(2026, 8, (sequence - 1) % 28 + 1, 12, 0, 0, DateTimeKind.Utc);
    }

    private static DateTime CreateSaleStockOutTime(int sequence)
    {
        return CreateSaleStockSupportInTime(sequence).AddHours(4);
    }

    private static async Task<(
        int CreatedDeliveryTasks,
        int ReusedDeliveryTasks,
        int CreatedOrderReceipts,
        int ReusedOrderReceipts,
        int CreatedOrderCheckDetails,
        int ReusedOrderCheckDetails,
        int CreatedCustomerBills,
        int ReusedCustomerBills,
        int CreatedCustomerBillDetails,
        int ReusedCustomerBillDetails)> GenerateDeliveryTasksAsync(
            ApplicationDbContext context,
            IDeliveryTaskService deliveryTaskService,
            DemoAuditUser auditUser,
            CancellationToken cancellationToken)
    {
        var stockOutRemarks = Enumerable.Range(1, 40)
            .Select(CreateSaleStockOutRemark)
            .ToArray();
        var driverCodes = Enumerable.Range(1, 30)
            .Select(sequence => DemoDataStableKeyCatalog.Create("DRIVER", sequence))
            .ToArray();

        var managedStockOuts = await context.StockOutOrders
            .Include(order => order.Details)
            .Where(order => order.Remark != null
                            && stockOutRemarks.Contains(order.Remark)
                            && order.OrderType == StockOutOrderType.Sale
                            && order.BusinessStatus == StockDocumentStatus.Audited)
            .OrderBy(order => order.Remark)
            .ToListAsync(cancellationToken);
        if (managedStockOuts.Count != 40)
        {
            throw new InvalidOperationException(
                $"受管配送任务生成需要 40 张已审核销售出库，当前为 {managedStockOuts.Count} 张。");
        }

        var managedDrivers = await context.Drivers
            .Include(driver => driver.Carrier)
            .Where(driver => driverCodes.Contains(driver.Code))
            .ToDictionaryAsync(driver => driver.Code, StringComparer.Ordinal, cancellationToken);
        var stockOutIds = managedStockOuts.Select(order => order.Id).ToArray();
        var existingTasks = await context.DeliveryTasks
            .Include(task => task.StockOutOrder)
            .ThenInclude(order => order.Details)
            .Include(task => task.SaleOrder)
            .ThenInclude(order => order.Details)
            .Include(task => task.Receipt)
            .ThenInclude(receipt => receipt!.CheckDetails)
            .Where(task => stockOutIds.Contains(task.StockOutOrderId))
            .ToDictionaryAsync(task => task.StockOutOrderId, cancellationToken);

        var createdDeliveryTasks = 0;
        var reusedDeliveryTasks = 0;
        var createdOrderReceipts = 0;
        var reusedOrderReceipts = 0;
        var createdOrderCheckDetails = 0;
        var reusedOrderCheckDetails = 0;
        var createdCustomerBills = 0;
        var reusedCustomerBills = 0;
        var createdCustomerBillDetails = 0;
        var reusedCustomerBillDetails = 0;

        for (var index = 0; index < managedStockOuts.Count; index++)
        {
            var sequence = index + 1;
            var stockOut = managedStockOuts[index];
            var driverSequence = (sequence - 1) % 30 + 1;
            var driver = GetManagedReference(
                managedDrivers,
                DemoDataStableKeyCatalog.Create("DRIVER", driverSequence),
                "配送司机");

            var taskWasCreated = !existingTasks.TryGetValue(stockOut.Id, out var task);
            if (taskWasCreated)
            {
                var created = await deliveryTaskService.GenerateFromStockOutAsync(stockOut.Id);
                task = await GetManagedDeliveryTaskAsync(context, created.Id, cancellationToken);
            }
            else
            {
                task = await GetManagedDeliveryTaskAsync(context, task!.Id, cancellationToken);
            }

            if (task.DeliveryStatus == DeliveryTaskStatus.PendingAssign)
            {
                await deliveryTaskService.AssignDriverAsync(new AssignDeliveryDriverDto
                {
                    TaskIds = [task.Id],
                    DriverId = driver.Id
                });
                task = await GetManagedDeliveryTaskAsync(context, task.Id, cancellationToken);
            }

            if (task.DeliveryStatus == DeliveryTaskStatus.Assigned && !task.RouteId.HasValue)
            {
                await deliveryTaskService.IntelligentPlanAsync(new IntelligentPlanDeliveryTasksDto
                {
                    TaskIds = [task.Id]
                });
                task = await GetManagedDeliveryTaskAsync(context, task.Id, cancellationToken);
            }

            if (task.DeliveryStatus == DeliveryTaskStatus.Assigned)
            {
                await deliveryTaskService.StartDeliveryAsync(task.Id);
                task = await GetManagedDeliveryTaskAsync(context, task.Id, cancellationToken);
            }

            var receiptWasCreated = task.Receipt is null;
            var billBeforeSign = await GetManagedCustomerBillAsync(context, task.SaleOrderId, cancellationToken);
            if (task.DeliveryStatus == DeliveryTaskStatus.Delivering)
            {
                await deliveryTaskService.SignAsync(task.Id, CreateDeliverySignDto(task, sequence));
                task = await GetManagedDeliveryTaskAsync(context, task.Id, cancellationToken);
            }
            else if (task.DeliveryStatus != DeliveryTaskStatus.Signed)
            {
                throw new InvalidOperationException(
                    $"受管配送任务 {CreateDeliveryTaskRemark(sequence)} 当前状态为 {task.DeliveryStatus}，不能安全复用。");
            }

            if (task.Receipt is null)
            {
                throw new InvalidOperationException($"受管配送任务 {CreateDeliveryTaskRemark(sequence)} 缺少签收回单。");
            }

            if (!task.Receipt.ReturnedTime.HasValue)
            {
                await deliveryTaskService.ReturnReceiptAsync(task.Id, CreateDeliveryReturnReceiptDto(sequence));
                task = await GetManagedDeliveryTaskAsync(context, task.Id, cancellationToken);
            }

            var customerBill = await GetManagedCustomerBillAsync(context, task.SaleOrderId, cancellationToken)
                               ?? throw new InvalidOperationException(
                                   $"受管配送任务 {CreateDeliveryTaskRemark(sequence)} 签收后未生成客户账单。");

            ApplyManagedDeliveryTaskFields(task, sequence, auditUser);
            ApplyManagedCustomerBillFields(customerBill, sequence, auditUser);
            await context.SaveChangesAsync(cancellationToken);

            var receipt = task.Receipt
                          ?? throw new InvalidOperationException($"受管配送任务 {CreateDeliveryTaskRemark(sequence)} 缺少签收回单。");
            if (taskWasCreated)
            {
                createdDeliveryTasks++;
            }
            else
            {
                reusedDeliveryTasks++;
            }

            if (receiptWasCreated)
            {
                createdOrderReceipts++;
                createdOrderCheckDetails += receipt.CheckDetails.Count;
            }
            else
            {
                reusedOrderReceipts++;
                reusedOrderCheckDetails += receipt.CheckDetails.Count;
            }

            if (billBeforeSign is null)
            {
                createdCustomerBills++;
                createdCustomerBillDetails += customerBill.Details.Count;
            }
            else
            {
                reusedCustomerBills++;
                reusedCustomerBillDetails += customerBill.Details.Count;
            }
        }

        return (
            createdDeliveryTasks,
            reusedDeliveryTasks,
            createdOrderReceipts,
            reusedOrderReceipts,
            createdOrderCheckDetails,
            reusedOrderCheckDetails,
            createdCustomerBills,
            reusedCustomerBills,
            createdCustomerBillDetails,
            reusedCustomerBillDetails);
    }

    private static async Task<DeliveryTask> GetManagedDeliveryTaskAsync(
        ApplicationDbContext context,
        Guid deliveryTaskId,
        CancellationToken cancellationToken)
    {
        return await context.DeliveryTasks
            .Include(task => task.StockOutOrder)
            .ThenInclude(order => order.Details)
            .Include(task => task.SaleOrder)
            .ThenInclude(order => order.Details)
            .Include(task => task.Receipt)
            .ThenInclude(receipt => receipt!.CheckDetails)
            .SingleAsync(task => task.Id == deliveryTaskId, cancellationToken);
    }

    private static async Task<CustomerBill?> GetManagedCustomerBillAsync(
        ApplicationDbContext context,
        Guid saleOrderId,
        CancellationToken cancellationToken)
    {
        return await context.CustomerBills
            .Include(bill => bill.Details)
            .SingleOrDefaultAsync(bill => bill.SaleOrderId == saleOrderId, cancellationToken);
    }

    private static SignDeliveryTaskDto CreateDeliverySignDto(DeliveryTask task, int sequence)
    {
        return new SignDeliveryTaskDto
        {
            SignerName = $"华东客户签收员{sequence:D2}",
            Remark = CreateDeliverySignRemark(sequence),
            Details = task.StockOutOrder.Details
                .OrderBy(detail => detail.GoodsCodeSnapshot, StringComparer.Ordinal)
                .Select((detail, detailIndex) => new SignDeliveryCheckDetailDto
                {
                    StockOutDetailId = detail.Id,
                    AcceptedBaseQuantity = detail.BaseQuantity,
                    CheckStatus = OrderCustomerCheckStatus.Accepted,
                    Remark = CreateDeliveryCheckDetailRemark(sequence, detail.GoodsCodeSnapshot, detailIndex)
                })
                .ToList()
        };
    }

    private static ReturnOrderReceiptDto CreateDeliveryReturnReceiptDto(int sequence)
    {
        return new ReturnOrderReceiptDto
        {
            ReceiptImageUrl = $"https://assets.skyroc.example/receipts/{DemoDataStableKeyCatalog.Create("DELIVERY-RECEIPT", sequence).ToLowerInvariant()}.pdf",
            Remark = CreateDeliveryReturnRemark(sequence)
        };
    }

    private static void ApplyManagedDeliveryTaskFields(
        DeliveryTask task,
        int sequence,
        DemoAuditUser auditUser)
    {
        task.Remark = CreateDeliveryTaskRemark(sequence);
        if (task.CreateBy != auditUser.Id || task.CreateName != auditUser.Username)
        {
            task.CreateBy = auditUser.Id;
            task.CreateName = auditUser.Username;
        }

        if (task.Receipt is not null)
        {
            task.Receipt.SignRemark = CreateDeliverySignRemark(sequence);
            task.Receipt.ReceiptImageUrl = CreateDeliveryReturnReceiptDto(sequence).ReceiptImageUrl;
            task.Receipt.ReturnRemark = CreateDeliveryReturnRemark(sequence);
            if (task.Receipt.CreateBy != auditUser.Id || task.Receipt.CreateName != auditUser.Username)
            {
                task.Receipt.CreateBy = auditUser.Id;
                task.Receipt.CreateName = auditUser.Username;
            }
            if (task.Receipt.UpdateBy != auditUser.Id || task.Receipt.UpdateName != auditUser.Username)
            {
                task.Receipt.UpdateBy = auditUser.Id;
                task.Receipt.UpdateName = auditUser.Username;
            }

            var orderedCheckDetails = task.Receipt.CheckDetails
                .OrderBy(detail => detail.GoodsCodeSnapshot, StringComparer.Ordinal)
                .ToArray();
            for (var detailIndex = 0; detailIndex < orderedCheckDetails.Length; detailIndex++)
            {
                var detail = orderedCheckDetails[detailIndex];
                detail.Remark = CreateDeliveryCheckDetailRemark(sequence, detail.GoodsCodeSnapshot, detailIndex);
                if (detail.CreateBy != auditUser.Id || detail.CreateName != auditUser.Username)
                {
                    detail.CreateBy = auditUser.Id;
                    detail.CreateName = auditUser.Username;
                }
                if (detail.UpdateBy != auditUser.Id || detail.UpdateName != auditUser.Username)
                {
                    detail.UpdateBy = auditUser.Id;
                    detail.UpdateName = auditUser.Username;
                }
            }
        }
    }

    private static void ApplyManagedCustomerBillFields(
        CustomerBill bill,
        int sequence,
        DemoAuditUser auditUser)
    {
        bill.Remark = $"SkyRoc 联调客户账单：第 {sequence:D2} 张配送签收后生成的客户应收账单，等待后续客户结款。";
        if (bill.CreateBy != auditUser.Id || bill.CreateName != auditUser.Username)
        {
            bill.CreateBy = auditUser.Id;
            bill.CreateName = auditUser.Username;
        }
        if (bill.UpdateBy != auditUser.Id || bill.UpdateName != auditUser.Username)
        {
            bill.UpdateBy = auditUser.Id;
            bill.UpdateName = auditUser.Username;
        }

        foreach (var detail in bill.Details)
        {
            if (string.IsNullOrWhiteSpace(detail.Remark))
            {
                detail.Remark = $"SkyRoc 联调客户账单明细：签收商品 {detail.GoodsCodeSnapshot} 形成客户应收。";
            }

            if (detail.CreateBy != auditUser.Id || detail.CreateName != auditUser.Username)
            {
                detail.CreateBy = auditUser.Id;
                detail.CreateName = auditUser.Username;
            }
            if (detail.UpdateBy != auditUser.Id || detail.UpdateName != auditUser.Username)
            {
                detail.UpdateBy = auditUser.Id;
                detail.UpdateName = auditUser.Username;
            }
        }
    }

    private static string CreateDeliveryTaskRemark(int sequence)
    {
        var stableKey = DemoDataStableKeyCatalog.Create("DELIVERY-TASK", sequence);
        return $"{stableKey} 华东联调配送任务{sequence:D2}：来源受管销售出库，已完成分配、路线规划、签收和回单。";
    }

    private static string CreateDeliverySignRemark(int sequence)
    {
        return $"SkyRoc 联调配送签收：第 {sequence:D2} 张任务客户已按出库明细完成验收。";
    }

    private static string CreateDeliveryCheckDetailRemark(int sequence, string goodsCode, int detailIndex)
    {
        return $"SkyRoc 联调配送验收明细：第 {sequence:D2} 张任务第 {detailIndex + 1} 行商品 {goodsCode} 全量验收通过。";
    }

    private static string CreateDeliveryReturnRemark(int sequence)
    {
        return $"SkyRoc 联调配送回单：第 {sequence:D2} 张任务电子回单已归档，用于订单回单状态聚合。";
    }

    private static async Task<AfterSaleGenerationResult> GenerateAfterSalesAsync(
            ApplicationDbContext context,
            IAfterSaleService afterSaleService,
            IPickupTaskService pickupTaskService,
            IStockInService stockInService,
            DemoAuditUser auditUser,
            int existingCreatedCustomerBillDetails,
            int existingReusedCustomerBillDetails,
            CancellationToken cancellationToken)
    {
        var saleOrderKeys = Enumerable.Range(1, 60)
            .Select(sequence => DemoDataStableKeyCatalog.Create("SALE-ORDER", sequence))
            .ToArray();
        var afterSaleRemarks = Enumerable.Range(1, 40)
            .Select(CreateAfterSaleRemark)
            .ToArray();
        var salesReturnRemarks = Enumerable.Range(1, 40)
            .Select(CreateSalesReturnStockInRemark)
            .ToArray();
        var driverCodes = Enumerable.Range(1, 30)
            .Select(sequence => DemoDataStableKeyCatalog.Create("DRIVER", sequence))
            .ToArray();
        var departmentCodes = Enumerable.Range(1, 30)
            .Select(sequence => DemoDataStableKeyCatalog.Create("DEPARTMENT", sequence))
            .ToArray();
        var supplierCodes = Enumerable.Range(1, 30)
            .Select(sequence => DemoDataStableKeyCatalog.Create("SUPPLIER", sequence))
            .ToArray();

        var signedOrders = await context.SaleOrders
            .Include(order => order.Details)
            .Include(order => order.Customer)
            .Where(order => order.InnerRemark != null
                            && order.OrderStatus == SaleOrderStatus.Signed
                            && saleOrderKeys.Contains(order.InnerRemark))
            .OrderBy(order => order.InnerRemark)
            .Take(40)
            .ToListAsync(cancellationToken);
        if (signedOrders.Count != 40)
        {
            throw new InvalidOperationException(
                $"受管售后生成需要 40 张已签收销售订单，当前为 {signedOrders.Count} 张。");
        }

        var managedDrivers = await context.Drivers
            .Where(driver => driverCodes.Contains(driver.Code))
            .ToDictionaryAsync(driver => driver.Code, StringComparer.Ordinal, cancellationToken);
        var managedDepartments = await context.Departments
            .Where(department => departmentCodes.Contains(department.Code))
            .ToDictionaryAsync(department => department.Code, StringComparer.Ordinal, cancellationToken);
        var managedSuppliers = await context.Suppliers
            .Where(supplier => supplierCodes.Contains(supplier.Code))
            .ToDictionaryAsync(supplier => supplier.Code, StringComparer.Ordinal, cancellationToken);
        var existingAfterSales = await context.AfterSales
            .Include(afterSale => afterSale.Goods)
            .Include(afterSale => afterSale.AuditLogs)
            .Include(afterSale => afterSale.PickupTasks)
            .ThenInclude(task => task.StockInDetail)
            .ThenInclude(detail => detail!.StockInOrder)
            .ThenInclude(order => order.Details)
            .Where(afterSale => afterSale.Remark != null && afterSaleRemarks.Contains(afterSale.Remark))
            .ToDictionaryAsync(afterSale => afterSale.Remark!, StringComparer.Ordinal, cancellationToken);
        var existingSalesReturns = await context.StockInOrders
            .Include(order => order.Details)
            .ThenInclude(detail => detail.StockBatch)
            .Where(order => order.Remark != null
                            && salesReturnRemarks.Contains(order.Remark)
                            && order.OrderType == StockInOrderType.SalesReturn)
            .ToDictionaryAsync(order => order.Remark!, StringComparer.Ordinal, cancellationToken);

        var createdAfterSales = 0;
        var reusedAfterSales = 0;
        var createdAfterSaleGoods = 0;
        var reusedAfterSaleGoods = 0;
        var createdAfterSaleAuditLogs = 0;
        var reusedAfterSaleAuditLogs = 0;
        var createdPickupTasks = 0;
        var reusedPickupTasks = 0;
        var createdSalesReturnStockIns = 0;
        var reusedSalesReturnStockIns = 0;
        var createdSalesReturnStockInDetails = 0;
        var reusedSalesReturnStockInDetails = 0;
        var createdSalesReturnBatches = 0;
        var reusedSalesReturnBatches = 0;
        var createdSalesReturnLedgers = 0;
        var reusedSalesReturnLedgers = 0;
        var createdCustomerBillDetails = existingCreatedCustomerBillDetails;
        var reusedCustomerBillDetails = existingReusedCustomerBillDetails;

        for (var index = 0; index < signedOrders.Count; index++)
        {
            var sequence = index + 1;
            var saleOrder = signedOrders[index];
            var afterSaleRemark = CreateAfterSaleRemark(sequence);
            var referenceSequence = (sequence - 1) % 30 + 1;
            var driver = GetManagedReference(
                managedDrivers,
                DemoDataStableKeyCatalog.Create("DRIVER", referenceSequence),
                "取货司机");
            var department = GetManagedReference(
                managedDepartments,
                DemoDataStableKeyCatalog.Create("DEPARTMENT", referenceSequence),
                "责任部门");
            var supplier = GetManagedReference(
                managedSuppliers,
                DemoDataStableKeyCatalog.Create("SUPPLIER", referenceSequence),
                "供应商");
            var scenario = ResolveAfterSaleScenario(sequence);
            var orderedDetails = saleOrder.Details
                .OrderBy(detail => detail.GoodsCodeSnapshot, StringComparer.Ordinal)
                .ToArray();
            if (orderedDetails.Length == 0)
            {
                throw new InvalidOperationException($"受管销售订单 {saleOrder.InnerRemark} 缺少可售后明细。");
            }

            var primaryDetail = orderedDetails[0];
            var afterSaleWasCreated = !existingAfterSales.TryGetValue(afterSaleRemark, out var afterSale);
            if (afterSaleWasCreated)
            {
                var created = await afterSaleService.CreateAsync(new CreateAfterSaleDto
                {
                    SaleOrderId = saleOrder.Id,
                    CustomerId = saleOrder.CustomerId,
                    Source = "后台联调建单",
                    ContactName = saleOrder.ContactNameSnapshot ?? $"华东售后联系人{sequence:D2}",
                    ContactPhone = saleOrder.ContactPhoneSnapshot ?? $"1398000{sequence:D4}",
                    PickupAddress = saleOrder.DeliveryAddressSnapshot
                                    ?? $"上海市浦东新区鲜品大道{referenceSequence}号客户退货月台{sequence:D2}",
                    Remark = afterSaleRemark,
                    Goods =
                    [
                        new CreateAfterSaleGoodsDto
                        {
                            SaleOrderDetailId = primaryDetail.Id,
                            ActualRefundQuantity = CreateAfterSaleRefundQuantity(primaryDetail, scenario),
                            AfterSaleType = scenario.AfterSaleType,
                            SupplierId = supplier.Id,
                            DepartmentId = department.Id,
                            ReasonType = scenario.ReasonType,
                            HandleType = scenario.HandleType,
                            Remark = CreateAfterSaleGoodsRemark(sequence, primaryDetail.GoodsCodeSnapshot)
                        }
                    ]
                });
                afterSale = await GetManagedAfterSaleAsync(context, created.Id, cancellationToken);
            }
            else
            {
                afterSale = await GetManagedAfterSaleAsync(context, afterSale!.Id, cancellationToken);
            }

            if (afterSale.AfterStatus == AfterSaleStatus.Draft
                && afterSale.AuditLogs.All(log => log.Action != AfterSaleAuditAction.Reject))
            {
                await afterSaleService.SubmitAsync(afterSale.Id, CreateAfterSaleSubmitRemark(sequence));
                afterSale = await GetManagedAfterSaleAsync(context, afterSale.Id, cancellationToken);
            }

            if (scenario.TargetStatus == AfterSaleStatus.Draft
                && afterSale.AfterStatus == AfterSaleStatus.PendingAudit
                && afterSale.AuditLogs.All(log => log.Action != AfterSaleAuditAction.Reject))
            {
                await afterSaleService.RejectAsync(afterSale.Id, CreateAfterSaleRejectRemark(sequence));
                afterSale = await GetManagedAfterSaleAsync(context, afterSale.Id, cancellationToken);
            }

            if (scenario.TargetStatus is AfterSaleStatus.ReturnPending
                    or AfterSaleStatus.RefundPending
                    or AfterSaleStatus.Completed
                && afterSale.AfterStatus == AfterSaleStatus.PendingAudit)
            {
                await afterSaleService.ApproveAsync(afterSale.Id, CreateAfterSaleApproveRemark(sequence));
                afterSale = await GetManagedAfterSaleAsync(context, afterSale.Id, cancellationToken);
            }

            if (scenario.TargetStatus == AfterSaleStatus.Completed)
            {
                if (scenario.AfterSaleType == AfterSaleType.ReturnAndRefund)
                {
                    var pickupTask = afterSale.PickupTasks.SingleOrDefault()
                                     ?? throw new InvalidOperationException(
                                         $"受管售后 {afterSaleRemark} 审核后未生成取货任务。");
                    if (pickupTask.PickupStatus == PickupTaskStatus.PendingAssign)
                    {
                        await pickupTaskService.AssignAsync(pickupTask.Id, new AssignPickupTaskDto
                        {
                            DriverId = driver.Id,
                            PlannedPickupTime = CreatePickupPlannedTime(sequence),
                            Remark = CreatePickupAssignRemark(sequence)
                        });
                        afterSale = await GetManagedAfterSaleAsync(context, afterSale.Id, cancellationToken);
                        pickupTask = afterSale.PickupTasks.Single();
                    }

                    if (pickupTask.PickupStatus == PickupTaskStatus.PendingPickup)
                    {
                        await pickupTaskService.StartAsync(pickupTask.Id);
                        afterSale = await GetManagedAfterSaleAsync(context, afterSale.Id, cancellationToken);
                        pickupTask = afterSale.PickupTasks.Single();
                    }

                    if (pickupTask.PickupStatus == PickupTaskStatus.PickingUp)
                    {
                        await pickupTaskService.CompleteAsync(pickupTask.Id);
                        afterSale = await GetManagedAfterSaleAsync(context, afterSale.Id, cancellationToken);
                        pickupTask = afterSale.PickupTasks.Single();
                    }

                    var salesReturnResult = await EnsureManagedSalesReturnAsync(
                        context,
                        stockInService,
                        existingSalesReturns,
                        saleOrder,
                        afterSale,
                        pickupTask,
                        department.Id,
                        sequence,
                        auditUser,
                        cancellationToken);
                    if (salesReturnResult.WasCreated)
                    {
                        createdSalesReturnStockIns++;
                        createdSalesReturnStockInDetails += salesReturnResult.DetailCount;
                        createdSalesReturnBatches += salesReturnResult.BatchCount;
                        createdSalesReturnLedgers += salesReturnResult.LedgerCount;
                    }
                    else
                    {
                        reusedSalesReturnStockIns++;
                        reusedSalesReturnStockInDetails += salesReturnResult.DetailCount;
                        reusedSalesReturnBatches += salesReturnResult.BatchCount;
                        reusedSalesReturnLedgers += salesReturnResult.LedgerCount;
                    }

                    afterSale = await GetManagedAfterSaleAsync(context, afterSale.Id, cancellationToken);
                }

                var billBeforeComplete = await GetManagedCustomerBillAsync(
                    context,
                    afterSale.SaleOrderId!.Value,
                    cancellationToken);
                var adjustmentCountBefore = billBeforeComplete?.Details
                    .Count(detail => detail.SourceType == CustomerBillDetailSourceType.AfterSaleAdjustment) ?? 0;
                if (afterSale.AfterStatus != AfterSaleStatus.Completed)
                {
                    await afterSaleService.CompleteAsync(afterSale.Id);
                    afterSale = await GetManagedAfterSaleAsync(context, afterSale.Id, cancellationToken);
                }

                var billAfterComplete = await GetManagedCustomerBillAsync(
                    context,
                    afterSale.SaleOrderId!.Value,
                    cancellationToken)
                    ?? throw new InvalidOperationException(
                        $"受管售后 {afterSaleRemark} 完成后未找到客户账单。");
                var adjustmentCountAfter = billAfterComplete.Details
                    .Count(detail => detail.SourceType == CustomerBillDetailSourceType.AfterSaleAdjustment);
                if (adjustmentCountAfter > adjustmentCountBefore)
                {
                    createdCustomerBillDetails += adjustmentCountAfter - adjustmentCountBefore;
                }
                else
                {
                    reusedCustomerBillDetails += adjustmentCountAfter;
                }
            }

            afterSale = await GetManagedAfterSaleAsync(context, afterSale.Id, cancellationToken);
            if (afterSale.AfterStatus != scenario.TargetStatus)
            {
                throw new InvalidOperationException(
                    $"受管售后 {afterSaleRemark} 目标状态为 {scenario.TargetStatus}，当前为 {afterSale.AfterStatus}。");
            }

            ApplyManagedAfterSaleFields(afterSale, sequence, auditUser);
            await context.SaveChangesAsync(cancellationToken);

            if (afterSaleWasCreated)
            {
                createdAfterSales++;
                createdAfterSaleGoods += afterSale.Goods.Count;
                createdAfterSaleAuditLogs += afterSale.AuditLogs.Count;
                createdPickupTasks += afterSale.PickupTasks.Count;
            }
            else
            {
                reusedAfterSales++;
                reusedAfterSaleGoods += afterSale.Goods.Count;
                reusedAfterSaleAuditLogs += afterSale.AuditLogs.Count;
                reusedPickupTasks += afterSale.PickupTasks.Count;
            }
        }

        return new AfterSaleGenerationResult
        {
            AfterSales = new GenerationLayerCount
            {
                Created = createdAfterSales,
                Reused = reusedAfterSales
            },
            AfterSaleGoods = new GenerationLayerCount
            {
                Created = createdAfterSaleGoods,
                Reused = reusedAfterSaleGoods
            },
            AfterSaleAuditLogs = new GenerationLayerCount
            {
                Created = createdAfterSaleAuditLogs,
                Reused = reusedAfterSaleAuditLogs
            },
            PickupTasks = new GenerationLayerCount
            {
                Created = createdPickupTasks,
                Reused = reusedPickupTasks
            },
            SalesReturnStockIns = new GenerationLayerCount
            {
                Created = createdSalesReturnStockIns,
                Reused = reusedSalesReturnStockIns
            },
            SalesReturnStockInDetails = new GenerationLayerCount
            {
                Created = createdSalesReturnStockInDetails,
                Reused = reusedSalesReturnStockInDetails
            },
            SalesReturnBatches = new GenerationLayerCount
            {
                Created = createdSalesReturnBatches,
                Reused = reusedSalesReturnBatches
            },
            SalesReturnLedgers = new GenerationLayerCount
            {
                Created = createdSalesReturnLedgers,
                Reused = reusedSalesReturnLedgers
            },
            CustomerBillDetails = new GenerationLayerCount
            {
                Created = createdCustomerBillDetails,
                Reused = reusedCustomerBillDetails
            }
        };
    }

    private static async Task<ManagedSalesReturnResult> EnsureManagedSalesReturnAsync(
        ApplicationDbContext context,
        IStockInService stockInService,
        Dictionary<string, StockInOrder> existingSalesReturns,
        SaleOrder saleOrder,
        AfterSale afterSale,
        PickupTask pickupTask,
        Guid departmentId,
        int sequence,
        DemoAuditUser auditUser,
        CancellationToken cancellationToken)
    {
        var salesReturnRemark = CreateSalesReturnStockInRemark(sequence);
        StockInOrder salesReturn;
        var salesReturnWasCreated = false;
        if (!existingSalesReturns.TryGetValue(salesReturnRemark, out var existingSalesReturn)
            && pickupTask.StockInDetail is null)
        {
            if (!saleOrder.WareId.HasValue)
            {
                throw new InvalidOperationException(
                    $"受管销售订单 {saleOrder.InnerRemark} 未配置退货入库仓库。");
            }

            var goods = afterSale.Goods.Single();
            var createdSalesReturn = await stockInService.CreateSalesReturnAsync(
                new CreateSalesReturnStockInDto
                {
                    AfterSaleId = afterSale.Id,
                    WareId = saleOrder.WareId.Value,
                    CustomerId = saleOrder.CustomerId,
                    DepartmentId = departmentId,
                    InTime = CreateSalesReturnInTime(sequence),
                    Remark = salesReturnRemark,
                    Details =
                    [
                        new CreateStockInDetailDto
                        {
                            PickupTaskId = pickupTask.Id,
                            GoodsId = goods.GoodsId,
                            GoodsUnitId = goods.GoodsUnitId,
                            Quantity = goods.ActualRefundQuantity,
                            UnitPrice = goods.UnitPrice,
                            BatchNo = CreateSalesReturnBatchNo(sequence),
                            ProductDate = CreateSalesReturnProductDate(sequence),
                            ExpireDate = CreateSalesReturnExpireDate(sequence),
                            Remark = CreateSalesReturnDetailRemark(
                                sequence,
                                goods.GoodsCodeSnapshot)
                        }
                    ]
                });
            await stockInService.AuditAsync(
                StockInOrderType.SalesReturn,
                createdSalesReturn.Id,
                CreateSalesReturnAuditRemark(sequence));
            salesReturn = await GetManagedSalesReturnStockInAsync(
                context,
                createdSalesReturn.Id,
                cancellationToken);
            salesReturnWasCreated = true;
            existingSalesReturns[salesReturnRemark] = salesReturn;
        }
        else if (existingSalesReturns.TryGetValue(salesReturnRemark, out existingSalesReturn))
        {
            salesReturn = existingSalesReturn;
            if (salesReturn.BusinessStatus is StockDocumentStatus.Draft or StockDocumentStatus.PendingAudit)
            {
                await stockInService.AuditAsync(
                    StockInOrderType.SalesReturn,
                    salesReturn.Id,
                    CreateSalesReturnAuditRemark(sequence));
                salesReturn = await GetManagedSalesReturnStockInAsync(
                    context,
                    salesReturn.Id,
                    cancellationToken);
                existingSalesReturns[salesReturnRemark] = salesReturn;
            }
        }
        else
        {
            salesReturn = await GetManagedSalesReturnStockInAsync(
                context,
                pickupTask.StockInDetail!.StockInOrderId,
                cancellationToken);
            existingSalesReturns[salesReturnRemark] = salesReturn;
        }

        ApplyManagedSalesReturnStockInFields(salesReturn, sequence, auditUser);
        await context.SaveChangesAsync(cancellationToken);

        var salesReturnDetailIds = salesReturn.Details.Select(detail => detail.Id).ToArray();
        var salesReturnLedgers = await context.StockLedgers
            .Where(ledger => ledger.SourceType == StockLedgerSourceType.SalesReturnInbound
                             && salesReturnDetailIds.Contains(ledger.SourceDetailId))
            .ToListAsync(cancellationToken);
        return new ManagedSalesReturnResult(
            salesReturnWasCreated,
            salesReturn.Details.Count,
            salesReturn.Details.Select(detail => detail.StockBatchId).Distinct().Count(),
            salesReturnLedgers.Count);
    }

    private static async Task<AfterSale> GetManagedAfterSaleAsync(
        ApplicationDbContext context,
        Guid afterSaleId,
        CancellationToken cancellationToken)
    {
        return await context.AfterSales
            .Include(afterSale => afterSale.Goods)
            .Include(afterSale => afterSale.AuditLogs)
            .Include(afterSale => afterSale.PickupTasks)
            .ThenInclude(task => task.StockInDetail)
            .ThenInclude(detail => detail!.StockInOrder)
            .ThenInclude(order => order.Details)
            .SingleAsync(afterSale => afterSale.Id == afterSaleId, cancellationToken);
    }

    private static async Task<StockInOrder> GetManagedSalesReturnStockInAsync(
        ApplicationDbContext context,
        Guid stockInOrderId,
        CancellationToken cancellationToken)
    {
        return await context.StockInOrders
            .Include(order => order.Details)
            .ThenInclude(detail => detail.StockBatch)
            .SingleAsync(order => order.Id == stockInOrderId, cancellationToken);
    }

    private sealed record AfterSaleScenario(
        AfterSaleStatus TargetStatus,
        AfterSaleType AfterSaleType,
        AfterSaleReasonType ReasonType,
        AfterSaleHandleType HandleType);

    private sealed record AfterSaleGenerationResult
    {
        public required GenerationLayerCount AfterSales { get; init; }

        public required GenerationLayerCount AfterSaleGoods { get; init; }

        public required GenerationLayerCount AfterSaleAuditLogs { get; init; }

        public required GenerationLayerCount PickupTasks { get; init; }

        public required GenerationLayerCount SalesReturnStockIns { get; init; }

        public required GenerationLayerCount SalesReturnStockInDetails { get; init; }

        public required GenerationLayerCount SalesReturnBatches { get; init; }

        public required GenerationLayerCount SalesReturnLedgers { get; init; }

        public required GenerationLayerCount CustomerBillDetails { get; init; }
    }

    private sealed record GenerationLayerCount
    {
        public required int Created { get; init; }

        public required int Reused { get; init; }
    }

    private sealed record ManagedSalesReturnResult(
        bool WasCreated,
        int DetailCount,
        int BatchCount,
        int LedgerCount);

    private static AfterSaleScenario ResolveAfterSaleScenario(int sequence)
    {
        return sequence switch
        {
            <= 5 => new AfterSaleScenario(
                AfterSaleStatus.Draft,
                AfterSaleType.RefundOnly,
                AfterSaleReasonType.OrderingError,
                AfterSaleHandleType.CustomerCommunication),
            <= 10 => new AfterSaleScenario(
                AfterSaleStatus.PendingAudit,
                AfterSaleType.RefundOnly,
                AfterSaleReasonType.LateDelivery,
                AfterSaleHandleType.BillAdjustment),
            <= 15 => new AfterSaleScenario(
                AfterSaleStatus.RefundPending,
                AfterSaleType.RefundOnly,
                AfterSaleReasonType.QuantityMismatch,
                AfterSaleHandleType.GoodsDiscount),
            <= 20 => new AfterSaleScenario(
                AfterSaleStatus.ReturnPending,
                AfterSaleType.ReturnAndRefund,
                AfterSaleReasonType.SpecificationMismatch,
                AfterSaleHandleType.Exchange),
            _ => new AfterSaleScenario(
                AfterSaleStatus.Completed,
                AfterSaleType.ReturnAndRefund,
                AfterSaleReasonType.QualityIssue,
                AfterSaleHandleType.GoodsDiscount)
        };
    }

    private static decimal CreateAfterSaleRefundQuantity(SaleOrderDetail detail, AfterSaleScenario scenario)
    {
        var availableBaseQuantity = detail.CustomerCheckBaseQuantity ?? detail.BaseQuantity;
        if (detail.UnitConversion <= 0m || availableBaseQuantity <= 0m || detail.Quantity <= 0m)
        {
            throw new InvalidOperationException(
                $"订单明细 {detail.GoodsCodeSnapshot} 的可售后数量不足以生成联调售后样本。");
        }

        var availableQuantity = NumericPrecision.RoundQuantity(
            availableBaseQuantity * detail.Quantity / detail.BaseQuantity);
        var ratio = scenario.TargetStatus == AfterSaleStatus.Completed ? 0.35m : 0.2m;
        var quantity = NumericPrecision.RoundQuantity(availableQuantity * ratio);
        if (quantity <= 0m)
        {
            quantity = NumericPrecision.RoundQuantity(Math.Min(availableQuantity, 0.5m));
        }

        if (quantity <= 0m || quantity > availableQuantity)
        {
            throw new InvalidOperationException(
                $"订单明细 {detail.GoodsCodeSnapshot} 的可售后数量不足以生成联调售后样本。");
        }

        return quantity;
    }

    private static void ApplyManagedAfterSaleFields(
        AfterSale afterSale,
        int sequence,
        DemoAuditUser auditUser)
    {
        afterSale.Remark = CreateAfterSaleRemark(sequence);
        afterSale.ContactNameSnapshot ??= $"华东售后联系人{sequence:D2}";
        afterSale.ContactPhoneSnapshot ??= $"1398000{sequence:D4}";
        afterSale.PickupAddressSnapshot ??= $"上海市浦东新区鲜品大道{sequence % 30 + 1}号客户退货月台{sequence:D2}";
        if (afterSale.CreateBy != auditUser.Id || afterSale.CreateName != auditUser.Username)
        {
            afterSale.CreateBy = auditUser.Id;
            afterSale.CreateName = auditUser.Username;
        }

        foreach (var goods in afterSale.Goods.OrderBy(item => item.GoodsCodeSnapshot, StringComparer.Ordinal))
        {
            goods.Remark = CreateAfterSaleGoodsRemark(sequence, goods.GoodsCodeSnapshot);
            if (goods.CreateBy != auditUser.Id || goods.CreateName != auditUser.Username)
            {
                goods.CreateBy = auditUser.Id;
                goods.CreateName = auditUser.Username;
            }
        }

        foreach (var log in afterSale.AuditLogs.OrderBy(item => item.AuditTime).ThenBy(item => item.Id))
        {
            log.Remark = log.Action switch
            {
                AfterSaleAuditAction.Submit => CreateAfterSaleSubmitRemark(sequence),
                AfterSaleAuditAction.Approve => CreateAfterSaleApproveRemark(sequence),
                AfterSaleAuditAction.Reject => CreateAfterSaleRejectRemark(sequence),
                _ => log.Remark
            };
            if (log.CreateBy != auditUser.Id || log.CreateName != auditUser.Username)
            {
                log.CreateBy = auditUser.Id;
                log.CreateName = auditUser.Username;
            }
        }

        foreach (var task in afterSale.PickupTasks)
        {
            task.Remark = CreatePickupAssignRemark(sequence);
            if (task.CreateBy != auditUser.Id || task.CreateName != auditUser.Username)
            {
                task.CreateBy = auditUser.Id;
                task.CreateName = auditUser.Username;
            }
        }
    }

    private static void ApplyManagedSalesReturnStockInFields(
        StockInOrder stockIn,
        int sequence,
        DemoAuditUser auditUser)
    {
        stockIn.Remark = CreateSalesReturnStockInRemark(sequence);
        if (stockIn.CreateBy != auditUser.Id || stockIn.CreateName != auditUser.Username)
        {
            stockIn.CreateBy = auditUser.Id;
            stockIn.CreateName = auditUser.Username;
        }

        var orderedDetails = stockIn.Details
            .OrderBy(detail => detail.GoodsCodeSnapshot, StringComparer.Ordinal)
            .ToArray();
        for (var detailIndex = 0; detailIndex < orderedDetails.Length; detailIndex++)
        {
            var detail = orderedDetails[detailIndex];
            detail.Remark = CreateSalesReturnDetailRemark(sequence, detail.GoodsCodeSnapshot);
            detail.BatchNo = CreateSalesReturnBatchNo(sequence);
            detail.ProductDate ??= CreateSalesReturnProductDate(sequence);
            detail.ExpireDate ??= CreateSalesReturnExpireDate(sequence);
            if (detail.CreateBy != auditUser.Id || detail.CreateName != auditUser.Username)
            {
                detail.CreateBy = auditUser.Id;
                detail.CreateName = auditUser.Username;
            }
        }
    }

    private static string CreateAfterSaleRemark(int sequence)
    {
        var stableKey = DemoDataStableKeyCatalog.Create("AFTER-SALE", sequence);
        return $"{stableKey} 华东联调售后单{sequence:D2}：基于已签收销售订单形成退款、退货、取货和账单冲减样本。";
    }

    private static string CreateAfterSaleGoodsRemark(int sequence, string goodsCode)
    {
        return $"SkyRoc 联调售后商品：第 {sequence:D2} 张售后单商品 {goodsCode} 的原因说明与处理依据。";
    }

    private static string CreateAfterSaleSubmitRemark(int sequence)
    {
        return $"SkyRoc 联调售后提交：第 {sequence:D2} 张售后单已提交运营审核。";
    }

    private static string CreateAfterSaleApproveRemark(int sequence)
    {
        return $"SkyRoc 联调售后审核通过：第 {sequence:D2} 张售后单同意按申请类型处理。";
    }

    private static string CreateAfterSaleRejectRemark(int sequence)
    {
        return $"SkyRoc 联调售后驳回：第 {sequence:D2} 张售后单需补充现场照片后重提。";
    }

    private static string CreatePickupAssignRemark(int sequence)
    {
        return $"SkyRoc 联调取货任务：第 {sequence:D2} 张售后退货任务已分配司机并完成上门回收。";
    }

    private static DateTime CreatePickupPlannedTime(int sequence)
    {
        return new DateTime(2026, 7, (sequence - 1) % 28 + 1, 9, 30, 0, DateTimeKind.Utc);
    }

    private static string CreateSalesReturnStockInRemark(int sequence)
    {
        var stableKey = DemoDataStableKeyCatalog.Create("SALES-RETURN-STOCK-IN", sequence);
        return $"{stableKey} 华东联调销售退货入库{sequence:D2}：来源受管售后取货任务，审核后回补库存并支撑售后完成。";
    }

    private static string CreateSalesReturnDetailRemark(int sequence, string goodsCode)
    {
        return $"SkyRoc 联调销售退货入库明细：第 {sequence:D2} 张退货入库商品 {goodsCode}，按批准数量回补库存。";
    }

    private static string CreateSalesReturnAuditRemark(int sequence)
    {
        return $"SkyRoc 联调销售退货入库审核：确认第 {sequence:D2} 张售后取货商品质检合格并回补库存。";
    }

    private static string CreateSalesReturnBatchNo(int sequence)
    {
        return $"SR-{DemoDataStableKeyCatalog.Create("AFTER-SALE", sequence)}";
    }

    private static DateTime CreateSalesReturnInTime(int sequence)
    {
        return new DateTime(2026, 7, (sequence - 1) % 28 + 1, 11, 0, 0, DateTimeKind.Utc);
    }

    private static DateOnly CreateSalesReturnProductDate(int sequence)
    {
        return new DateOnly(2026, 6, (sequence - 1) % 28 + 1);
    }

    private static DateOnly CreateSalesReturnExpireDate(int sequence)
    {
        return new DateOnly(2026, 8, (sequence - 1) % 28 + 1);
    }

    private static string SerializeGoodsInfo(Domain.Entities.Goods.Goods goods)
    {
        return JsonSerializer.Serialize(new
        {
            goods.Spec,
            goods.Brand,
            goods.Origin
        });
    }

    private static async Task ApplySaleOrderTargetStatusAsync(
        ApplicationDbContext context,
        ISaleOrderService saleOrderService,
        Guid orderId,
        SaleOrderStatus targetStatus,
        string auditRemark,
        DemoAuditUser auditUser,
        CancellationToken cancellationToken)
    {
        var order = await context.SaleOrders
            .Include(item => item.AuditLogs)
            .SingleAsync(item => item.Id == orderId, cancellationToken);
        if (order.OrderStatus == targetStatus)
            return;

        if (targetStatus == SaleOrderStatus.SortingPending
            && order.OrderStatus is SaleOrderStatus.Delivering or SaleOrderStatus.Signed)
        {
            return;
        }

        if (order.OrderStatus == SaleOrderStatus.PendingAudit && targetStatus == SaleOrderStatus.SortingPending)
        {
            await saleOrderService.ApproveAsync(orderId, auditRemark);
            return;
        }

        if (order.OrderStatus == SaleOrderStatus.PendingAudit && targetStatus == SaleOrderStatus.Rejected)
        {
            await saleOrderService.RejectAsync(orderId, auditRemark);
            return;
        }

        if (order.OrderStatus == SaleOrderStatus.Rejected && targetStatus == SaleOrderStatus.PendingAudit)
        {
            await saleOrderService.ResubmitAsync(orderId, auditRemark);
            return;
        }

        // 受管联调订单可能因生成规则升级需要状态校准；服务没有任意状态回退入口，故只补写完整稳定键订单。
        order.OrderStatus = targetStatus;
        order.UpdateBy = auditUser.Id;
        order.UpdateName = auditUser.Username;
    }

    private static IReadOnlyList<CompanySeed> CreateCompanySeeds()
    {
        return Enumerable.Range(1, 30)
            .Select(sequence => new CompanySeed(
                DemoDataStableKeyCatalog.Create("COMPANY", sequence),
                $"华东鲜品供应链有限公司{sequence:D2}",
                $"陈经理{sequence:D2}",
                $"021-6800{sequence:D4}",
                $"上海市浦东新区鲜品大道{sequence}号冷链供应中心",
                $"SkyRoc 联调公司资料：华东区域第 {sequence:D2} 个采购与配送业务主体。"))
            .ToArray();
    }

    private static IReadOnlyList<CarrierSeed> CreateCarrierSeeds()
    {
        return Enumerable.Range(1, 30)
            .Select(sequence => new CarrierSeed(
                DemoDataStableKeyCatalog.Create("CARRIER", sequence),
                $"华东冷链联调承运商{sequence:D2}",
                $"周调度{sequence:D2}",
                $"021-6500{sequence:D4}",
                $"上海市浦东新区冷链配送大道{sequence}号承运服务中心",
                $"SkyRoc 联调承运商资料：华东第 {sequence:D2} 个冷链配送合作伙伴，支持司机分配与配送履约。"))
            .ToArray();
    }

    private static IReadOnlyList<CustomerTagSeed> CreateCustomerTagSeeds()
    {
        return Enumerable.Range(1, 30)
            .Select(sequence => new CustomerTagSeed(
                DemoDataStableKeyCatalog.Create("CUSTOMER-TAG", sequence),
                $"华东联调客户分群{sequence:D2}",
                sequence,
                $"SkyRoc 联调客户标签：覆盖华东第 {sequence:D2} 个客户服务与价格分群。"))
            .ToArray();
    }

    private static IReadOnlyList<CustomerSeed> CreateCustomerSeeds()
    {
        return Enumerable.Range(1, 30)
            .Select(sequence => new CustomerSeed(
                DemoDataStableKeyCatalog.Create("CUSTOMER", sequence),
                DemoDataStableKeyCatalog.Create("COMPANY", sequence),
                DemoDataStableKeyCatalog.Create("CUSTOMER-TAG", sequence),
                $"华东鲜品团餐客户服务中心{sequence:D2}",
                $"91310115DEMO{sequence:D6}",
                $"李主任{sequence:D2}",
                $"{500 + sequence * 10}万元人民币",
                new DateTime(2020 + sequence % 5, sequence % 12 + 1, sequence % 27 + 1, 0, 0, 0, DateTimeKind.Utc),
                "2020-01-01 至 2040-12-31",
                "存续",
                "上海市浦东新区市场监督管理局",
                $"上海市浦东新区鲜品大道{sequence}号客户服务楼",
                "团餐配送、食材采购、食品销售及供应链管理服务。",
                $"华东鲜品团餐客户服务中心{sequence:D2}",
                $"91310115DEMO{sequence:D6}",
                $"上海市浦东新区鲜品大道{sequence}号客户服务楼",
                $"021-6800{sequence:D4}",
                "上海农商银行浦东鲜品支行",
                $"622580100000{sequence:D6}",
                $"王会计{sequence:D2}",
                $"1390020{sequence:D4}",
                $"上海市浦东新区鲜品大道{sequence}号客户收票室",
                $"billing{sequence:D2}@eastfresh.example",
                $"周经理{sequence:D2}",
                $"1380010{sequence:D4}",
                $"上海市浦东新区鲜品大道{sequence}号配送收货区",
                $"SkyRoc 联调客户资料：华东第 {sequence:D2} 个团餐客户，覆盖采购、配送和结算场景。"))
            .ToArray();
    }

    private static IReadOnlyList<CustomerSubAccountSeed> CreateCustomerSubAccountSeeds()
    {
        return Enumerable.Range(1, 30)
            .Select(sequence => new CustomerSubAccountSeed(
                DemoDataStableKeyCatalog.Create("CUSTOMER-SUB-ACCOUNT", sequence),
                DemoDataStableKeyCatalog.Create("COMPANY", sequence),
                DemoDataStableKeyCatalog.Create("CUSTOMER", sequence),
                $"华东联调客户采购账号{sequence:D2}",
                $"1368000{sequence:D4}",
                $"customer.buyer{sequence:D2}@eastfresh.example",
                $"SkyRoc 联调客户子账号：为客户 {sequence:D2} 维护下单与订单查询授权，不承载系统管理员权限。"))
            .ToArray();
    }

    private static IReadOnlyList<DepartmentSeed> CreateDepartmentSeeds()
    {
        return Enumerable.Range(1, 30)
            .Select(sequence => new DepartmentSeed(
                DemoDataStableKeyCatalog.Create("DEPARTMENT", sequence),
                sequence <= 6 ? null : DemoDataStableKeyCatalog.Create("DEPARTMENT", (sequence - 1) % 6 + 1),
                $"华东联调运营部门{sequence:D2}",
                $"1376000{sequence:D4}",
                $"department{sequence:D2}@eastfresh.example",
                sequence,
                $"SkyRoc 联调部门资料：华东第 {sequence:D2} 个运营组织单元，覆盖员工归属、采购责任与配送协同。"))
            .ToArray();
    }

    private static IReadOnlyList<DriverSeed> CreateDriverSeeds()
    {
        return Enumerable.Range(1, 30)
            .Select(sequence => new DriverSeed(
                DemoDataStableKeyCatalog.Create("DRIVER", sequence),
                DemoDataStableKeyCatalog.Create("CARRIER", sequence),
                $"华东联调配送司机{sequence:D2}",
                $"1377000{sequence:D4}",
                $"沪A{sequence:D5}",
                $"3101151988{sequence:D8}",
                $"SkyRoc 联调司机资料：华东第 {sequence:D2} 名配送司机，使用指定承运商和车辆执行客户履约。"))
            .ToArray();
    }

    private static IReadOnlyList<DeliveryRouteSeed> CreateDeliveryRouteSeeds()
    {
        return Enumerable.Range(1, 30)
            .Select(sequence => new DeliveryRouteSeed(
                DemoDataStableKeyCatalog.Create("DELIVERY-ROUTE", sequence),
                DemoDataStableKeyCatalog.Create("CUSTOMER", sequence),
                $"华东联调配送路线{sequence:D2}",
                $"覆盖浦东新区鲜品大道第 {sequence:D2} 个客户服务片区，按冷链车辆优先顺序履约。",
                sequence,
                $"SkyRoc 联调配送路线：为客户 {sequence:D2} 建立稳定路线关系，支持智能规划与配送状态流转。"))
            .ToArray();
    }

    private static IReadOnlyList<GoodsTypeSeed> CreateGoodsTypeSeeds()
    {
        return Enumerable.Range(1, 30)
            .Select(sequence => new GoodsTypeSeed(
                DemoDataStableKeyCatalog.Create("GOODS-TYPE", sequence),
                $"华东联调生鲜分类{sequence:D2}",
                $"https://assets.skyroc.example/goods-types/east-fresh-{sequence:D2}.png",
                $"1010{sequence:D6}",
                $"华东生鲜食材税收分类{sequence:D2}",
                sequence % 2 == 0 ? "生鲜蔬菜" : "冷链食材",
                sequence % 5 == 0 ? 0m : 0.09m,
                sequence % 5 == 0,
                sequence % 5 == 0 ? "农产品流通环节免征增值税政策。" : null,
                sequence,
                $"SkyRoc 联调商品分类：华东第 {sequence:D2} 个生鲜品类，用于商品建档、报价和采购规则。"))
            .ToArray();
    }

    private static IReadOnlyList<GoodsSeed> CreateGoodsSeeds()
    {
        return Enumerable.Range(1, 30)
            .Select(sequence => new GoodsSeed(
                DemoDataStableKeyCatalog.Create("GOODS", sequence),
                DemoDataStableKeyCatalog.Create("GOODS-UNIT", sequence),
                DemoDataStableKeyCatalog.Create("GOODS-TYPE", sequence),
                DemoDataStableKeyCatalog.Create("SUPPLIER", sequence),
                DemoDataStableKeyCatalog.Create("WARE", sequence),
                $"华东联调生鲜商品{sequence:D2}",
                $"{10 + sequence} 千克/箱",
                sequence % 2 == 0 ? "东海鲜选" : "浦江农鲜",
                sequence % 2 == 0 ? "上海崇明生态种植基地" : "江苏盐城冷链集散基地",
                $"华东联调生鲜商品{sequence:D2}，用于报价、采购、库存和配送完整链路。",
                sequence % 5 == 0 ? 0m : 0.09m,
                $"千克{sequence:D2}",
                $"SkyRoc 联调商品单位：商品 {sequence:D2} 的基础计量单位，用于数量换算与库存台账。",
                $"SkyRoc 联调商品资料：华东第 {sequence:D2} 个商品，覆盖销售、采购、库存与配送场景。"))
            .ToArray();
    }

    private static IReadOnlyList<WareSeed> CreateWareSeeds()
    {
        return Enumerable.Range(1, 30)
            .Select(sequence => new WareSeed(
                DemoDataStableKeyCatalog.Create("WARE", sequence),
                $"华东冷链联调仓库{sequence:D2}",
                $"赵库管{sequence:D2}",
                $"021-6900{sequence:D4}",
                $"上海市浦东新区冷链物流园{sequence}号仓储中心",
                sequence,
                $"SkyRoc 联调仓库资料：华东第 {sequence:D2} 个冷链仓储节点，支持订单履约与库存批次场景。"))
            .ToArray();
    }

    private static IReadOnlyList<PurchaserSeed> CreatePurchaserSeeds()
    {
        return Enumerable.Range(1, 30)
            .Select(sequence => new PurchaserSeed(
                sequence,
                DemoDataStableKeyCatalog.Create("PURCHASER", sequence),
                $"华东联调采购专员{sequence:D2}",
                $"1389030{sequence:D4}",
                $"SkyRoc 联调采购员资料：华东第 {sequence:D2} 个采购责任岗位，覆盖采购计划、采购单与入库场景。"))
            .ToArray();
    }

    private static IReadOnlyList<PurchaseRuleSeed> CreatePurchaseRuleSeeds()
    {
        return Enumerable.Range(1, 30)
            .Select(sequence => new PurchaseRuleSeed(
                DemoDataStableKeyCatalog.Create("PURCHASE-RULE", sequence),
                DemoDataStableKeyCatalog.Create("SUPPLIER", sequence),
                DemoDataStableKeyCatalog.Create("PURCHASER", sequence),
                DemoDataStableKeyCatalog.Create("WARE", sequence),
                DemoDataStableKeyCatalog.Create("GOODS-TYPE", sequence),
                DemoDataStableKeyCatalog.Create("GOODS", sequence),
                DemoDataStableKeyCatalog.Create("CUSTOMER", sequence),
                $"华东联调采购适用规则{sequence:D2}",
                sequence % 2 == 0 ? 2 : 1,
                $"SkyRoc 联调采购规则：客户 {sequence:D2} 的商品 {sequence:D2} 由指定供应商、采购员和仓库按既定采购模式履约。"))
            .ToArray();
    }

    private static IReadOnlyList<SaleOrderSeed> CreateSaleOrderSeeds()
    {
        return Enumerable.Range(1, 60)
            .Select(sequence =>
            {
                var primary = (sequence - 1) % 30 + 1;
                var secondary = sequence % 30 + 1;
                return new SaleOrderSeed(
                    sequence,
                    DemoDataStableKeyCatalog.Create("SALE-ORDER", sequence),
                    DemoDataStableKeyCatalog.Create("CUSTOMER", primary),
                    DemoDataStableKeyCatalog.Create("QUOTATION", primary),
                    DemoDataStableKeyCatalog.Create("WARE", primary),
                    new DateTime(2026, 7, sequence % 28 + 1, 6 + sequence % 8, 15, 0, DateTimeKind.Utc),
                    new DateTime(2026, 7, sequence % 28 + 1, 14 + sequence % 8, 30, 0, DateTimeKind.Utc),
                    $"华东联调订单联系人{sequence:D2}",
                    $"1385000{sequence:D4}",
                    $"上海市浦东新区鲜品大道{primary}号客户配送月台{sequence:D2}",
                    $"SkyRoc 联调销售订单：第 {sequence:D2} 张长期订单，覆盖客户下单、审核、采购计划和销售出库来源。",
                    sequence % 6 == 0
                        ? SaleOrderStatus.Rejected
                        : sequence % 3 == 0
                            ? SaleOrderStatus.PendingAudit
                            : SaleOrderStatus.SortingPending,
                    $"SkyRoc 联调订单审核意见：第 {sequence:D2} 张订单按状态样本进入后续链路。",
                    [
                        new SaleOrderDetailSeed(
                            DemoDataStableKeyCatalog.Create("GOODS", primary),
                            DemoDataStableKeyCatalog.Create("GOODS-UNIT", primary),
                            NumericPrecision.RoundQuantity(4.5m + sequence % 7),
                            NumericPrecision.RoundMoney(9.25m + primary),
                            $"SkyRoc 联调订单明细：主商品 {primary:D2} 用于订单、采购和库存链路。",
                            $"SkyRoc 联调订单内部备注：主商品 {primary:D2} 需按客户交期优先履约。"),
                        new SaleOrderDetailSeed(
                            DemoDataStableKeyCatalog.Create("GOODS", secondary),
                            DemoDataStableKeyCatalog.Create("GOODS-UNIT", secondary),
                            NumericPrecision.RoundQuantity(2.25m + sequence % 5),
                            NumericPrecision.RoundMoney(8.75m + secondary),
                            $"SkyRoc 联调订单明细：搭配商品 {secondary:D2} 用于多明细金额和快照校验。",
                            $"SkyRoc 联调订单内部备注：搭配商品 {secondary:D2} 需与主商品同车配送。")
                    ]);
            })
            .ToArray();
    }

    private static IReadOnlyList<SupplierSeed> CreateSupplierSeeds()
    {
        return Enumerable.Range(1, 30)
            .Select(sequence => new SupplierSeed(
                DemoDataStableKeyCatalog.Create("SUPPLIER", sequence),
                $"华东生鲜联调供应商{sequence:D2}",
                $"孙采购{sequence:D2}",
                $"021-6700{sequence:D4}",
                $"上海市嘉定区鲜品采购路{sequence}号供应中心",
                "上海农商银行嘉定生鲜支行",
                $"622581200000{sequence:D6}",
                $"91310114SUP{sequence:D6}",
                $"SkyRoc 联调供应商资料：华东第 {sequence:D2} 个生鲜供应伙伴，覆盖采购、入库与供应商结算场景。"))
            .ToArray();
    }

    private static IReadOnlyList<SystemRoleSeed> CreateSystemRoleSeeds()
    {
        return Enumerable.Range(1, 30)
            .Select(sequence => new SystemRoleSeed(
                DemoDataStableKeyCatalog.Create("SYSTEM-ROLE", sequence),
                $"华东联调运营角色{sequence:D2}",
                $"SkyRoc 联调系统角色：华东业务第 {sequence:D2} 个运营岗位，拥有首页与系统管理菜单访问范围。"))
            .ToArray();
    }

    private static IReadOnlyList<SystemUserSeed> CreateSystemUserSeeds()
    {
        return Enumerable.Range(1, 30)
            .Select(sequence => new SystemUserSeed(
                sequence,
                DemoDataStableKeyCatalog.Create("SYSTEM-USER", sequence),
                DemoDataStableKeyCatalog.Create("SYSTEM-ROLE", sequence),
                $"华东联调运营专员{sequence:D2}",
                sequence % 2 == 0 ? GenderType.Female : GenderType.Male,
                $"1397000{sequence:D4}",
                $"system.operator{sequence:D2}@eastfresh.example"))
            .ToArray();
    }

    private static IReadOnlyList<QuotationSeed> CreateQuotationSeeds()
    {
        return Enumerable.Range(1, 30)
            .Select(sequence => new QuotationSeed(
                DemoDataStableKeyCatalog.Create("QUOTATION", sequence),
                DemoDataStableKeyCatalog.Create("CUSTOMER", sequence),
                DemoDataStableKeyCatalog.Create("GOODS", sequence),
                DemoDataStableKeyCatalog.Create("GOODS-UNIT", sequence),
                $"华东联调客户报价方案{sequence:D2}",
                $"面向华东联调客户 {sequence:D2} 的生鲜商品销售价格，覆盖订单计价、起订量与有效期校验。",
                new DateTime(2026, sequence % 12 + 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2028, sequence % 12 + 1, 28, 23, 59, 59, DateTimeKind.Utc),
                sequence % 5 != 0,
                8.5m + sequence,
                5m + sequence % 4,
                $"SkyRoc 联调报价商品：商品 {sequence:D2} 的有效客户销售价格与最小起订量。"))
            .ToArray();
    }

    private static IReadOnlyList<CustomerProtocolSeed> CreateCustomerProtocolSeeds()
    {
        return Enumerable.Range(1, 30)
            .Select(sequence => new CustomerProtocolSeed(
                DemoDataStableKeyCatalog.Create("CUSTOMER-PROTOCOL", sequence),
                DemoDataStableKeyCatalog.Create("QUOTATION", sequence),
                DemoDataStableKeyCatalog.Create("CUSTOMER", sequence),
                DemoDataStableKeyCatalog.Create("GOODS", sequence),
                DemoDataStableKeyCatalog.Create("GOODS-UNIT", sequence),
                $"华东联调客户协议价{sequence:D2}",
                new DateTime(2026, sequence % 12 + 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2029, sequence % 12 + 1, 28, 23, 59, 59, DateTimeKind.Utc),
                NumericPrecision.RoundMoney(7.25m + sequence),
                NumericPrecision.RoundQuantity(3m + sequence % 5),
                $"SkyRoc 联调协议价商品：客户 {sequence:D2} 在有效期内采购商品 {sequence:D2} 的专属价格与起订量。",
                $"SkyRoc 联调客户协议价：为客户 {sequence:D2} 绑定报价、商品和协议有效期，覆盖订单价格优先级场景。"))
            .ToArray();
    }

    private static IReadOnlyList<ServicePeriodSeed> CreateServicePeriodSeeds()
    {
        return Enumerable.Range(1, 30)
            .Select(sequence =>
            {
                var hour = 5 + sequence % 12;
                return new ServicePeriodSeed(
                    $"{DemoDataStableKeyCatalog.Create("SERVICE-PERIOD", sequence)} 华东运营服务时段{sequence:D2}",
                    new TimeOnly(hour, sequence % 2 == 0 ? 0 : 30),
                    new TimeOnly(hour + 2, sequence % 2 == 0 ? 30 : 0),
                    sequence,
                    sequence % 6 != 0);
            })
            .ToArray();
    }

    private static IReadOnlyList<NoticeSeed> CreateNoticeSeeds()
    {
        return Enumerable.Range(1, 30)
            .Select(sequence => new NoticeSeed(
                $"{DemoDataStableKeyCatalog.Create("NOTICE", sequence)} 前端联调公告{sequence:D2}",
                $"SkyRoc 联调公告第 {sequence:D2} 条：提醒华东运营、采购、配送和财务团队关注当日订单履约、库存批次与结算核对事项。",
                sequence % 3 == 0 ? NoticeStatus.Draft : NoticeStatus.Published,
                new DateTime(2026, 7, sequence % 28 + 1, 8, sequence % 60, 0, DateTimeKind.Utc)))
            .ToArray();
    }

    private static IReadOnlyList<PrintTemplateSeed> CreatePrintTemplateSeeds()
    {
        var businessTypes = Enum.GetValues<PrintBusinessType>();
        return Enumerable.Range(1, 30)
            .Select(sequence => new PrintTemplateSeed(
                $"SKYROC_DEMO_PRINT_TEMPLATE_{sequence:D3}",
                $"华东联调打印模板{sequence:D2}",
                businessTypes[(sequence - 1) % businessTypes.Length],
                $$"""{"version":"1.0","purpose":"SkyRoc 华东联调打印模板 {{sequence:D2}}","layout":"standard-a4"}""",
                sequence % 5 != 0,
                [
                    new PrintTemplateFieldSeed("documentNo", "业务单据号", 0, null),
                    new PrintTemplateFieldSeed("businessTime", "业务时间", 1, "yyyy-MM-dd HH:mm"),
                    new PrintTemplateFieldSeed("totalAmount", "合计金额", 2, "0.00")
                ]))
            .ToArray();
    }

    private static IReadOnlyList<OperationLogSeed> CreateOperationLogSeeds()
    {
        var modules = new[] { "order", "purchase", "storage", "delivery", "finance", "traceability" };
        var methods = new[] { "POST", "PUT", "GET", "DELETE" };
        return Enumerable.Range(1, 120)
            .Select(sequence => new OperationLogSeed(
                sequence,
                modules[(sequence - 1) % modules.Length],
                sequence % 4 == 0 ? "Update" : sequence % 4 == 1 ? "Create" : sequence % 4 == 2 ? "Query" : "Audit",
                $"{DemoDataStableKeyCatalog.Create("OPERATION-LOG", sequence)} 自动业务联调审计样本{sequence:D3}",
                methods[(sequence - 1) % methods.Length],
                $"/api/demo-business/{modules[(sequence - 1) % modules.Length]}/{sequence:D3}",
                $$"""{"businessKey":"{{DemoDataStableKeyCatalog.Create("OPERATION-LOG", sequence)}}","source":"demo-data-generator"}""",
                $$"""{"code":200,"message":"联调审计样本已记录","sequence":{{sequence}}}""",
                $"10.20.{sequence / 255}.{sequence % 255}",
                sequence % 2 == 0 ? "上海市浦东新区" : "上海市嘉定区",
                sequence % 3 == 0 ? "Microsoft Edge" : "Chrome",
                sequence % 4 == 0 ? "Windows 11" : "Windows Server 2022",
                80 + sequence,
                sequence % 7 != 0,
                sequence % 7 == 0 ? "业务规则拒绝样本：状态不允许重复提交" : null))
            .ToArray();
    }

    private static IReadOnlyList<LoginLogSeed> CreateLoginLogSeeds()
    {
        return Enumerable.Range(1, 120)
            .Select(sequence => new LoginLogSeed(
                sequence,
                DemoDataStableKeyCatalog.Create("LOGIN-LOG", sequence),
                sequence % 5 != 0,
                sequence % 5 == 0 ? "凭据校验失败或账号未启用" : null,
                $"10.30.{sequence / 255}.{sequence % 255}",
                sequence % 2 == 0 ? "SkyRoc Frontend QA Browser" : "SkyRoc Mobile Joint Debug Client",
                new DateTime(2026, 7, sequence % 28 + 1, 9, sequence % 60, 0, DateTimeKind.Utc)))
            .ToArray();
    }

    private static Guid GetManagedReferenceId<T>(
        IReadOnlyDictionary<string, T> entities,
        string businessCode,
        string referenceName)
        where T : class
    {
        return entities.TryGetValue(businessCode, out var entity)
            ? entity switch
            {
                Domain.Entities.Customers.Company company => company.Id,
                Domain.Entities.Customers.Customer customer => customer.Id,
                Domain.Entities.Customers.CustomerTag customerTag => customerTag.Id,
                Domain.Entities.Department department => department.Id,
                Domain.Entities.Delivery.Carrier carrier => carrier.Id,
                Domain.Entities.Goods.GoodsType goodsType => goodsType.Id,
                Domain.Entities.Purchases.Supplier supplier => supplier.Id,
                Domain.Entities.Purchases.Purchaser purchaser => purchaser.Id,
                Domain.Entities.Storage.Ware ware => ware.Id,
                Domain.Entities.Goods.Goods goods => goods.Id,
                _ => throw new InvalidOperationException($"不支持的{referenceName}受管引用类型。")
            }
            : throw new InvalidOperationException($"未找到稳定编码为 {businessCode} 的受管{referenceName}。 ");
    }

    private static T GetManagedReference<T>(
        IReadOnlyDictionary<string, T> entities,
        string businessCode,
        string referenceName)
        where T : class
    {
        return entities.TryGetValue(businessCode, out var entity)
            ? entity
            : throw new InvalidOperationException($"未找到稳定编码为 {businessCode} 的受管{referenceName}。 ");
    }

    private static T GetManagedReference<TKey, T>(
        IReadOnlyDictionary<TKey, T> entities,
        TKey key,
        string referenceName)
        where TKey : notnull
        where T : class
    {
        return entities.TryGetValue(key, out var entity)
            ? entity
            : throw new InvalidOperationException($"未找到键为 {key} 的受管{referenceName}。 ");
    }

    private static void SetAuditUser(IHttpContextAccessor httpContextAccessor, DemoAuditUser auditUser)
    {
        httpContextAccessor.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, auditUser.Id.ToString()),
                new Claim(ClaimTypes.Name, auditUser.Username)
            ],
            "DemoDataGenerator"))
        };
    }

    private sealed record CompanySeed(
        string Code,
        string Name,
        string ContactName,
        string ContactPhone,
        string Address,
        string Remark)
    {
        public CreateCompanyDto ToCreateDto() => new()
        {
            Code = Code,
            Name = Name,
            ContactName = ContactName,
            ContactPhone = ContactPhone,
            Address = Address,
            Remark = Remark,
            Status = Status.Enable
        };

        public UpdateCompanyDto ToUpdateDto(Guid id) => new()
        {
            Id = id,
            Code = Code,
            Name = Name,
            ContactName = ContactName,
            ContactPhone = ContactPhone,
            Address = Address,
            Remark = Remark,
            Status = Status.Enable
        };

        public bool Matches(Domain.Entities.Customers.Company company)
        {
            return company.Name == Name
                   && company.ContactName == ContactName
                   && company.ContactPhone == ContactPhone
                   && company.Address == Address
                   && company.Remark == Remark
                   && company.Status == Status.Enable;
        }
    }

    private sealed record CustomerTagSeed(
        string Code,
        string Name,
        int Sort,
        string Remark)
    {
        public CreateCustomerTagDto ToCreateDto() => new()
        {
            Code = Code,
            Name = Name,
            Sort = Sort,
            Remark = Remark,
            Status = Status.Enable
        };

        public UpdateCustomerTagDto ToUpdateDto(Guid id) => new()
        {
            Id = id,
            Code = Code,
            Name = Name,
            Sort = Sort,
            Remark = Remark,
            Status = Status.Enable
        };

        public bool Matches(Domain.Entities.Customers.CustomerTag tag)
        {
            return tag.Name == Name
                   && tag.Sort == Sort
                   && tag.Remark == Remark
                   && tag.Status == Status.Enable
                   && tag.ParentId is null;
        }
    }

    private sealed record CustomerSeed(
        string Code,
        string CompanyCode,
        string CustomerTagCode,
        string Name,
        string UnifiedSocialCreditCode,
        string LegalRepresentative,
        string RegisteredCapital,
        DateTime EstablishDate,
        string BusinessTerm,
        string RegistrationStatus,
        string RegistrationAuthority,
        string RegisteredAddress,
        string BusinessScope,
        string InvoiceTitle,
        string TaxpayerIdentificationNumber,
        string InvoiceAddress,
        string InvoicePhone,
        string BankName,
        string BankAccount,
        string InvoiceReceiverName,
        string InvoiceReceiverPhone,
        string InvoiceReceiverAddress,
        string InvoiceEmail,
        string ContactName,
        string ContactPhone,
        string Address,
        string Remark)
    {
        public CreateCustomerDto ToCreateDto(Guid companyId, Guid customerTagId) => new()
        {
            Code = Code,
            Name = Name,
            CompanyId = companyId,
            UnifiedSocialCreditCode = UnifiedSocialCreditCode,
            LegalRepresentative = LegalRepresentative,
            RegisteredCapital = RegisteredCapital,
            EstablishDate = EstablishDate,
            BusinessTerm = BusinessTerm,
            RegistrationStatus = RegistrationStatus,
            RegistrationAuthority = RegistrationAuthority,
            RegisteredAddress = RegisteredAddress,
            BusinessScope = BusinessScope,
            InvoiceTitle = InvoiceTitle,
            TaxpayerIdentificationNumber = TaxpayerIdentificationNumber,
            InvoiceAddress = InvoiceAddress,
            InvoicePhone = InvoicePhone,
            BankName = BankName,
            BankAccount = BankAccount,
            InvoiceReceiverName = InvoiceReceiverName,
            InvoiceReceiverPhone = InvoiceReceiverPhone,
            InvoiceReceiverAddress = InvoiceReceiverAddress,
            InvoiceEmail = InvoiceEmail,
            ContactName = ContactName,
            ContactPhone = ContactPhone,
            Address = Address,
            Remark = Remark,
            TagIds = [customerTagId],
            Status = Status.Enable
        };

        public UpdateCustomerDto ToUpdateDto(
            Guid id,
            Guid? companyId,
            Guid customerTagId,
            Guid? quotationId = null,
            Guid? defaultWareId = null)
        {
            var dto = ToCreateDto(
                companyId ?? throw new InvalidOperationException("受管客户必须关联受管公司。"),
                customerTagId);
            return new UpdateCustomerDto
            {
                Id = id,
                Code = dto.Code,
                Name = dto.Name,
                CompanyId = dto.CompanyId,
                QuotationId = quotationId,
                DefaultWareId = defaultWareId,
                UnifiedSocialCreditCode = dto.UnifiedSocialCreditCode,
                LegalRepresentative = dto.LegalRepresentative,
                RegisteredCapital = dto.RegisteredCapital,
                EstablishDate = dto.EstablishDate,
                BusinessTerm = dto.BusinessTerm,
                RegistrationStatus = dto.RegistrationStatus,
                RegistrationAuthority = dto.RegistrationAuthority,
                RegisteredAddress = dto.RegisteredAddress,
                BusinessScope = dto.BusinessScope,
                InvoiceTitle = dto.InvoiceTitle,
                TaxpayerIdentificationNumber = dto.TaxpayerIdentificationNumber,
                InvoiceAddress = dto.InvoiceAddress,
                InvoicePhone = dto.InvoicePhone,
                BankName = dto.BankName,
                BankAccount = dto.BankAccount,
                InvoiceReceiverName = dto.InvoiceReceiverName,
                InvoiceReceiverPhone = dto.InvoiceReceiverPhone,
                InvoiceReceiverAddress = dto.InvoiceReceiverAddress,
                InvoiceEmail = dto.InvoiceEmail,
                ContactName = dto.ContactName,
                ContactPhone = dto.ContactPhone,
                Address = dto.Address,
                Remark = dto.Remark,
                TagIds = dto.TagIds,
                Status = dto.Status
            };
        }

        public bool Matches(Domain.Entities.Customers.Customer customer, Guid companyId, Guid customerTagId)
        {
            return customer.CompanyId == companyId
                   && customer.Name == Name
                   && customer.UnifiedSocialCreditCode == UnifiedSocialCreditCode
                   && customer.LegalRepresentative == LegalRepresentative
                   && customer.RegisteredCapital == RegisteredCapital
                   && customer.EstablishDate == EstablishDate
                   && customer.BusinessTerm == BusinessTerm
                   && customer.RegistrationStatus == RegistrationStatus
                   && customer.RegistrationAuthority == RegistrationAuthority
                   && customer.RegisteredAddress == RegisteredAddress
                   && customer.BusinessScope == BusinessScope
                   && customer.InvoiceTitle == InvoiceTitle
                   && customer.TaxpayerIdentificationNumber == TaxpayerIdentificationNumber
                   && customer.InvoiceAddress == InvoiceAddress
                   && customer.InvoicePhone == InvoicePhone
                   && customer.BankName == BankName
                   && customer.BankAccount == BankAccount
                   && customer.InvoiceReceiverName == InvoiceReceiverName
                   && customer.InvoiceReceiverPhone == InvoiceReceiverPhone
                   && customer.InvoiceReceiverAddress == InvoiceReceiverAddress
                   && customer.InvoiceEmail == InvoiceEmail
                   && customer.ContactName == ContactName
                   && customer.ContactPhone == ContactPhone
                   && customer.Address == Address
                   && customer.Remark == Remark
                   && customer.Status == Status.Enable
                   && customer.TagRelations.Count == 1
                   && customer.TagRelations.Single().CustomerTagId == customerTagId;
        }
    }

    private sealed record CustomerSubAccountSeed(
        string Username,
        string CompanyCode,
        string CustomerCode,
        string NickName,
        string Phone,
        string Email,
        string Remark)
    {
        private string PasswordHash => Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes($"SkyRoc customer sub-account::{Username}")));

        public CreateCustomerSubAccountDto ToCreateDto(Guid companyId, Guid customerId) => new()
        {
            CompanyId = companyId,
            CustomerId = customerId,
            Username = Username,
            NickName = NickName,
            Phone = Phone,
            Email = Email,
            PasswordHash = PasswordHash,
            Remark = Remark,
            Status = Status.Enable
        };

        public UpdateCustomerSubAccountDto ToUpdateDto(Guid id, Guid companyId, Guid customerId) => new()
        {
            Id = id,
            CompanyId = companyId,
            CustomerId = customerId,
            Username = Username,
            NickName = NickName,
            Phone = Phone,
            Email = Email,
            PasswordHash = PasswordHash,
            Remark = Remark,
            Status = Status.Enable
        };

        public bool Matches(Domain.Entities.Customers.CustomerSubAccount subAccount, Guid companyId, Guid customerId)
        {
            return subAccount.CompanyId == companyId
                   && subAccount.CustomerId == customerId
                   && subAccount.NickName == NickName
                   && subAccount.Phone == Phone
                   && subAccount.Email == Email
                   && subAccount.PasswordHash == PasswordHash
                   && subAccount.Remark == Remark
                   && subAccount.Status == Status.Enable;
        }
    }

    private sealed record GoodsTypeSeed(
        string Code,
        string Name,
        string ImageUrl,
        string TaxCategoryCode,
        string TaxCategoryName,
        string InvoiceGoodsShortName,
        decimal DefaultTaxRate,
        bool IsTaxExempt,
        string? TaxPolicyBasis,
        int Sort,
        string Remark)
    {
        public CreateGoodsTypeDto ToCreateDto() => new()
        {
            Code = Code,
            Name = Name,
            ImageUrl = ImageUrl,
            TaxCategoryCode = TaxCategoryCode,
            TaxCategoryName = TaxCategoryName,
            InvoiceGoodsShortName = InvoiceGoodsShortName,
            DefaultTaxRate = DefaultTaxRate,
            IsTaxExempt = IsTaxExempt,
            TaxPolicyBasis = TaxPolicyBasis,
            Sort = Sort,
            Remark = Remark,
            Status = Status.Enable
        };

        public UpdateGoodsTypeDto ToUpdateDto(Guid id)
        {
            var dto = ToCreateDto();
            return new UpdateGoodsTypeDto
            {
                Id = id,
                Code = dto.Code,
                Name = dto.Name,
                ImageUrl = dto.ImageUrl,
                TaxCategoryCode = dto.TaxCategoryCode,
                TaxCategoryName = dto.TaxCategoryName,
                InvoiceGoodsShortName = dto.InvoiceGoodsShortName,
                DefaultTaxRate = dto.DefaultTaxRate,
                IsTaxExempt = dto.IsTaxExempt,
                TaxPolicyBasis = dto.TaxPolicyBasis,
                Sort = dto.Sort,
                Remark = dto.Remark,
                Status = dto.Status
            };
        }

        public bool Matches(Domain.Entities.Goods.GoodsType goodsType)
        {
            return goodsType.Name == Name
                   && goodsType.ImageUrl == ImageUrl
                   && goodsType.TaxCategoryCode == TaxCategoryCode
                   && goodsType.TaxCategoryName == TaxCategoryName
                   && goodsType.InvoiceGoodsShortName == InvoiceGoodsShortName
                   && goodsType.DefaultTaxRate == DefaultTaxRate
                   && goodsType.IsTaxExempt == IsTaxExempt
                   && goodsType.TaxPolicyBasis == TaxPolicyBasis
                   && goodsType.Sort == Sort
                   && goodsType.Remark == Remark
                   && goodsType.Status == Status.Enable
                   && goodsType.ParentId is null;
        }
    }

    private sealed record GoodsSeed(
        string Code,
        string UnitCode,
        string GoodsTypeCode,
        string SupplierCode,
        string WareCode,
        string Name,
        string Spec,
        string Brand,
        string Origin,
        string Description,
        decimal TaxRate,
        string UnitName,
        string UnitRemark,
        string Remark)
    {
        public CreateGoodsDto ToCreateDto(Guid goodsTypeId, Guid supplierId, Guid wareId) => new()
        {
            Code = Code,
            Name = Name,
            GoodsTypeId = goodsTypeId,
            DefaultSupplierId = supplierId,
            DefaultWareId = wareId,
            Spec = Spec,
            Brand = Brand,
            Origin = Origin,
            Description = Description,
            TaxRate = TaxRate,
            IsOnSale = true,
            SupplierIds = [supplierId],
            Remark = Remark,
            Status = Status.Enable
        };

        public UpdateGoodsDto ToUpdateDto(Guid id, Guid goodsTypeId, Guid supplierId, Guid wareId)
        {
            var dto = ToCreateDto(goodsTypeId, supplierId, wareId);
            return new UpdateGoodsDto
            {
                Id = id,
                Code = dto.Code,
                Name = dto.Name,
                GoodsTypeId = dto.GoodsTypeId,
                DefaultSupplierId = dto.DefaultSupplierId,
                DefaultWareId = dto.DefaultWareId,
                Spec = dto.Spec,
                Brand = dto.Brand,
                Origin = dto.Origin,
                Description = dto.Description,
                TaxRate = dto.TaxRate,
                IsOnSale = dto.IsOnSale,
                SupplierIds = dto.SupplierIds,
                Remark = dto.Remark,
                Status = dto.Status
            };
        }

        public CreateGoodsUnitDto ToCreateGoodsUnitDto(Guid goodsId) => new()
        {
            GoodsId = goodsId,
            Code = UnitCode,
            Name = UnitName,
            ConversionRate = 1m,
            IsBaseUnit = true,
            Sort = 1,
            Remark = UnitRemark,
            Status = Status.Enable
        };

        public UpdateGoodsUnitDto ToUpdateGoodsUnitDto(Guid id, Guid goodsId)
        {
            var dto = ToCreateGoodsUnitDto(goodsId);
            return new UpdateGoodsUnitDto
            {
                Id = id,
                GoodsId = dto.GoodsId,
                Code = dto.Code,
                Name = dto.Name,
                ConversionRate = dto.ConversionRate,
                IsBaseUnit = dto.IsBaseUnit,
                Sort = dto.Sort,
                Remark = dto.Remark,
                Status = dto.Status
            };
        }

        public bool Matches(Domain.Entities.Goods.Goods goods, Guid goodsTypeId, Guid supplierId, Guid wareId)
        {
            return goods.Name == Name
                   && goods.GoodsTypeId == goodsTypeId
                   && goods.DefaultSupplierId == supplierId
                   && goods.DefaultWareId == wareId
                   && goods.Spec == Spec
                   && goods.Brand == Brand
                   && goods.Origin == Origin
                   && goods.Description == Description
                   && goods.TaxRate == TaxRate
                   && goods.IsOnSale
                   && goods.Remark == Remark
                   && goods.Status == Status.Enable;
        }

        public bool Matches(Domain.Entities.Goods.GoodsUnit unit, Guid goodsId)
        {
            return unit.GoodsId == goodsId
                   && unit.Name == UnitName
                   && unit.ConversionRate == 1m
                   && unit.IsBaseUnit
                   && unit.Sort == 1
                   && unit.Remark == UnitRemark
                   && unit.Status == Status.Enable;
        }
    }

    private sealed record QuotationSeed(
        string Code,
        string CustomerCode,
        string GoodsCode,
        string GoodsUnitCode,
        string Name,
        string Description,
        DateTime EffectiveStart,
        DateTime EffectiveEnd,
        bool IsAudited,
        decimal UnitPrice,
        decimal MinOrderQuantity,
        string GoodsRemark)
    {
        public CreateQuotationDto ToCreateDto(Guid customerId) => new()
        {
            Code = Code,
            Name = Name,
            Description = Description,
            EffectiveStart = EffectiveStart,
            EffectiveEnd = EffectiveEnd,
            IsAudited = IsAudited,
            CustomerIds = [customerId],
            Status = Status.Enable
        };

        public UpdateQuotationDto ToUpdateDto(Guid id, Guid customerId)
        {
            var dto = ToCreateDto(customerId);
            return new UpdateQuotationDto
            {
                Id = id,
                Code = dto.Code,
                Name = dto.Name,
                Description = dto.Description,
                EffectiveStart = dto.EffectiveStart,
                EffectiveEnd = dto.EffectiveEnd,
                IsAudited = dto.IsAudited,
                CustomerIds = dto.CustomerIds,
                Status = dto.Status
            };
        }

        public CreateQuotationGoodsDto ToCreateGoodsDto(Guid quotationId, Guid goodsId, Guid goodsUnitId) => new()
        {
            QuotationId = quotationId,
            GoodsId = goodsId,
            GoodsUnitId = goodsUnitId,
            UnitPrice = UnitPrice,
            MinOrderQuantity = MinOrderQuantity,
            IsOnSale = true,
            Remark = GoodsRemark
        };

        public UpdateQuotationGoodsDto ToUpdateGoodsDto(Guid id, Guid quotationId, Guid goodsId, Guid goodsUnitId)
        {
            var dto = ToCreateGoodsDto(quotationId, goodsId, goodsUnitId);
            return new UpdateQuotationGoodsDto
            {
                Id = id,
                QuotationId = dto.QuotationId,
                GoodsId = dto.GoodsId,
                GoodsUnitId = dto.GoodsUnitId,
                UnitPrice = dto.UnitPrice,
                MinOrderQuantity = dto.MinOrderQuantity,
                IsOnSale = dto.IsOnSale,
                Remark = dto.Remark
            };
        }

        public bool Matches(Domain.Entities.Pricing.Quotation quotation)
        {
            return quotation.Name == Name
                   && quotation.Description == Description
                   && quotation.EffectiveStart == EffectiveStart
                   && quotation.EffectiveEnd == EffectiveEnd
                   && quotation.IsAudited == IsAudited;
        }

        public bool Matches(Domain.Entities.Pricing.QuotationGoods quotationGoods)
        {
            return quotationGoods.UnitPrice == UnitPrice
                   && quotationGoods.MinOrderQuantity == MinOrderQuantity
                   && quotationGoods.IsOnSale
                   && quotationGoods.Remark == GoodsRemark;
        }
    }

    private sealed record CustomerProtocolSeed(
        string Code,
        string QuotationCode,
        string CustomerCode,
        string GoodsCode,
        string GoodsUnitCode,
        string Name,
        DateTime EffectiveStart,
        DateTime EffectiveEnd,
        decimal ProtocolPrice,
        decimal MinOrderQuantity,
        string GoodsRemark,
        string Remark)
    {
        public CreateCustomerProtocolDto ToCreateDto(Guid quotationId, Guid customerId) => new()
        {
            Code = Code,
            Name = Name,
            QuotationId = quotationId,
            EffectiveStart = EffectiveStart,
            EffectiveEnd = EffectiveEnd,
            CustomerIds = [customerId],
            Remark = Remark,
            Status = Status.Enable
        };

        public UpdateCustomerProtocolDto ToUpdateDto(Guid id, Guid quotationId, Guid customerId)
        {
            var dto = ToCreateDto(quotationId, customerId);
            return new UpdateCustomerProtocolDto
            {
                Id = id,
                Code = dto.Code,
                Name = dto.Name,
                QuotationId = dto.QuotationId,
                EffectiveStart = dto.EffectiveStart,
                EffectiveEnd = dto.EffectiveEnd,
                CustomerIds = dto.CustomerIds,
                Remark = dto.Remark,
                Status = dto.Status
            };
        }

        public CreateCustomerProtocolGoodsDto ToCreateGoodsDto(Guid customerProtocolId, Guid goodsId, Guid goodsUnitId) => new()
        {
            CustomerProtocolId = customerProtocolId,
            GoodsId = goodsId,
            GoodsUnitId = goodsUnitId,
            ProtocolPrice = ProtocolPrice,
            MinOrderQuantity = MinOrderQuantity,
            Remark = GoodsRemark
        };

        public UpdateCustomerProtocolGoodsDto ToUpdateGoodsDto(
            Guid id,
            Guid customerProtocolId,
            Guid goodsId,
            Guid goodsUnitId)
        {
            var dto = ToCreateGoodsDto(customerProtocolId, goodsId, goodsUnitId);
            return new UpdateCustomerProtocolGoodsDto
            {
                Id = id,
                CustomerProtocolId = dto.CustomerProtocolId,
                GoodsId = dto.GoodsId,
                GoodsUnitId = dto.GoodsUnitId,
                ProtocolPrice = dto.ProtocolPrice,
                MinOrderQuantity = dto.MinOrderQuantity,
                Remark = dto.Remark
            };
        }

        public bool Matches(Domain.Entities.Pricing.CustomerProtocol customerProtocol, Guid quotationId, Guid customerId)
        {
            return customerProtocol.Name == Name
                   && customerProtocol.QuotationId == quotationId
                   && customerProtocol.EffectiveStart == EffectiveStart
                   && customerProtocol.EffectiveEnd == EffectiveEnd
                   && customerProtocol.Remark == Remark
                   && customerProtocol.Status == Status.Enable
                   && customerProtocol.Customers.Count == 1
                   && customerProtocol.Customers.Single().CustomerId == customerId;
        }

        public bool Matches(Domain.Entities.Pricing.CustomerProtocolGoods customerProtocolGoods)
        {
            return customerProtocolGoods.ProtocolPrice == ProtocolPrice
                   && customerProtocolGoods.MinOrderQuantity == MinOrderQuantity
                   && customerProtocolGoods.Remark == GoodsRemark;
        }
    }

    private sealed record DepartmentSeed(
        string Code,
        string? ParentCode,
        string Name,
        string Phone,
        string Email,
        int Sort,
        string Remark)
    {
        public CreateDepartmentDto ToCreateDto(Guid? parentId, DemoAuditUser auditUser) => new()
        {
            Code = Code,
            ParentId = parentId,
            Name = Name,
            LeaderId = auditUser.Id,
            LeaderName = auditUser.Username,
            Phone = Phone,
            Email = Email,
            Sort = Sort,
            Remark = Remark,
            Status = Status.Enable
        };

        public UpdateDepartmentDto ToUpdateDto(Guid id, Guid? parentId, DemoAuditUser auditUser) => new()
        {
            Id = id,
            Code = Code,
            ParentId = parentId,
            Name = Name,
            LeaderId = auditUser.Id,
            LeaderName = auditUser.Username,
            Phone = Phone,
            Email = Email,
            Sort = Sort,
            Remark = Remark,
            Status = Status.Enable
        };

        public bool Matches(Domain.Entities.Department department, Guid? parentId, DemoAuditUser auditUser)
        {
            return department.ParentId == parentId
                   && department.Name == Name
                   && department.LeaderId == auditUser.Id
                   && department.LeaderName == auditUser.Username
                   && department.Phone == Phone
                   && department.Email == Email
                   && department.Sort == Sort
                   && department.Remark == Remark
                   && department.Status == Status.Enable;
        }
    }

    private sealed record SystemRoleSeed(string Code, string Name, string Description)
    {
        public CreateRoleDto ToCreateDto() => new()
        {
            Code = Code,
            Name = Name,
            Desc = Description,
            Status = Status.Enable
        };

        public UpdateRoleDto ToUpdateDto(Guid id) => new()
        {
            Id = id,
            Code = Code,
            Name = Name,
            Desc = Description,
            Status = Status.Enable
        };

        public bool Matches(Domain.Entities.Role role)
        {
            return role.Name == Name
                   && role.Desc == Description
                   && role.Status == Status.Enable;
        }
    }

    private sealed record SystemUserSeed(
        int Sequence,
        string Username,
        string RoleCode,
        string NickName,
        GenderType Gender,
        string Phone,
        string Email)
    {
        private string Password => $"SkyRocSystem{Sequence:D2}!";

        public CreateUserDto ToCreateDto() => new()
        {
            Username = Username,
            NickName = NickName,
            Gender = Gender,
            Phone = Phone,
            Email = Email,
            Password = Password,
            Status = Status.Enable
        };

        public UpdateUserDto ToUpdateDto(Guid id) => new()
        {
            Id = id,
            Username = Username,
            NickName = NickName,
            Gender = Gender,
            Phone = Phone,
            Email = Email,
            Status = Status.Enable
        };

        public bool Matches(Domain.Entities.User user)
        {
            return user.NickName == NickName
                   && user.Gender == Gender
                   && user.Phone == Phone
                   && user.Email == Email
                   && !string.IsNullOrWhiteSpace(user.PasswordHash)
                   && user.Status == Status.Enable;
        }
    }

    private sealed record WareSeed(
        string Code,
        string Name,
        string ContactName,
        string ContactPhone,
        string Address,
        int Sort,
        string Remark)
    {
        public CreateWareDto ToCreateDto() => new()
        {
            Code = Code,
            Name = Name,
            ContactName = ContactName,
            ContactPhone = ContactPhone,
            Address = Address,
            Sort = Sort,
            Remark = Remark,
            Status = Status.Enable
        };

        public UpdateWareDto ToUpdateDto(Guid id) => new()
        {
            Id = id,
            Code = Code,
            Name = Name,
            ContactName = ContactName,
            ContactPhone = ContactPhone,
            Address = Address,
            Sort = Sort,
            Remark = Remark,
            Status = Status.Enable
        };

        public bool Matches(Domain.Entities.Storage.Ware ware)
        {
            return ware.Name == Name
                   && ware.ContactName == ContactName
                   && ware.ContactPhone == ContactPhone
                   && ware.Address == Address
                   && ware.Sort == Sort
                   && ware.Remark == Remark
                   && ware.Status == Status.Enable;
        }
    }

    private sealed record SupplierSeed(
        string Code,
        string Name,
        string ContactName,
        string ContactPhone,
        string Address,
        string BankName,
        string BankAccount,
        string TaxNo,
        string Remark)
    {
        public CreateSupplierDto ToCreateDto() => new()
        {
            Code = Code,
            Name = Name,
            ContactName = ContactName,
            ContactPhone = ContactPhone,
            Address = Address,
            BankName = BankName,
            BankAccount = BankAccount,
            TaxNo = TaxNo,
            Remark = Remark,
            Status = Status.Enable
        };

        public UpdateSupplierDto ToUpdateDto(Guid id) => new()
        {
            Id = id,
            Code = Code,
            Name = Name,
            ContactName = ContactName,
            ContactPhone = ContactPhone,
            Address = Address,
            BankName = BankName,
            BankAccount = BankAccount,
            TaxNo = TaxNo,
            Remark = Remark,
            Status = Status.Enable
        };

        public bool Matches(Domain.Entities.Purchases.Supplier supplier)
        {
            return supplier.Name == Name
                   && supplier.ContactName == ContactName
                   && supplier.ContactPhone == ContactPhone
                   && supplier.Address == Address
                   && supplier.BankName == BankName
                   && supplier.BankAccount == BankAccount
                   && supplier.TaxNo == TaxNo
                   && supplier.Remark == Remark
                   && supplier.Status == Status.Enable;
        }
    }

    private sealed record PurchaseRuleSeed(
        string Code,
        string SupplierCode,
        string PurchaserCode,
        string WareCode,
        string GoodsTypeCode,
        string GoodsCode,
        string CustomerCode,
        string Name,
        int PurchasePattern,
        string Remark)
    {
        public CreatePurchaseRuleDto ToCreateDto(
            Guid supplierId,
            Guid purchaserId,
            Guid wareId,
            Guid goodsTypeId,
            Guid goodsId,
            Guid customerId) => new()
            {
                Code = Code,
                Name = Name,
                SupplierId = supplierId,
                PurchaserId = purchaserId,
                WareId = wareId,
                GoodsTypeId = goodsTypeId,
                PurchasePattern = PurchasePattern,
                GoodsIds = [goodsId],
                CustomerIds = [customerId],
                Remark = Remark,
                Status = Status.Enable
            };

        public UpdatePurchaseRuleDto ToUpdateDto(
            Guid id,
            Guid supplierId,
            Guid purchaserId,
            Guid wareId,
            Guid goodsTypeId,
            Guid goodsId,
            Guid customerId) => new()
            {
                Id = id,
                Code = Code,
                Name = Name,
                SupplierId = supplierId,
                PurchaserId = purchaserId,
                WareId = wareId,
                GoodsTypeId = goodsTypeId,
                PurchasePattern = PurchasePattern,
                GoodsIds = [goodsId],
                CustomerIds = [customerId],
                Remark = Remark,
                Status = Status.Enable
            };

        public bool Matches(
            Domain.Entities.Purchases.PurchaseRule purchaseRule,
            Guid supplierId,
            Guid purchaserId,
            Guid wareId,
            Guid goodsTypeId,
            Guid goodsId,
            Guid customerId)
        {
            return purchaseRule.Name == Name
                   && purchaseRule.SupplierId == supplierId
                   && purchaseRule.PurchaserId == purchaserId
                   && purchaseRule.WareId == wareId
                   && purchaseRule.GoodsTypeId == goodsTypeId
                   && purchaseRule.PurchasePattern == PurchasePattern
                   && purchaseRule.Remark == Remark
                   && purchaseRule.Status == Status.Enable
                   && purchaseRule.Goods.Count == 1
                   && purchaseRule.Goods.Single().GoodsId == goodsId
                   && purchaseRule.Customers.Count == 1
                   && purchaseRule.Customers.Single().CustomerId == customerId;
        }
    }

    private sealed record SaleOrderSeed(
        int Sequence,
        string StableKey,
        string CustomerCode,
        string QuotationCode,
        string WareCode,
        DateTime OrderDate,
        DateTime ReceiveDate,
        string ContactName,
        string ContactPhone,
        string DeliveryAddress,
        string Remark,
        SaleOrderStatus TargetStatus,
        string AuditRemark,
        IReadOnlyList<SaleOrderDetailSeed> Details)
    {
        public CreateSaleOrderDto ToCreateDto(
            Guid customerId,
            Guid quotationId,
            Guid wareId,
            IReadOnlyDictionary<string, Domain.Entities.Goods.Goods> goods,
            IReadOnlyDictionary<string, Domain.Entities.Goods.GoodsUnit> goodsUnits) => new()
            {
                CustomerId = customerId,
                QuotationId = quotationId,
                WareId = wareId,
                OrderDate = OrderDate,
                ReceiveDate = ReceiveDate,
                ContactName = ContactName,
                ContactPhone = ContactPhone,
                DeliveryAddress = DeliveryAddress,
                Remark = Remark,
                InnerRemark = StableKey,
                Details = Details.Select(detail => detail.ToCreateDto(goods, goodsUnits)).ToList()
            };

        public bool Matches(SaleOrder order, Guid customerId, Guid quotationId, Guid wareId)
        {
            return order.CustomerId == customerId
                   && order.QuotationId == quotationId
                   && order.WareId == wareId
                   && order.OrderDate == OrderDate
                   && order.ReceiveDate == ReceiveDate
                   && order.ContactNameSnapshot == ContactName
                   && order.ContactPhoneSnapshot == ContactPhone
                   && order.DeliveryAddressSnapshot == DeliveryAddress
                   && order.Remark == Remark
                   && order.InnerRemark == StableKey;
        }
    }

    private sealed record SaleOrderDetailSeed(
        string GoodsCode,
        string GoodsUnitCode,
        decimal Quantity,
        decimal FixedPrice,
        string Remark,
        string InnerRemark)
    {
        public CreateSaleOrderDetailDto ToCreateDto(
            IReadOnlyDictionary<string, Domain.Entities.Goods.Goods> goods,
            IReadOnlyDictionary<string, Domain.Entities.Goods.GoodsUnit> goodsUnits)
        {
            var goodsEntity = GetManagedReference(goods, GoodsCode, "商品");
            var unit = GetManagedReference(goodsUnits, GoodsUnitCode, "商品单位");
            return new CreateSaleOrderDetailDto
            {
                GoodsId = goodsEntity.Id,
                GoodsUnitId = unit.Id,
                FixedGoodsUnitId = unit.Id,
                Quantity = Quantity,
                FixedPrice = FixedPrice,
                Remark = Remark,
                InnerRemark = InnerRemark
            };
        }
    }

    private sealed record PurchaserSeed(
        int Sequence,
        string Code,
        string Name,
        string Phone,
        string Remark)
    {
        public CreatePurchaserDto ToCreateDto(Guid userId, Guid departmentId) => new()
        {
            Code = Code,
            Name = Name,
            Phone = Phone,
            UserId = userId,
            DepartmentId = departmentId,
            Remark = Remark,
            Status = Status.Enable
        };

        public UpdatePurchaserDto ToUpdateDto(Guid id, Guid userId, Guid departmentId) => new()
        {
            Id = id,
            Code = Code,
            Name = Name,
            Phone = Phone,
            UserId = userId,
            DepartmentId = departmentId,
            Remark = Remark,
            Status = Status.Enable
        };

        public bool Matches(Domain.Entities.Purchases.Purchaser purchaser, Guid userId, Guid departmentId)
        {
            return purchaser.Name == Name
                   && purchaser.Phone == Phone
                   && purchaser.UserId == userId
                   && purchaser.DepartmentId == departmentId
                   && purchaser.Remark == Remark
                   && purchaser.Status == Status.Enable;
        }
    }

    private sealed record CarrierSeed(
        string Code,
        string Name,
        string ContactName,
        string ContactPhone,
        string Address,
        string Remark)
    {
        public CreateCarrierDto ToCreateDto() => new()
        {
            Code = Code,
            Name = Name,
            ContactName = ContactName,
            ContactPhone = ContactPhone,
            Address = Address,
            Remark = Remark,
            Status = Status.Enable
        };

        public UpdateCarrierDto ToUpdateDto(Guid id) => new()
        {
            Id = id,
            Code = Code,
            Name = Name,
            ContactName = ContactName,
            ContactPhone = ContactPhone,
            Address = Address,
            Remark = Remark,
            Status = Status.Enable
        };

        public bool Matches(Domain.Entities.Delivery.Carrier carrier)
        {
            return carrier.Name == Name
                   && carrier.ContactName == ContactName
                   && carrier.ContactPhone == ContactPhone
                   && carrier.Address == Address
                   && carrier.Remark == Remark
                   && carrier.Status == Status.Enable;
        }
    }

    private sealed record DriverSeed(
        string Code,
        string CarrierCode,
        string Name,
        string Phone,
        string PlateNumber,
        string LicenseNo,
        string Remark)
    {
        public CreateDriverDto ToCreateDto(Guid carrierId) => new()
        {
            Code = Code,
            Name = Name,
            Phone = Phone,
            CarrierId = carrierId,
            PlateNumber = PlateNumber,
            LicenseNo = LicenseNo,
            Remark = Remark,
            Status = Status.Enable
        };

        public UpdateDriverDto ToUpdateDto(Guid id, Guid carrierId) => new()
        {
            Id = id,
            Code = Code,
            Name = Name,
            Phone = Phone,
            CarrierId = carrierId,
            PlateNumber = PlateNumber,
            LicenseNo = LicenseNo,
            Remark = Remark,
            Status = Status.Enable
        };

        public bool Matches(Domain.Entities.Delivery.Driver driver, Guid carrierId)
        {
            return driver.Name == Name
                   && driver.CarrierId == carrierId
                   && driver.Phone == Phone
                   && driver.PlateNumber == PlateNumber
                   && driver.LicenseNo == LicenseNo
                   && driver.Remark == Remark
                   && driver.Status == Status.Enable;
        }
    }

    private sealed record DeliveryRouteSeed(
        string Code,
        string CustomerCode,
        string Name,
        string Description,
        int Sort,
        string Remark)
    {
        public CreateDeliveryRouteDto ToCreateDto(Guid customerId) => new()
        {
            Code = Code,
            Name = Name,
            Description = Description,
            Sort = Sort,
            CustomerIds = [customerId],
            Remark = Remark,
            Status = Status.Enable
        };

        public UpdateDeliveryRouteDto ToUpdateDto(Guid id, Guid customerId) => new()
        {
            Id = id,
            Code = Code,
            Name = Name,
            Description = Description,
            Sort = Sort,
            CustomerIds = [customerId],
            Remark = Remark,
            Status = Status.Enable
        };

        public bool Matches(Domain.Entities.Delivery.DeliveryRoute route, Guid customerId)
        {
            return route.Name == Name
                   && route.Description == Description
                   && route.Sort == Sort
                   && route.Remark == Remark
                   && route.Status == Status.Enable
                   && route.CustomerRoutes.Count == 1
                   && route.CustomerRoutes.Single().CustomerId == customerId;
        }
    }

    private sealed record ServicePeriodSeed(
        string Name,
        TimeOnly StartTime,
        TimeOnly EndTime,
        int SortOrder,
        bool IsEnabled)
    {
        public UpsertServicePeriodDto ToUpsertDto() => new()
        {
            Name = Name,
            StartTime = StartTime,
            EndTime = EndTime,
            SortOrder = SortOrder,
            IsEnabled = IsEnabled
        };

        public bool Matches(ServicePeriod servicePeriod)
        {
            return servicePeriod.StartTime == StartTime
                   && servicePeriod.EndTime == EndTime
                   && servicePeriod.SortOrder == SortOrder
                   && servicePeriod.Status == (IsEnabled ? Status.Enable : Status.Disable);
        }
    }

    private sealed record NoticeSeed(
        string Title,
        string Content,
        NoticeStatus NoticeStatus,
        DateTime PublishedTime)
    {
        public UpsertNoticeDto ToUpsertDto() => new()
        {
            Title = Title,
            Content = Content
        };

        public bool Matches(Notice notice)
        {
            return notice.Content == Content;
        }
    }

    private sealed record PrintTemplateSeed(
        string TemplateCode,
        string Name,
        PrintBusinessType BusinessType,
        string DesignJson,
        bool IsEnabled,
        IReadOnlyList<PrintTemplateFieldSeed> Fields)
    {
        public CreatePrintTemplateDto ToCreateDto() => new()
        {
            TemplateCode = TemplateCode,
            Name = Name,
            BusinessType = BusinessType,
            DesignJson = DesignJson,
            IsEnabled = IsEnabled,
            Fields = Fields.Select(field => field.ToInputDto()).ToList()
        };

        public UpdatePrintTemplateDto ToUpdateDto(Guid id) => new()
        {
            Id = id,
            TemplateCode = TemplateCode,
            Name = Name,
            BusinessType = BusinessType,
            DesignJson = DesignJson,
            IsEnabled = IsEnabled,
            Fields = Fields.Select(field => field.ToInputDto()).ToList()
        };

        public bool Matches(PrintTemplate template)
        {
            return template.Name == Name
                   && template.BusinessType == BusinessType
                   && template.DesignJson == DesignJson
                   && template.IsEnabled == IsEnabled
                   && template.Fields.Count == Fields.Count
                   && template.Fields
                       .OrderBy(field => field.DisplayOrder)
                       .Zip(Fields.OrderBy(field => field.DisplayOrder))
                       .All(pair => pair.First.FieldKey == pair.Second.FieldKey
                                    && pair.First.DisplayName == pair.Second.DisplayName
                                    && pair.First.DisplayOrder == pair.Second.DisplayOrder
                                    && pair.First.Format == pair.Second.Format);
        }
    }

    private sealed record PrintTemplateFieldSeed(
        string FieldKey,
        string DisplayName,
        int DisplayOrder,
        string? Format)
    {
        public PrintTemplateFieldInputDto ToInputDto() => new()
        {
            FieldKey = FieldKey,
            DisplayName = DisplayName,
            DisplayOrder = DisplayOrder,
            Format = Format
        };

        public PrintTemplateField ToEntity(Guid printTemplateId, DemoAuditUser auditUser) => new()
        {
            Id = Guid.NewGuid(),
            PrintTemplateId = printTemplateId,
            FieldKey = FieldKey,
            DisplayName = DisplayName,
            DisplayOrder = DisplayOrder,
            Format = Format,
            CreateBy = auditUser.Id,
            CreateName = auditUser.Username,
            Status = Status.Enable
        };
    }

    private sealed record OperationLogSeed(
        int Sequence,
        string Module,
        string OperationType,
        string Description,
        string Method,
        string Url,
        string RequestParams,
        string ResponseResult,
        string IpAddress,
        string Location,
        string Browser,
        string Os,
        long ExecutionDuration,
        bool IsSuccess,
        string? ErrorMessage)
    {
        public OperationLog ToEntity(DemoOrganizationalUser user, DemoAuditUser auditUser)
        {
            var entity = new OperationLog { Id = Guid.NewGuid() };
            Apply(entity, user, auditUser);
            return entity;
        }

        public void Apply(OperationLog operationLog, DemoOrganizationalUser user, DemoAuditUser auditUser)
        {
            operationLog.Module = Module;
            operationLog.OperationType = OperationType;
            operationLog.Desc = Description;
            operationLog.Method = Method;
            operationLog.Url = Url;
            operationLog.RequestParams = RequestParams;
            operationLog.ResponseResult = ResponseResult;
            operationLog.IpAddress = IpAddress;
            operationLog.Location = Location;
            operationLog.Browser = Browser;
            operationLog.Os = Os;
            operationLog.ExecutionDuration = ExecutionDuration;
            operationLog.IsSuccess = IsSuccess;
            operationLog.ErrorMessage = ErrorMessage;
            operationLog.Status = Status.Enable;
            operationLog.CreateTime = new DateTime(2026, 7, Sequence % 28 + 1, 10, Sequence % 60, 0, DateTimeKind.Utc);
            operationLog.CreateBy = user.Id;
            operationLog.CreateName = user.Username;
            operationLog.UpdateTime = new DateTime(2026, 7, Sequence % 28 + 1, 11, Sequence % 60, 0, DateTimeKind.Utc);
            operationLog.UpdateBy = auditUser.Id;
            operationLog.UpdateName = auditUser.Username;
        }
    }

    private sealed record LoginLogSeed(
        int Sequence,
        string Username,
        bool IsSuccess,
        string? FailureReason,
        string IpAddress,
        string UserAgent,
        DateTime LoginTime)
    {
        public LoginLog ToEntity(DemoOrganizationalUser user, DemoAuditUser auditUser)
        {
            var entity = new LoginLog { Id = Guid.NewGuid() };
            Apply(entity, user, auditUser);
            return entity;
        }

        public void Apply(LoginLog loginLog, DemoOrganizationalUser user, DemoAuditUser auditUser)
        {
            loginLog.Username = Username;
            loginLog.UserId = IsSuccess ? user.Id : null;
            loginLog.IsSuccess = IsSuccess;
            loginLog.FailureReason = FailureReason;
            loginLog.IpAddress = IpAddress;
            loginLog.UserAgent = UserAgent;
            loginLog.LoginTime = LoginTime;
            loginLog.Status = Status.Enable;
            loginLog.CreateTime = LoginTime;
            loginLog.CreateBy = auditUser.Id;
            loginLog.CreateName = auditUser.Username;
            loginLog.UpdateTime = LoginTime.AddMinutes(1);
            loginLog.UpdateBy = auditUser.Id;
            loginLog.UpdateName = auditUser.Username;
        }
    }

    private sealed record DemoAuditUser(Guid Id, string Username);

    private sealed record DemoOrganizationalUser(Guid Id, string Username);

    private sealed record DemoOrganizationalDepartment(Guid Id, string Code);
}
