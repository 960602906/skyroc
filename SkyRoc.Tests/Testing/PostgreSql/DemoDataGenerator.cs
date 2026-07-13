using Application.DTOs.Customers;
using Application.DTOs.Goods;
using Application.DTOs.Purchases;
using Application.DTOs.Pricing;
using Application.DTOs.Storage;
using Application.interfaces;
using Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using Shared.Constants;

namespace SkyRoc.Tests.Testing.PostgreSql;

/// <summary>
///     按完整稳定业务键补齐长期前端联调数据；当前先提供公司基础资料层，后续层在同一安全边界内追加。
/// </summary>
public sealed class DemoDataGenerator(PostgreSqlTestFixture fixture)
{
    private const string CompaniesLayer = "companies";
    private const string CustomerTagsLayer = "customer-tags";
    private const string CustomerProtocolGoodsLayer = "customer-protocol-goods";
    private const string CustomerProtocolsLayer = "customer-protocols";
    private const string CustomersLayer = "customers";
    private const string GoodsLayer = "goods";
    private const string GoodsUnitsLayer = "goods-units";
    private const string GoodsTypesLayer = "goods-types";
    private const string PurchasersLayer = "purchasers";
    private const string QuotationGoodsLayer = "quotation-goods";
    private const string QuotationsLayer = "quotations";
    private const string SuppliersLayer = "suppliers";
    private const string WaresLayer = "wares";

    /// <summary>
    ///     在经白名单验证的真实 PostgreSQL 中幂等生成当前已实现的联调资料层。
    /// </summary>
    /// <param name="cancellationToken">取消生成的令牌。</param>
    /// <returns>按资料层汇总的新增与复用数量。</returns>
    public async Task<DemoDataGenerationResult> GenerateAsync(CancellationToken cancellationToken = default)
    {
        DatabaseSafetyGuard.Validate(fixture.Settings);

        using var factory = fixture.CreateWebApplicationFactory();
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var companyService = scope.ServiceProvider.GetRequiredService<ICompanyService>();
        var customerTagService = scope.ServiceProvider.GetRequiredService<ICustomerTagService>();
        var customerService = scope.ServiceProvider.GetRequiredService<ICustomerService>();
        var customerProtocolGoodsService = scope.ServiceProvider.GetRequiredService<ICustomerProtocolGoodsService>();
        var customerProtocolService = scope.ServiceProvider.GetRequiredService<ICustomerProtocolService>();
        var goodsService = scope.ServiceProvider.GetRequiredService<IGoodsService>();
        var goodsUnitService = scope.ServiceProvider.GetRequiredService<IGoodsUnitService>();
        var goodsTypeService = scope.ServiceProvider.GetRequiredService<IGoodsTypeService>();
        var purchaserService = scope.ServiceProvider.GetRequiredService<IPurchaserService>();
        var quotationGoodsService = scope.ServiceProvider.GetRequiredService<IQuotationGoodsService>();
        var quotationService = scope.ServiceProvider.GetRequiredService<IQuotationService>();
        var supplierService = scope.ServiceProvider.GetRequiredService<ISupplierService>();
        var wareService = scope.ServiceProvider.GetRequiredService<IWareService>();
        var companySeeds = CreateCompanySeeds();
        var customerTagSeeds = CreateCustomerTagSeeds();
        var customerSeeds = CreateCustomerSeeds();
        var customerProtocolSeeds = CreateCustomerProtocolSeeds();
        var goodsSeeds = CreateGoodsSeeds();
        var goodsTypeSeeds = CreateGoodsTypeSeeds();
        var purchaserSeeds = CreatePurchaserSeeds();
        var quotationSeeds = CreateQuotationSeeds();
        var supplierSeeds = CreateSupplierSeeds();
        var wareSeeds = CreateWareSeeds();
        var companyCodes = companySeeds.Select(seed => seed.Code).ToArray();
        var customerTagCodes = customerTagSeeds.Select(seed => seed.Code).ToArray();
        var customerCodes = customerSeeds.Select(seed => seed.Code).ToArray();
        var customerProtocolCodes = customerProtocolSeeds.Select(seed => seed.Code).ToArray();
        var goodsCodes = goodsSeeds.Select(seed => seed.Code).ToArray();
        var goodsUnitCodes = goodsSeeds.Select(seed => seed.UnitCode).ToArray();
        var goodsTypeCodes = goodsTypeSeeds.Select(seed => seed.Code).ToArray();
        var purchaserCodes = purchaserSeeds.Select(seed => seed.Code).ToArray();
        var quotationCodes = quotationSeeds.Select(seed => seed.Code).ToArray();
        var supplierCodes = supplierSeeds.Select(seed => seed.Code).ToArray();
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
        var existingCustomerTags = await context.CustomerTags
            .Where(tag => customerTagCodes.Contains(tag.Code))
            .ToDictionaryAsync(tag => tag.Code, StringComparer.Ordinal, cancellationToken);
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
        var existingPurchasers = await context.Purchasers
            .Where(purchaser => purchaserCodes.Contains(purchaser.Code))
            .ToDictionaryAsync(purchaser => purchaser.Code, StringComparer.Ordinal, cancellationToken);
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
        var organizationalDepartments = await context.Departments
            .OrderBy(department => department.Code)
            .Select(department => new DemoOrganizationalDepartment(department.Id, department.Code))
            .ToListAsync(cancellationToken);

        if (organizationalUsers.Count == 0 || organizationalDepartments.Count == 0)
        {
            throw new InvalidOperationException("长期联调采购员生成需要至少一条已存在的系统用户和部门资料。");
        }

        var createdCompanies = 0;
        var reusedCompanies = 0;
        var createdCustomerTags = 0;
        var reusedCustomerTags = 0;
        var createdCustomers = 0;
        var reusedCustomers = 0;
        var createdCustomerProtocolGoods = 0;
        var reusedCustomerProtocolGoods = 0;
        var createdCustomerProtocols = 0;
        var reusedCustomerProtocols = 0;
        var createdGoods = 0;
        var reusedGoods = 0;
        var createdGoodsUnits = 0;
        var reusedGoodsUnits = 0;
        var createdGoodsTypes = 0;
        var reusedGoodsTypes = 0;
        var createdPurchasers = 0;
        var reusedPurchasers = 0;
        var createdQuotationGoods = 0;
        var reusedQuotationGoods = 0;
        var createdQuotations = 0;
        var reusedQuotations = 0;
        var createdSuppliers = 0;
        var reusedSuppliers = 0;
        var createdWares = 0;
        var reusedWares = 0;
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
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

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return new DemoDataGenerationResult(
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                [CompaniesLayer] = createdCompanies,
                [CustomerTagsLayer] = createdCustomerTags,
                [CustomerProtocolGoodsLayer] = createdCustomerProtocolGoods,
                [CustomerProtocolsLayer] = createdCustomerProtocols,
                [CustomersLayer] = createdCustomers,
                [GoodsLayer] = createdGoods,
                [GoodsUnitsLayer] = createdGoodsUnits,
                [GoodsTypesLayer] = createdGoodsTypes,
                [PurchasersLayer] = createdPurchasers,
                [QuotationGoodsLayer] = createdQuotationGoods,
                [QuotationsLayer] = createdQuotations,
                [SuppliersLayer] = createdSuppliers,
                [WaresLayer] = createdWares
            },
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                [CompaniesLayer] = reusedCompanies,
                [CustomerTagsLayer] = reusedCustomerTags,
                [CustomerProtocolGoodsLayer] = reusedCustomerProtocolGoods,
                [CustomerProtocolsLayer] = reusedCustomerProtocols,
                [CustomersLayer] = reusedCustomers,
                [GoodsLayer] = reusedGoods,
                [GoodsUnitsLayer] = reusedGoodsUnits,
                [GoodsTypesLayer] = reusedGoodsTypes,
                [PurchasersLayer] = reusedPurchasers,
                [QuotationGoodsLayer] = reusedQuotationGoods,
                [QuotationsLayer] = reusedQuotations,
                [SuppliersLayer] = reusedSuppliers,
                [WaresLayer] = reusedWares
            });
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
                Domain.Entities.Customers.CustomerTag customerTag => customerTag.Id,
                Domain.Entities.Goods.GoodsType goodsType => goodsType.Id,
                Domain.Entities.Purchases.Supplier supplier => supplier.Id,
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

    private sealed record DemoAuditUser(Guid Id, string Username);

    private sealed record DemoOrganizationalUser(Guid Id, string Username);

    private sealed record DemoOrganizationalDepartment(Guid Id, string Code);
}
