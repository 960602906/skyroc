using Microsoft.EntityFrameworkCore;
using Domain.Entities.Delivery;
using Domain.Entities.Orders;
using Domain.Entities.Printing;
using Domain.Entities.Purchases;
using Domain.Entities.Finance;
using Domain.Entities.Storage;
using Domain.Entities.System;
using Shared.Constants;
using SkyRoc.Tests.Testing.PostgreSql;
using Xunit;

namespace SkyRoc.Tests.PostgreSql;

/// <summary>
///     验证长期联调数据生成器在真实 PostgreSQL 中仅管理完整稳定键对应的数据。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class DemoDataGeneratorTests(PostgreSqlTestFixture fixture)
{
    /// <summary>
    ///     首次生成必须补齐公司层，重复执行不得新增重复稳定业务键，并且可写资料字段均有业务含义。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_CreatesCompleteManagedCompanies_AndSecondRunIsIdempotent()
    {
        var first = await fixture.GenerateDemoDataAsync();
        var second = await fixture.GenerateDemoDataAsync();

        var managedCompanyCodes = Enumerable.Range(1, 30)
            .Select(sequence => DemoDataStableKeyCatalog.Create("COMPANY", sequence))
            .ToArray();
        await using var context = fixture.CreateDbContext();
        var companies = await context.Companies
            .Where(company => managedCompanyCodes.Contains(company.Code))
            .OrderBy(company => company.Code)
            .ToListAsync();

        Assert.Equal(30, companies.Count);
        Assert.Equal(30, companies.Select(company => company.Code).Distinct().Count());
        Assert.Equal(30, first.CreatedByLayer["companies"] + first.ReusedByLayer["companies"]);
        Assert.Equal(0, second.CreatedByLayer["companies"]);
        Assert.All(companies, company =>
        {
            Assert.False(string.IsNullOrWhiteSpace(company.Name));
            Assert.False(string.IsNullOrWhiteSpace(company.ContactName));
            Assert.False(string.IsNullOrWhiteSpace(company.ContactPhone));
            Assert.False(string.IsNullOrWhiteSpace(company.Address));
            Assert.False(string.IsNullOrWhiteSpace(company.Remark));
            Assert.NotNull(company.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(company.CreateName));
        });
    }

    /// <summary>
    ///     生成器必须为客户分群补齐稳定编码的树形标签，并在第二次运行时复用既有记录而不是重复插入。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_CreatesManagedCustomerTags_AndSecondRunIsIdempotent()
    {
        var first = await fixture.GenerateDemoDataAsync();
        var second = await fixture.GenerateDemoDataAsync();

        var managedTagCodes = Enumerable.Range(1, 30)
            .Select(sequence => DemoDataStableKeyCatalog.Create("CUSTOMER-TAG", sequence))
            .ToArray();
        await using var context = fixture.CreateDbContext();
        var tags = await context.CustomerTags
            .Where(tag => managedTagCodes.Contains(tag.Code))
            .OrderBy(tag => tag.Code)
            .ToListAsync();

        Assert.Equal(30, tags.Count);
        Assert.Equal(30, tags.Select(tag => tag.Code).Distinct().Count());
        Assert.Equal(30, first.CreatedByLayer["customer-tags"] + first.ReusedByLayer["customer-tags"]);
        Assert.Equal(0, second.CreatedByLayer["customer-tags"]);
        Assert.All(tags, tag =>
        {
            Assert.False(string.IsNullOrWhiteSpace(tag.Name));
            Assert.False(string.IsNullOrWhiteSpace(tag.Remark));
            Assert.NotNull(tag.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(tag.CreateName));
        });
    }

    /// <summary>
    ///     生成器必须基于受管公司和标签补齐客户资料，填满当前阶段适用的工商、开票与联系字段，并在重复运行时不重复创建客户或标签关系。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_CreatesManagedCustomersWithBusinessReferences_AndSecondRunIsIdempotent()
    {
        var first = await fixture.GenerateDemoDataAsync();
        var second = await fixture.GenerateDemoDataAsync();

        var managedCustomerCodes = Enumerable.Range(1, 30)
            .Select(sequence => DemoDataStableKeyCatalog.Create("CUSTOMER", sequence))
            .ToArray();
        await using var context = fixture.CreateDbContext();
        var customers = await context.Customers
            .Include(customer => customer.Company)
            .Include(customer => customer.TagRelations)
            .Where(customer => managedCustomerCodes.Contains(customer.Code))
            .OrderBy(customer => customer.Code)
            .ToListAsync();

        Assert.Equal(30, customers.Count);
        Assert.Equal(30, customers.Select(customer => customer.Code).Distinct().Count());
        Assert.Equal(30, first.CreatedByLayer["customers"] + first.ReusedByLayer["customers"]);
        Assert.Equal(0, second.CreatedByLayer["customers"]);
        Assert.All(customers, customer =>
        {
            Assert.NotNull(customer.Company);
            Assert.Single(customer.TagRelations);
            Assert.False(string.IsNullOrWhiteSpace(customer.UnifiedSocialCreditCode));
            Assert.False(string.IsNullOrWhiteSpace(customer.LegalRepresentative));
            Assert.False(string.IsNullOrWhiteSpace(customer.RegisteredCapital));
            Assert.NotNull(customer.EstablishDate);
            Assert.False(string.IsNullOrWhiteSpace(customer.BusinessTerm));
            Assert.False(string.IsNullOrWhiteSpace(customer.RegistrationStatus));
            Assert.False(string.IsNullOrWhiteSpace(customer.RegistrationAuthority));
            Assert.False(string.IsNullOrWhiteSpace(customer.RegisteredAddress));
            Assert.False(string.IsNullOrWhiteSpace(customer.BusinessScope));
            Assert.False(string.IsNullOrWhiteSpace(customer.InvoiceTitle));
            Assert.False(string.IsNullOrWhiteSpace(customer.TaxpayerIdentificationNumber));
            Assert.False(string.IsNullOrWhiteSpace(customer.InvoiceAddress));
            Assert.False(string.IsNullOrWhiteSpace(customer.InvoicePhone));
            Assert.False(string.IsNullOrWhiteSpace(customer.BankName));
            Assert.False(string.IsNullOrWhiteSpace(customer.BankAccount));
            Assert.False(string.IsNullOrWhiteSpace(customer.InvoiceReceiverName));
            Assert.False(string.IsNullOrWhiteSpace(customer.InvoiceReceiverPhone));
            Assert.False(string.IsNullOrWhiteSpace(customer.InvoiceReceiverAddress));
            Assert.False(string.IsNullOrWhiteSpace(customer.InvoiceEmail));
            Assert.False(string.IsNullOrWhiteSpace(customer.ContactName));
            Assert.False(string.IsNullOrWhiteSpace(customer.ContactPhone));
            Assert.False(string.IsNullOrWhiteSpace(customer.Address));
            Assert.False(string.IsNullOrWhiteSpace(customer.Remark));
            Assert.NotNull(customer.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(customer.CreateName));
        });
    }

    /// <summary>
    ///     生成器必须经商品分类应用服务补齐稳定编码的税务分类资料，覆盖免税与应税语义，并在重复运行时复用既有分类。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_CreatesManagedGoodsTypesWithTaxSemantics_AndSecondRunIsIdempotent()
    {
        var first = await fixture.GenerateDemoDataAsync();
        var second = await fixture.GenerateDemoDataAsync();

        var managedGoodsTypeCodes = Enumerable.Range(1, 30)
            .Select(sequence => DemoDataStableKeyCatalog.Create("GOODS-TYPE", sequence))
            .ToArray();
        await using var context = fixture.CreateDbContext();
        var goodsTypes = await context.GoodsTypes
            .Where(goodsType => managedGoodsTypeCodes.Contains(goodsType.Code))
            .OrderBy(goodsType => goodsType.Code)
            .ToListAsync();

        Assert.Equal(30, goodsTypes.Count);
        Assert.Equal(30, goodsTypes.Select(goodsType => goodsType.Code).Distinct().Count());
        Assert.Equal(30, first.CreatedByLayer["goods-types"] + first.ReusedByLayer["goods-types"]);
        Assert.Equal(0, second.CreatedByLayer["goods-types"]);
        Assert.Contains(goodsTypes, goodsType => goodsType.IsTaxExempt);
        Assert.Contains(goodsTypes, goodsType => !goodsType.IsTaxExempt);
        Assert.All(goodsTypes, goodsType =>
        {
            Assert.False(string.IsNullOrWhiteSpace(goodsType.Name));
            Assert.False(string.IsNullOrWhiteSpace(goodsType.ImageUrl));
            Assert.False(string.IsNullOrWhiteSpace(goodsType.TaxCategoryCode));
            Assert.False(string.IsNullOrWhiteSpace(goodsType.TaxCategoryName));
            Assert.False(string.IsNullOrWhiteSpace(goodsType.InvoiceGoodsShortName));
            Assert.False(string.IsNullOrWhiteSpace(goodsType.Remark));
            Assert.NotNull(goodsType.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(goodsType.CreateName));
            if (goodsType.IsTaxExempt)
            {
                Assert.Equal(0m, goodsType.DefaultTaxRate);
                Assert.False(string.IsNullOrWhiteSpace(goodsType.TaxPolicyBasis));
            }
            else
            {
                Assert.True(goodsType.DefaultTaxRate > 0m);
                Assert.Null(goodsType.TaxPolicyBasis);
            }
        });
    }

    /// <summary>
    ///     生成器必须经仓库应用服务补齐稳定编码的仓库资料，填满当前可写联系人与地址字段，并在重复运行时复用既有仓库。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_CreatesManagedWaresWithCompleteContactData_AndSecondRunIsIdempotent()
    {
        var first = await fixture.GenerateDemoDataAsync();
        var second = await fixture.GenerateDemoDataAsync();

        var managedWareCodes = Enumerable.Range(1, 30)
            .Select(sequence => DemoDataStableKeyCatalog.Create("WARE", sequence))
            .ToArray();
        await using var context = fixture.CreateDbContext();
        var wares = await context.Wares
            .Where(ware => managedWareCodes.Contains(ware.Code))
            .OrderBy(ware => ware.Code)
            .ToListAsync();

        Assert.Equal(30, wares.Count);
        Assert.Equal(30, wares.Select(ware => ware.Code).Distinct().Count());
        Assert.Equal(30, first.CreatedByLayer["wares"] + first.ReusedByLayer["wares"]);
        Assert.Equal(0, second.CreatedByLayer["wares"]);
        Assert.All(wares, ware =>
        {
            Assert.False(string.IsNullOrWhiteSpace(ware.Name));
            Assert.False(string.IsNullOrWhiteSpace(ware.ContactName));
            Assert.False(string.IsNullOrWhiteSpace(ware.ContactPhone));
            Assert.False(string.IsNullOrWhiteSpace(ware.Address));
            Assert.False(string.IsNullOrWhiteSpace(ware.Remark));
            Assert.NotNull(ware.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(ware.CreateName));
        });
    }

    /// <summary>
    ///     生成器必须经供应商应用服务补齐稳定编码的供应商资料，填满当前可写的联系、开户与税务字段，并在重复运行时复用既有供应商。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_CreatesManagedSuppliersWithCompleteCommercialData_AndSecondRunIsIdempotent()
    {
        var first = await fixture.GenerateDemoDataAsync();
        var second = await fixture.GenerateDemoDataAsync();

        var managedSupplierCodes = Enumerable.Range(1, 30)
            .Select(sequence => DemoDataStableKeyCatalog.Create("SUPPLIER", sequence))
            .ToArray();
        await using var context = fixture.CreateDbContext();
        var suppliers = await context.Suppliers
            .Where(supplier => managedSupplierCodes.Contains(supplier.Code))
            .OrderBy(supplier => supplier.Code)
            .ToListAsync();

        Assert.Equal(30, suppliers.Count);
        Assert.Equal(30, suppliers.Select(supplier => supplier.Code).Distinct().Count());
        Assert.Equal(30, first.CreatedByLayer["suppliers"] + first.ReusedByLayer["suppliers"]);
        Assert.Equal(0, second.CreatedByLayer["suppliers"]);
        Assert.All(suppliers, supplier =>
        {
            Assert.False(string.IsNullOrWhiteSpace(supplier.Name));
            Assert.False(string.IsNullOrWhiteSpace(supplier.ContactName));
            Assert.False(string.IsNullOrWhiteSpace(supplier.ContactPhone));
            Assert.False(string.IsNullOrWhiteSpace(supplier.Address));
            Assert.False(string.IsNullOrWhiteSpace(supplier.BankName));
            Assert.False(string.IsNullOrWhiteSpace(supplier.BankAccount));
            Assert.False(string.IsNullOrWhiteSpace(supplier.TaxNo));
            Assert.False(string.IsNullOrWhiteSpace(supplier.Remark));
            Assert.NotNull(supplier.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(supplier.CreateName));
        });
    }

    /// <summary>
    ///     生成器必须经采购员应用服务补齐稳定编码的采购员资料，关联现有系统用户与部门，并在重复运行时复用既有采购员。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_CreatesManagedPurchasersWithCompleteOrganizationalReferences_AndSecondRunIsIdempotent()
    {
        var first = await fixture.GenerateDemoDataAsync();
        var second = await fixture.GenerateDemoDataAsync();

        var managedPurchaserCodes = Enumerable.Range(1, 30)
            .Select(sequence => DemoDataStableKeyCatalog.Create("PURCHASER", sequence))
            .ToArray();
        await using var context = fixture.CreateDbContext();
        var purchasers = await context.Purchasers
            .Include(purchaser => purchaser.User)
            .Include(purchaser => purchaser.Department)
            .Where(purchaser => managedPurchaserCodes.Contains(purchaser.Code))
            .OrderBy(purchaser => purchaser.Code)
            .ToListAsync();

        Assert.Equal(30, purchasers.Count);
        Assert.Equal(30, purchasers.Select(purchaser => purchaser.Code).Distinct().Count());
        Assert.Equal(30, first.CreatedByLayer["purchasers"] + first.ReusedByLayer["purchasers"]);
        Assert.Equal(0, second.CreatedByLayer["purchasers"]);
        Assert.All(purchasers, purchaser =>
        {
            Assert.False(string.IsNullOrWhiteSpace(purchaser.Name));
            Assert.False(string.IsNullOrWhiteSpace(purchaser.Phone));
            Assert.False(string.IsNullOrWhiteSpace(purchaser.Remark));
            Assert.NotNull(purchaser.UserId);
            Assert.NotNull(purchaser.DepartmentId);
            Assert.NotNull(purchaser.User);
            Assert.NotNull(purchaser.Department);
            Assert.NotNull(purchaser.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(purchaser.CreateName));
        });
    }

    /// <summary>
    ///     生成器必须经商品与商品单位应用服务补齐稳定编码的商品资料；每个商品均应具备完整的分类、供应商、仓库和基础单位关系，并在重复运行时复用既有记录。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_CreatesManagedGoodsWithBaseUnitsAndBusinessReferences_AndSecondRunIsIdempotent()
    {
        var first = await fixture.GenerateDemoDataAsync();
        var second = await fixture.GenerateDemoDataAsync();

        var managedGoodsCodes = Enumerable.Range(1, 30)
            .Select(sequence => DemoDataStableKeyCatalog.Create("GOODS", sequence))
            .ToArray();
        var managedGoodsUnitCodes = Enumerable.Range(1, 30)
            .Select(sequence => DemoDataStableKeyCatalog.Create("GOODS-UNIT", sequence))
            .ToArray();
        await using var context = fixture.CreateDbContext();
        var goods = await context.Goods
            .Include(item => item.GoodsType)
            .Include(item => item.DefaultSupplier)
            .Include(item => item.DefaultWare)
            .Include(item => item.Units)
            .Include(item => item.SupplierRelations)
            .Where(item => managedGoodsCodes.Contains(item.Code))
            .OrderBy(item => item.Code)
            .ToListAsync();
        var units = await context.GoodsUnits
            .Where(item => managedGoodsUnitCodes.Contains(item.Code))
            .OrderBy(item => item.Code)
            .ToListAsync();

        Assert.Equal(30, goods.Count);
        Assert.Equal(30, units.Count);
        Assert.Equal(30, goods.Select(item => item.Code).Distinct().Count());
        Assert.Equal(30, units.Select(item => item.Code).Distinct().Count());
        Assert.Equal(30, first.CreatedByLayer["goods"] + first.ReusedByLayer["goods"]);
        Assert.Equal(30, first.CreatedByLayer["goods-units"] + first.ReusedByLayer["goods-units"]);
        Assert.Equal(0, second.CreatedByLayer["goods"]);
        Assert.Equal(0, second.CreatedByLayer["goods-units"]);
        Assert.All(goods, item =>
        {
            Assert.NotNull(item.GoodsType);
            Assert.NotNull(item.DefaultSupplier);
            Assert.NotNull(item.DefaultWare);
            Assert.NotNull(item.BaseUnitId);
            Assert.True(item.IsOnSale);
            Assert.False(string.IsNullOrWhiteSpace(item.Name));
            Assert.False(string.IsNullOrWhiteSpace(item.Spec));
            Assert.False(string.IsNullOrWhiteSpace(item.Brand));
            Assert.False(string.IsNullOrWhiteSpace(item.Origin));
            Assert.False(string.IsNullOrWhiteSpace(item.Description));
            Assert.NotNull(item.TaxRate);
            Assert.False(string.IsNullOrWhiteSpace(item.Remark));
            Assert.NotNull(item.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(item.CreateName));
            Assert.Single(item.Units);
            Assert.Single(item.SupplierRelations);
            Assert.Equal(item.BaseUnitId, item.Units.Single().Id);
        });
        Assert.All(units, unit =>
        {
            Assert.True(unit.IsBaseUnit);
            Assert.Equal(1m, unit.ConversionRate);
            Assert.False(string.IsNullOrWhiteSpace(unit.Name));
            Assert.False(string.IsNullOrWhiteSpace(unit.Remark));
            Assert.NotNull(unit.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(unit.CreateName));
        });
    }

    /// <summary>
    ///     生成器必须经报价与报价商品应用服务补齐稳定编码的报价资料，为每个受管客户和商品保留有效期、审核状态、单价及最小起订量，并在重复运行时复用既有记录。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_CreatesManagedQuotationsWithGoodsAndCustomerReferences_AndSecondRunIsIdempotent()
    {
        var first = await fixture.GenerateDemoDataAsync();
        var second = await fixture.GenerateDemoDataAsync();

        var managedQuotationCodes = Enumerable.Range(1, 30)
            .Select(sequence => DemoDataStableKeyCatalog.Create("QUOTATION", sequence))
            .ToArray();
        await using var context = fixture.CreateDbContext();
        var quotations = await context.Quotations
            .Include(quotation => quotation.Goods)
            .Include(quotation => quotation.CustomerQuotations)
            .Where(quotation => managedQuotationCodes.Contains(quotation.Code))
            .OrderBy(quotation => quotation.Code)
            .ToListAsync();

        Assert.Equal(30, quotations.Count);
        Assert.Equal(30, quotations.Select(quotation => quotation.Code).Distinct().Count());
        Assert.Equal(30, first.CreatedByLayer["quotations"] + first.ReusedByLayer["quotations"]);
        Assert.Equal(30, first.CreatedByLayer["quotation-goods"] + first.ReusedByLayer["quotation-goods"]);
        Assert.Equal(0, second.CreatedByLayer["quotations"]);
        Assert.Equal(0, second.CreatedByLayer["quotation-goods"]);
        Assert.Contains(quotations, quotation => quotation.IsAudited);
        Assert.Contains(quotations, quotation => !quotation.IsAudited);
        Assert.All(quotations, quotation =>
        {
            Assert.False(string.IsNullOrWhiteSpace(quotation.Name));
            Assert.False(string.IsNullOrWhiteSpace(quotation.Description));
            Assert.NotNull(quotation.EffectiveStart);
            Assert.NotNull(quotation.EffectiveEnd);
            Assert.True(quotation.EffectiveEnd >= quotation.EffectiveStart);
            Assert.NotNull(quotation.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(quotation.CreateName));
            var goods = Assert.Single(quotation.Goods);
            Assert.True(goods.UnitPrice > 0m);
            Assert.NotNull(goods.MinOrderQuantity);
            Assert.True(goods.MinOrderQuantity > 0m);
            Assert.True(goods.IsOnSale);
            Assert.False(string.IsNullOrWhiteSpace(goods.Remark));
            Assert.NotNull(goods.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(goods.CreateName));
            Assert.Single(quotation.CustomerQuotations);
        });
    }

    /// <summary>
    ///     生成器必须经客户协议价与协议商品应用服务补齐稳定编码的协议价格资料，关联受管报价、客户、商品和单位，并在重复运行时复用既有记录。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_CreatesManagedCustomerProtocolsWithGoodsAndCustomerReferences_AndSecondRunIsIdempotent()
    {
        var first = await fixture.GenerateDemoDataAsync();
        var second = await fixture.GenerateDemoDataAsync();

        var managedProtocolCodes = Enumerable.Range(1, 30)
            .Select(sequence => DemoDataStableKeyCatalog.Create("CUSTOMER-PROTOCOL", sequence))
            .ToArray();
        await using var context = fixture.CreateDbContext();
        var protocols = await context.CustomerProtocols
            .Include(protocol => protocol.Quotation)
            .Include(protocol => protocol.Goods)
            .Include(protocol => protocol.Customers)
            .Where(protocol => managedProtocolCodes.Contains(protocol.Code))
            .OrderBy(protocol => protocol.Code)
            .ToListAsync();

        Assert.Equal(30, protocols.Count);
        Assert.Equal(30, protocols.Select(protocol => protocol.Code).Distinct().Count());
        Assert.Equal(30, first.CreatedByLayer["customer-protocols"] + first.ReusedByLayer["customer-protocols"]);
        Assert.Equal(30, first.CreatedByLayer["customer-protocol-goods"] + first.ReusedByLayer["customer-protocol-goods"]);
        Assert.Equal(0, second.CreatedByLayer["customer-protocols"]);
        Assert.Equal(0, second.CreatedByLayer["customer-protocol-goods"]);
        Assert.All(protocols, protocol =>
        {
            Assert.NotNull(protocol.Quotation);
            Assert.True(protocol.EffectiveEnd >= protocol.EffectiveStart);
            Assert.False(string.IsNullOrWhiteSpace(protocol.Name));
            Assert.False(string.IsNullOrWhiteSpace(protocol.Remark));
            Assert.NotNull(protocol.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(protocol.CreateName));
            Assert.Single(protocol.Customers);

            var goods = Assert.Single(protocol.Goods);
            Assert.True(goods.ProtocolPrice > 0m);
            Assert.NotNull(goods.MinOrderQuantity);
            Assert.True(goods.MinOrderQuantity > 0m);
            Assert.False(string.IsNullOrWhiteSpace(goods.Remark));
            Assert.NotNull(goods.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(goods.CreateName));
        });
    }

    /// <summary>
    ///     生成器必须经采购规则应用服务补齐稳定编码的采购适用规则，关联受管供应商、采购员、仓库、分类、商品和客户，并在重复运行时复用既有记录。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_CreatesManagedPurchaseRulesWithCompleteBusinessReferences_AndSecondRunIsIdempotent()
    {
        var first = await fixture.GenerateDemoDataAsync();
        var second = await fixture.GenerateDemoDataAsync();

        var managedPurchaseRuleCodes = Enumerable.Range(1, 30)
            .Select(sequence => DemoDataStableKeyCatalog.Create("PURCHASE-RULE", sequence))
            .ToArray();
        await using var context = fixture.CreateDbContext();
        var purchaseRules = await context.PurchaseRules
            .Include(rule => rule.Supplier)
            .Include(rule => rule.Purchaser)
            .Include(rule => rule.Ware)
            .Include(rule => rule.GoodsType)
            .Include(rule => rule.Goods)
            .Include(rule => rule.Customers)
            .Where(rule => managedPurchaseRuleCodes.Contains(rule.Code))
            .OrderBy(rule => rule.Code)
            .ToListAsync();

        Assert.Equal(30, purchaseRules.Count);
        Assert.Equal(30, purchaseRules.Select(rule => rule.Code).Distinct().Count());
        Assert.Equal(30, first.CreatedByLayer["purchase-rules"] + first.ReusedByLayer["purchase-rules"]);
        Assert.Equal(0, second.CreatedByLayer["purchase-rules"]);
        Assert.Contains(purchaseRules, rule => rule.PurchasePattern == 1);
        Assert.Contains(purchaseRules, rule => rule.PurchasePattern == 2);
        Assert.All(purchaseRules, rule =>
        {
            Assert.False(string.IsNullOrWhiteSpace(rule.Name));
            Assert.False(string.IsNullOrWhiteSpace(rule.Remark));
            Assert.Equal(Status.Enable, rule.Status);
            Assert.NotNull(rule.Supplier);
            Assert.NotNull(rule.Purchaser);
            Assert.NotNull(rule.Ware);
            Assert.NotNull(rule.GoodsType);
            Assert.Single(rule.Goods);
            Assert.Single(rule.Customers);
            Assert.NotNull(rule.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(rule.CreateName));
        });
    }

    /// <summary>
    ///     生成器必须经配送基础资料应用服务补齐稳定编码的承运商、司机和路线，并将每条路线精确关联一名受管客户；重复运行不得新增记录。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_CreatesManagedDeliveryFoundationDataWithCustomerRoutes_AndSecondRunIsIdempotent()
    {
        var first = await fixture.GenerateDemoDataAsync();
        var second = await fixture.GenerateDemoDataAsync();

        var managedCarrierCodes = Enumerable.Range(1, 30)
            .Select(sequence => DemoDataStableKeyCatalog.Create("CARRIER", sequence))
            .ToArray();
        var managedDriverCodes = Enumerable.Range(1, 30)
            .Select(sequence => DemoDataStableKeyCatalog.Create("DRIVER", sequence))
            .ToArray();
        var managedRouteCodes = Enumerable.Range(1, 30)
            .Select(sequence => DemoDataStableKeyCatalog.Create("DELIVERY-ROUTE", sequence))
            .ToArray();
        var managedCustomerCodes = Enumerable.Range(1, 30)
            .Select(sequence => DemoDataStableKeyCatalog.Create("CUSTOMER", sequence))
            .ToArray();
        await using var context = fixture.CreateDbContext();
        var carriers = await context.Carriers
            .Where(carrier => managedCarrierCodes.Contains(carrier.Code))
            .OrderBy(carrier => carrier.Code)
            .ToListAsync();
        var drivers = await context.Drivers
            .Include(driver => driver.Carrier)
            .Where(driver => managedDriverCodes.Contains(driver.Code))
            .OrderBy(driver => driver.Code)
            .ToListAsync();
        var routes = await context.DeliveryRoutes
            .Include(route => route.CustomerRoutes)
            .ThenInclude(relation => relation.Customer)
            .Where(route => managedRouteCodes.Contains(route.Code))
            .OrderBy(route => route.Code)
            .ToListAsync();

        Assert.Equal(30, carriers.Count);
        Assert.Equal(30, drivers.Count);
        Assert.Equal(30, routes.Count);
        Assert.Equal(30, first.CreatedByLayer["carriers"] + first.ReusedByLayer["carriers"]);
        Assert.Equal(30, first.CreatedByLayer["drivers"] + first.ReusedByLayer["drivers"]);
        Assert.Equal(30, first.CreatedByLayer["delivery-routes"] + first.ReusedByLayer["delivery-routes"]);
        Assert.Equal(0, second.CreatedByLayer["carriers"]);
        Assert.Equal(0, second.CreatedByLayer["drivers"]);
        Assert.Equal(0, second.CreatedByLayer["delivery-routes"]);
        Assert.All(carriers, carrier =>
        {
            Assert.False(string.IsNullOrWhiteSpace(carrier.Name));
            Assert.False(string.IsNullOrWhiteSpace(carrier.ContactName));
            Assert.False(string.IsNullOrWhiteSpace(carrier.ContactPhone));
            Assert.False(string.IsNullOrWhiteSpace(carrier.Address));
            Assert.False(string.IsNullOrWhiteSpace(carrier.Remark));
            Assert.Equal(Status.Enable, carrier.Status);
            Assert.NotNull(carrier.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(carrier.CreateName));
        });
        Assert.All(drivers, driver =>
        {
            Assert.NotNull(driver.Carrier);
            Assert.False(string.IsNullOrWhiteSpace(driver.Phone));
            Assert.False(string.IsNullOrWhiteSpace(driver.PlateNumber));
            Assert.False(string.IsNullOrWhiteSpace(driver.LicenseNo));
            Assert.False(string.IsNullOrWhiteSpace(driver.Remark));
            Assert.Equal(Status.Enable, driver.Status);
            Assert.NotNull(driver.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(driver.CreateName));
        });
        Assert.All(routes, route =>
        {
            Assert.False(string.IsNullOrWhiteSpace(route.Name));
            Assert.False(string.IsNullOrWhiteSpace(route.Description));
            Assert.False(string.IsNullOrWhiteSpace(route.Remark));
            Assert.Equal(Status.Enable, route.Status);
            Assert.NotNull(route.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(route.CreateName));
            var relation = Assert.Single(route.CustomerRoutes);
            Assert.NotNull(relation.Customer);
            Assert.Contains(relation.Customer!.Code, managedCustomerCodes);
            Assert.True(relation.Sort > 0);
            Assert.NotNull(relation.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(relation.CreateName));
        });
    }

    /// <summary>
    ///     生成器必须经客户子账号应用服务为每个受管公司和客户补齐稳定登录账号；重复运行不得新增重复账号或修改非受管资料。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_CreatesManagedCustomerSubAccountsWithBusinessReferences_AndSecondRunIsIdempotent()
    {
        var first = await fixture.GenerateDemoDataAsync();
        var second = await fixture.GenerateDemoDataAsync();

        var managedUsernames = Enumerable.Range(1, 30)
            .Select(sequence => DemoDataStableKeyCatalog.Create("CUSTOMER-SUB-ACCOUNT", sequence))
            .ToArray();
        var managedCompanyCodes = Enumerable.Range(1, 30)
            .Select(sequence => DemoDataStableKeyCatalog.Create("COMPANY", sequence))
            .ToArray();
        var managedCustomerCodes = Enumerable.Range(1, 30)
            .Select(sequence => DemoDataStableKeyCatalog.Create("CUSTOMER", sequence))
            .ToArray();
        await using var context = fixture.CreateDbContext();
        var subAccounts = await context.CustomerSubAccounts
            .Include(subAccount => subAccount.Company)
            .Include(subAccount => subAccount.Customer)
            .Where(subAccount => managedUsernames.Contains(subAccount.Username))
            .OrderBy(subAccount => subAccount.Username)
            .ToListAsync();

        Assert.Equal(30, subAccounts.Count);
        Assert.Equal(30, subAccounts.Select(subAccount => subAccount.Username).Distinct().Count());
        Assert.Equal(30, first.CreatedByLayer["customer-sub-accounts"] + first.ReusedByLayer["customer-sub-accounts"]);
        Assert.Equal(0, second.CreatedByLayer["customer-sub-accounts"]);
        Assert.All(subAccounts, subAccount =>
        {
            Assert.NotNull(subAccount.Company);
            Assert.NotNull(subAccount.Customer);
            Assert.Contains(subAccount.Company.Code, managedCompanyCodes);
            Assert.Contains(subAccount.Customer!.Code, managedCustomerCodes);
            Assert.False(string.IsNullOrWhiteSpace(subAccount.NickName));
            Assert.False(string.IsNullOrWhiteSpace(subAccount.Phone));
            Assert.False(string.IsNullOrWhiteSpace(subAccount.Email));
            Assert.False(string.IsNullOrWhiteSpace(subAccount.PasswordHash));
            Assert.False(string.IsNullOrWhiteSpace(subAccount.Remark));
            Assert.Equal(Status.Enable, subAccount.Status);
            Assert.NotNull(subAccount.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(subAccount.CreateName));
        });
    }

    /// <summary>
    ///     生成器必须使用系统用户与角色应用服务补齐受管的权限联调资料；每个用户关联一条受管角色和四项既有菜单权限，重复运行不得新增重复关系或改动非受管记录。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_CreatesManagedSystemUsersRolesAndPermissions_AndSecondRunIsIdempotent()
    {
        var first = await fixture.GenerateDemoDataAsync();
        var second = await fixture.GenerateDemoDataAsync();

        var managedUsernames = Enumerable.Range(1, 30)
            .Select(sequence => DemoDataStableKeyCatalog.Create("SYSTEM-USER", sequence))
            .ToArray();
        var managedRoleCodes = Enumerable.Range(1, 30)
            .Select(sequence => DemoDataStableKeyCatalog.Create("SYSTEM-ROLE", sequence))
            .ToArray();
        await using var context = fixture.CreateDbContext();
        var users = await context.Users
            .Include(user => user.Department)
            .Include(user => user.UserRoles)
            .ThenInclude(relation => relation.Role)
            .Where(user => managedUsernames.Contains(user.Username))
            .OrderBy(user => user.Username)
            .ToListAsync();
        var roles = await context.Roles
            .Include(role => role.RoleMenus)
            .Where(role => managedRoleCodes.Contains(role.Code))
            .OrderBy(role => role.Code)
            .ToListAsync();

        Assert.Equal(30, users.Count);
        Assert.Equal(30, roles.Count);
        Assert.Equal(30, users.Select(user => user.Username).Distinct().Count());
        Assert.Equal(30, roles.Select(role => role.Code).Distinct().Count());
        Assert.Equal(30, first.CreatedByLayer["system-users"] + first.ReusedByLayer["system-users"]);
        Assert.Equal(30, first.CreatedByLayer["system-roles"] + first.ReusedByLayer["system-roles"]);
        Assert.Equal(0, second.CreatedByLayer["system-users"]);
        Assert.Equal(0, second.CreatedByLayer["system-roles"]);
        Assert.All(users, user =>
        {
            Assert.NotNull(user.Department);
            Assert.False(string.IsNullOrWhiteSpace(user.NickName));
            Assert.False(string.IsNullOrWhiteSpace(user.Phone));
            Assert.False(string.IsNullOrWhiteSpace(user.Email));
            Assert.False(string.IsNullOrWhiteSpace(user.PasswordHash));
            Assert.Equal(Status.Enable, user.Status);
            Assert.NotNull(user.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(user.CreateName));
            var relation = Assert.Single(user.UserRoles);
            Assert.NotNull(relation.Role);
            Assert.Contains(relation.Role!.Code, managedRoleCodes);
        });
        Assert.All(roles, role =>
        {
            Assert.False(string.IsNullOrWhiteSpace(role.Name));
            Assert.False(string.IsNullOrWhiteSpace(role.Desc));
            Assert.Equal(Status.Enable, role.Status);
            Assert.NotNull(role.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(role.CreateName));
            Assert.Equal(4, role.RoleMenus.Count);
        });
    }

    /// <summary>
    ///     生成器必须通过部门应用服务补齐受管组织资料；部门树覆盖根部门与子部门，负责人、联系人和审计字段完整，重复运行不得产生重复稳定编码。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_CreatesManagedDepartmentsWithOrganizationalHierarchy_AndSecondRunIsIdempotent()
    {
        var first = await fixture.GenerateDemoDataAsync();
        var second = await fixture.GenerateDemoDataAsync();

        var managedDepartmentCodes = Enumerable.Range(1, 30)
            .Select(sequence => DemoDataStableKeyCatalog.Create("DEPARTMENT", sequence))
            .ToArray();
        await using var context = fixture.CreateDbContext();
        var departments = await context.Departments
            .Include(department => department.Leader)
            .Where(department => managedDepartmentCodes.Contains(department.Code))
            .OrderBy(department => department.Code)
            .ToListAsync();

        Assert.Equal(30, departments.Count);
        Assert.Equal(30, departments.Select(department => department.Code).Distinct().Count());
        Assert.Equal(30, first.CreatedByLayer["departments"] + first.ReusedByLayer["departments"]);
        Assert.Equal(0, second.CreatedByLayer["departments"]);
        Assert.All(departments, department =>
        {
            Assert.False(string.IsNullOrWhiteSpace(department.Name));
            Assert.NotNull(department.Leader);
            Assert.False(string.IsNullOrWhiteSpace(department.LeaderName));
            Assert.False(string.IsNullOrWhiteSpace(department.Phone));
            Assert.False(string.IsNullOrWhiteSpace(department.Email));
            Assert.False(string.IsNullOrWhiteSpace(department.Remark));
            Assert.True(department.Sort > 0);
            Assert.Equal(Status.Enable, department.Status);
            Assert.NotNull(department.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(department.CreateName));
        });
        Assert.Contains(departments, department => department.ParentId is null);
        Assert.Contains(departments, department => department.ParentId is not null);
    }

    /// <summary>
    ///     生成器必须补齐运营设置、服务时段、通知公告、打印模板和审计样本；没有公开写入口的日志仅构造完整稳定键命中的受管记录。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_CreatesManagedSystemSupportData_AndSecondRunIsIdempotent()
    {
        var first = await fixture.GenerateDemoDataAsync();
        var second = await fixture.GenerateDemoDataAsync();

        var managedServicePeriodNames = Enumerable.Range(1, 30)
            .Select(sequence => $"{DemoDataStableKeyCatalog.Create("SERVICE-PERIOD", sequence)} 华东运营服务时段{sequence:D2}")
            .ToArray();
        var managedNoticeTitles = Enumerable.Range(1, 30)
            .Select(sequence => $"{DemoDataStableKeyCatalog.Create("NOTICE", sequence)} 前端联调公告{sequence:D2}")
            .ToArray();
        var managedPrintTemplateCodes = Enumerable.Range(1, 30)
            .Select(sequence => $"SKYROC_DEMO_PRINT_TEMPLATE_{sequence:D3}")
            .ToArray();
        var managedOperationDescriptions = Enumerable.Range(1, 120)
            .Select(sequence => $"{DemoDataStableKeyCatalog.Create("OPERATION-LOG", sequence)} 自动业务联调审计样本{sequence:D3}")
            .ToArray();
        var managedLoginUsernames = Enumerable.Range(1, 120)
            .Select(sequence => DemoDataStableKeyCatalog.Create("LOGIN-LOG", sequence))
            .ToArray();

        await using var context = fixture.CreateDbContext();
        var servicePeriods = await context.ServicePeriods
            .Where(period => managedServicePeriodNames.Contains(period.Name))
            .OrderBy(period => period.Name)
            .ToListAsync();
        var notices = await context.Notices
            .Where(notice => managedNoticeTitles.Contains(notice.Title))
            .OrderBy(notice => notice.Title)
            .ToListAsync();
        var templates = await context.PrintTemplates
            .Include(template => template.Fields)
            .Where(template => managedPrintTemplateCodes.Contains(template.TemplateCode))
            .OrderBy(template => template.TemplateCode)
            .ToListAsync();
        var operationLogs = await context.OperationLogs
            .Where(log => managedOperationDescriptions.Contains(log.Desc))
            .OrderBy(log => log.Desc)
            .ToListAsync();
        var loginLogs = await context.LoginLogs
            .Where(log => managedLoginUsernames.Contains(log.Username))
            .OrderBy(log => log.Username)
            .ToListAsync();
        var settings = await context.SystemSettings.ToListAsync();

        Assert.Equal(30, servicePeriods.Count);
        Assert.Equal(30, notices.Count);
        Assert.Equal(30, templates.Count);
        Assert.Equal(120, operationLogs.Count);
        Assert.Equal(120, loginLogs.Count);
        Assert.Contains(settings, setting => setting.SettingKey == SystemSettingKey.MiniProgramOrder);
        Assert.Contains(settings, setting => setting.SettingKey == SystemSettingKey.SortingWeight);
        Assert.Equal(30, first.CreatedByLayer["service-periods"] + first.ReusedByLayer["service-periods"]);
        Assert.Equal(30, first.CreatedByLayer["notices"] + first.ReusedByLayer["notices"]);
        Assert.Equal(30, first.CreatedByLayer["print-templates"] + first.ReusedByLayer["print-templates"]);
        Assert.Equal(120, first.CreatedByLayer["operation-logs"] + first.ReusedByLayer["operation-logs"]);
        Assert.Equal(120, first.CreatedByLayer["login-logs"] + first.ReusedByLayer["login-logs"]);
        Assert.Equal(0, second.CreatedByLayer["service-periods"]);
        Assert.Equal(0, second.CreatedByLayer["notices"]);
        Assert.Equal(0, second.CreatedByLayer["print-templates"]);
        Assert.Equal(0, second.CreatedByLayer["operation-logs"]);
        Assert.Equal(0, second.CreatedByLayer["login-logs"]);
        Assert.Contains(servicePeriods, period => period.Status == Status.Enable);
        Assert.Contains(servicePeriods, period => period.Status == Status.Disable);
        Assert.Contains(notices, notice => notice.NoticeStatus == NoticeStatus.Published);
        Assert.Contains(notices, notice => notice.NoticeStatus == NoticeStatus.Draft);
        Assert.All(servicePeriods, period =>
        {
            Assert.True(period.EndTime > period.StartTime);
            Assert.True(period.SortOrder > 0);
            Assert.NotNull(period.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(period.CreateName));
        });
        Assert.All(notices, notice =>
        {
            Assert.False(string.IsNullOrWhiteSpace(notice.Content));
            Assert.NotNull(notice.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(notice.CreateName));
            Assert.Equal(notice.NoticeStatus == NoticeStatus.Published, notice.PublishedTime is not null);
        });
        Assert.All(templates, template =>
        {
            Assert.False(string.IsNullOrWhiteSpace(template.Name));
            Assert.True(Enum.IsDefined(template.BusinessType));
            Assert.False(string.IsNullOrWhiteSpace(template.DesignJson));
            Assert.Equal(3, template.Fields.Count);
            Assert.NotNull(template.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(template.CreateName));
            Assert.All(template.Fields, field =>
            {
                Assert.False(string.IsNullOrWhiteSpace(field.FieldKey));
                Assert.False(string.IsNullOrWhiteSpace(field.DisplayName));
                Assert.NotNull(field.CreateBy);
                Assert.False(string.IsNullOrWhiteSpace(field.CreateName));
            });
        });
        Assert.All(operationLogs, log =>
        {
            Assert.False(string.IsNullOrWhiteSpace(log.Module));
            Assert.False(string.IsNullOrWhiteSpace(log.OperationType));
            Assert.False(string.IsNullOrWhiteSpace(log.Method));
            Assert.False(string.IsNullOrWhiteSpace(log.Url));
            Assert.False(string.IsNullOrWhiteSpace(log.IpAddress));
            Assert.False(string.IsNullOrWhiteSpace(log.Location));
            Assert.False(string.IsNullOrWhiteSpace(log.Browser));
            Assert.False(string.IsNullOrWhiteSpace(log.Os));
            Assert.True(log.ExecutionDuration > 0);
            Assert.NotNull(log.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(log.CreateName));
        });
        Assert.All(loginLogs, log =>
        {
            Assert.False(string.IsNullOrWhiteSpace(log.IpAddress));
            Assert.False(string.IsNullOrWhiteSpace(log.UserAgent));
            Assert.True(log.LoginTime > DateTime.MinValue);
            Assert.NotNull(log.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(log.CreateName));
            if (log.IsSuccess)
            {
                Assert.Null(log.FailureReason);
                Assert.NotNull(log.UserId);
            }
            else
            {
                Assert.False(string.IsNullOrWhiteSpace(log.FailureReason));
            }
        });
    }

    /// <summary>
    ///     生成器必须通过销售订单应用服务补齐受管订单层，保留客户、仓库、商品、单位和价格快照，并在重复运行时不新增重复订单。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_CreatesManagedSaleOrdersWithDetailsAndAuditLogs_AndSecondRunIsIdempotent()
    {
        var first = await fixture.GenerateDemoDataAsync();
        var second = await fixture.GenerateDemoDataAsync();

        var managedOrderKeys = Enumerable.Range(1, 60)
            .Select(sequence => DemoDataStableKeyCatalog.Create("SALE-ORDER", sequence))
            .ToArray();
        await using var context = fixture.CreateDbContext();
        var orders = await context.SaleOrders
            .Include(order => order.Customer)
            .Include(order => order.Ware)
            .Include(order => order.Quotation)
            .Include(order => order.Details)
            .ThenInclude(detail => detail.Goods)
            .Include(order => order.Details)
            .ThenInclude(detail => detail.GoodsUnit)
            .Include(order => order.AuditLogs)
            .Where(order => order.InnerRemark != null && managedOrderKeys.Contains(order.InnerRemark))
            .OrderBy(order => order.InnerRemark)
            .ToListAsync();

        Assert.Equal(60, orders.Count);
        Assert.Equal(60, orders.Select(order => order.InnerRemark).Distinct().Count());
        Assert.Equal(60, first.CreatedByLayer["sale-orders"] + first.ReusedByLayer["sale-orders"]);
        Assert.Equal(120, first.CreatedByLayer["sale-order-details"] + first.ReusedByLayer["sale-order-details"]);
        Assert.Equal(0, second.CreatedByLayer["sale-orders"]);
        Assert.Equal(0, second.CreatedByLayer["sale-order-details"]);
        Assert.Contains(orders, order => order.OrderStatus == SaleOrderStatus.Signed);
        Assert.Contains(orders, order => order.OrderStatus == SaleOrderStatus.Rejected);
        Assert.All(orders, order =>
        {
            Assert.NotNull(order.Customer);
            Assert.NotNull(order.Ware);
            Assert.NotNull(order.Quotation);
            Assert.False(string.IsNullOrWhiteSpace(order.OrderNo));
            Assert.False(string.IsNullOrWhiteSpace(order.CustomerNameSnapshot));
            Assert.False(string.IsNullOrWhiteSpace(order.CustomerCodeSnapshot));
            Assert.False(string.IsNullOrWhiteSpace(order.ContactNameSnapshot));
            Assert.False(string.IsNullOrWhiteSpace(order.ContactPhoneSnapshot));
            Assert.False(string.IsNullOrWhiteSpace(order.DeliveryAddressSnapshot));
            Assert.False(string.IsNullOrWhiteSpace(order.Remark));
            Assert.False(string.IsNullOrWhiteSpace(order.InnerRemark));
            Assert.True(order.OrderPrice > 0m);
            Assert.True(order.SettlementPrice > 0m);
            Assert.NotNull(order.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(order.CreateName));
            Assert.Equal(2, order.Details.Count);
            Assert.NotEmpty(order.AuditLogs);
            Assert.Contains(order.AuditLogs, log => log.Action == OrderAuditAction.Submit);
            Assert.All(order.Details, detail =>
            {
                Assert.NotNull(detail.Goods);
                Assert.NotNull(detail.GoodsUnit);
                Assert.False(string.IsNullOrWhiteSpace(detail.GoodsNameSnapshot));
                Assert.False(string.IsNullOrWhiteSpace(detail.GoodsCodeSnapshot));
                Assert.False(string.IsNullOrWhiteSpace(detail.GoodsTypeNameSnapshot));
                Assert.False(string.IsNullOrWhiteSpace(detail.GoodsDescriptionSnapshot));
                Assert.False(string.IsNullOrWhiteSpace(detail.GoodsUnitNameSnapshot));
                Assert.False(string.IsNullOrWhiteSpace(detail.BaseUnitNameSnapshot));
                Assert.False(string.IsNullOrWhiteSpace(detail.FixedGoodsUnitNameSnapshot));
                Assert.False(string.IsNullOrWhiteSpace(detail.Remark));
                Assert.False(string.IsNullOrWhiteSpace(detail.InnerRemark));
                Assert.True(detail.Quantity > 0m);
                Assert.True(detail.BaseQuantity > 0m);
                Assert.True(detail.FixedPrice > 0m);
                Assert.True(detail.TotalPrice > 0m);
                Assert.NotNull(detail.CreateBy);
                Assert.False(string.IsNullOrWhiteSpace(detail.CreateName));
            });
        });
    }

    /// <summary>
    ///     生成器必须基于已审核的受管销售订单补齐采购计划，保留来源订单关系、供应商和采购员快照，并在重复运行时不重复生成计划。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_CreatesManagedPurchasePlansFromApprovedSaleOrders_AndSecondRunIsIdempotent()
    {
        var first = await fixture.GenerateDemoDataAsync();
        var second = await fixture.GenerateDemoDataAsync();

        var managedPlanKeys = Enumerable.Range(1, 40)
            .Select(sequence => $"{DemoDataStableKeyCatalog.Create("PURCHASE-PLAN", sequence)} 华东联调采购计划{sequence:D2}：由已审核销售订单生成，用于采购单、入库和供应商结算链路。")
            .ToArray();
        var managedOrderKeys = Enumerable.Range(1, 60)
            .Select(sequence => DemoDataStableKeyCatalog.Create("SALE-ORDER", sequence))
            .ToArray();

        await using var context = fixture.CreateDbContext();
        var plans = await context.PurchasePlans
            .Include(plan => plan.Supplier)
            .Include(plan => plan.Purchaser)
            .Include(plan => plan.Details)
            .ThenInclude(detail => detail.Goods)
            .Include(plan => plan.Details)
            .ThenInclude(detail => detail.PurchaseUnit)
            .Include(plan => plan.Details)
            .ThenInclude(detail => detail.OrderRelations)
            .ThenInclude(relation => relation.SaleOrder)
            .Where(plan => plan.Remark != null && managedPlanKeys.Contains(plan.Remark))
            .OrderBy(plan => plan.Remark)
            .ToListAsync();

        Assert.Equal(40, plans.Count);
        Assert.Equal(40, plans.Select(plan => plan.Remark).Distinct().Count());
        Assert.Equal(40, first.CreatedByLayer["purchase-plans"] + first.ReusedByLayer["purchase-plans"]);
        Assert.Equal(80, first.CreatedByLayer["purchase-plan-details"] + first.ReusedByLayer["purchase-plan-details"]);
        Assert.Equal(80, first.CreatedByLayer["purchase-plan-order-relations"] + first.ReusedByLayer["purchase-plan-order-relations"]);
        Assert.Equal(0, second.CreatedByLayer["purchase-plans"]);
        Assert.Equal(0, second.CreatedByLayer["purchase-plan-details"]);
        Assert.Equal(0, second.CreatedByLayer["purchase-plan-order-relations"]);
        Assert.All(plans, plan =>
        {
            Assert.Equal(PurchasePlanStatus.Generated, plan.PurchaseStatus);
            Assert.Equal(PurchasePattern.SupplierDirect, plan.PurchasePattern);
            Assert.NotNull(plan.Supplier);
            Assert.NotNull(plan.Purchaser);
            Assert.False(string.IsNullOrWhiteSpace(plan.PlanNo));
            Assert.False(string.IsNullOrWhiteSpace(plan.SupplierNameSnapshot));
            Assert.False(string.IsNullOrWhiteSpace(plan.PurchaserNameSnapshot));
            Assert.False(string.IsNullOrWhiteSpace(plan.Remark));
            Assert.NotNull(plan.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(plan.CreateName));
            Assert.Equal(2, plan.Details.Count);

            Assert.All(plan.Details, detail =>
            {
                Assert.NotNull(detail.Goods);
                Assert.NotNull(detail.PurchaseUnit);
                Assert.False(string.IsNullOrWhiteSpace(detail.GoodsNameSnapshot));
                Assert.False(string.IsNullOrWhiteSpace(detail.GoodsCodeSnapshot));
                Assert.False(string.IsNullOrWhiteSpace(detail.PurchaseUnitNameSnapshot));
                Assert.False(string.IsNullOrWhiteSpace(detail.Remark));
                Assert.True(detail.RequiredQuantity > 0m);
                Assert.Equal(detail.RequiredQuantity, detail.PlannedQuantity);
                Assert.Equal(detail.PlannedQuantity, detail.PurchasedQuantity);
                Assert.NotNull(detail.CreateBy);
                Assert.False(string.IsNullOrWhiteSpace(detail.CreateName));
                var relation = Assert.Single(detail.OrderRelations);
                Assert.Contains(relation.SaleOrder.InnerRemark, managedOrderKeys);
                Assert.Equal(detail.RequiredQuantity, relation.RequiredQuantity);
                Assert.NotNull(relation.CreateBy);
                Assert.False(string.IsNullOrWhiteSpace(relation.CreateName));
            });
        });
    }

    /// <summary>
    ///     生成器必须基于受管采购计划生成长期采购单，补齐采购价格、生产日期和来源计划占用，并在重复运行时不重复创建单据。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_CreatesManagedPurchaseOrdersFromPlans_AndSecondRunIsIdempotent()
    {
        var first = await fixture.GenerateDemoDataAsync();
        var second = await fixture.GenerateDemoDataAsync();

        var managedOrderRemarks = Enumerable.Range(1, 50)
            .Select(sequence => $"{DemoDataStableKeyCatalog.Create("PURCHASE-ORDER", sequence)} 华东联调采购单{sequence:D2}：{(sequence <= 40 ? "由受管采购计划生成" : "由手工补货场景创建")}，用于采购入库、库存和供应商结算链路。")
            .ToArray();

        await using var context = fixture.CreateDbContext();
        var orders = await context.PurchaseOrders
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
            .Where(order => order.Remark != null && managedOrderRemarks.Contains(order.Remark))
            .OrderBy(order => order.Remark)
            .ToListAsync();

        Assert.Equal(50, orders.Count);
        Assert.Equal(50, orders.Select(order => order.Remark).Distinct().Count());
        Assert.Equal(50, first.CreatedByLayer["purchase-orders"] + first.ReusedByLayer["purchase-orders"]);
        Assert.Equal(100, first.CreatedByLayer["purchase-order-details"] + first.ReusedByLayer["purchase-order-details"]);
        Assert.Equal(80, first.CreatedByLayer["purchase-order-plan-relations"] + first.ReusedByLayer["purchase-order-plan-relations"]);
        Assert.Equal(0, second.CreatedByLayer["purchase-orders"]);
        Assert.Equal(0, second.CreatedByLayer["purchase-order-details"]);
        Assert.Equal(0, second.CreatedByLayer["purchase-order-plan-relations"]);
        Assert.Equal(40, orders.Count(order => order.BusinessStatus == PurchaseOrderStatus.Completed));
        Assert.Equal(5, orders.Count(order => order.BusinessStatus == PurchaseOrderStatus.Draft));
        Assert.Equal(5, orders.Count(order => order.BusinessStatus == PurchaseOrderStatus.Cancelled));
        Assert.All(orders, order =>
        {
            Assert.NotNull(order.Supplier);
            Assert.NotNull(order.Purchaser);
            Assert.False(string.IsNullOrWhiteSpace(order.PurchaseNo));
            Assert.False(string.IsNullOrWhiteSpace(order.SupplierNameSnapshot));
            Assert.False(string.IsNullOrWhiteSpace(order.SupplierContactNameSnapshot));
            Assert.False(string.IsNullOrWhiteSpace(order.SupplierContactPhoneSnapshot));
            Assert.False(string.IsNullOrWhiteSpace(order.PurchaserNameSnapshot));
            Assert.NotNull(order.ReceiveTime);
            Assert.False(string.IsNullOrWhiteSpace(order.Remark));
            Assert.NotNull(order.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(order.CreateName));
            Assert.Equal(2, order.Details.Count);

            var isPlanGenerated = order.Remark!.Contains("由受管采购计划生成", StringComparison.Ordinal);
            Assert.All(order.Details, detail =>
            {
                Assert.NotNull(detail.Goods);
                Assert.NotNull(detail.PurchaseUnit);
                Assert.False(string.IsNullOrWhiteSpace(detail.GoodsNameSnapshot));
                Assert.False(string.IsNullOrWhiteSpace(detail.GoodsCodeSnapshot));
                Assert.False(string.IsNullOrWhiteSpace(detail.GoodsInfoSnapshot));
                Assert.False(string.IsNullOrWhiteSpace(detail.PurchaseUnitNameSnapshot));
                Assert.True(detail.RequiredQuantity > 0m);
                Assert.True(detail.PurchaseQuantity > 0m);
                Assert.True(detail.PurchasePrice > 0m);
                Assert.Equal(
                    NumericPrecision.RoundMoney(detail.PurchaseQuantity * detail.PurchasePrice),
                    detail.PurchaseTotalPrice);
                Assert.NotNull(detail.ProductDate);
                Assert.False(string.IsNullOrWhiteSpace(detail.Remark));
                Assert.NotNull(detail.CreateBy);
                Assert.False(string.IsNullOrWhiteSpace(detail.CreateName));

                if (isPlanGenerated)
                {
                    var relation = Assert.Single(detail.PlanRelations);
                    Assert.Equal(detail.PurchaseQuantity, relation.AllocatedQuantity);
                    Assert.NotNull(relation.PurchasePlanDetail.PurchasePlan);
                    Assert.NotNull(relation.CreateBy);
                    Assert.False(string.IsNullOrWhiteSpace(relation.CreateName));
                }
                else
                {
                    Assert.Empty(detail.PlanRelations);
                }
            });
        });
    }

    /// <summary>
    ///     生成器必须基于已完成的受管采购单创建采购入库，审核后形成库存批次、库存流水和供应商待结账单，并在重复运行时不重复创建。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_CreatesManagedPurchaseStockInsWithBatchesLedgersAndSupplierBills_AndSecondRunIsIdempotent()
    {
        var first = await fixture.GenerateDemoDataAsync();
        var second = await fixture.GenerateDemoDataAsync();

        var managedStockInRemarks = Enumerable.Range(1, 40)
            .Select(sequence => $"{DemoDataStableKeyCatalog.Create("PURCHASE-STOCK-IN", sequence)} 华东联调采购入库{sequence:D2}：来源受管采购单，用于库存批次、流水和供应商待结链路。")
            .ToArray();

        await using var context = fixture.CreateDbContext();
        var stockIns = await context.StockInOrders
            .Include(order => order.Ware)
            .Include(order => order.Supplier)
            .Include(order => order.Purchaser)
            .Include(order => order.PurchaseOrder)
            .Include(order => order.Details)
            .ThenInclude(detail => detail.Goods)
            .Include(order => order.Details)
            .ThenInclude(detail => detail.GoodsUnit)
            .Include(order => order.Details)
            .ThenInclude(detail => detail.StockBatch)
            .Where(order => order.Remark != null && managedStockInRemarks.Contains(order.Remark))
            .OrderBy(order => order.Remark)
            .ToListAsync();
        var stockInIds = stockIns.Select(order => order.Id).ToArray();
        var stockInDetailIds = stockIns.SelectMany(order => order.Details.Select(detail => detail.Id)).ToArray();
        var ledgers = await context.StockLedgers
            .Where(ledger => stockInIds.Contains(ledger.SourceOrderId))
            .OrderBy(ledger => ledger.SourceDetailId)
            .ToListAsync();
        var supplierBills = await context.SupplierBills
            .Include(bill => bill.Details)
            .Where(bill => bill.StockInOrderId != null && stockInIds.Contains(bill.StockInOrderId.Value))
            .OrderBy(bill => bill.SourceDocumentNoSnapshot)
            .ToListAsync();

        Assert.Equal(40, stockIns.Count);
        Assert.Equal(40, stockIns.Select(order => order.Remark).Distinct().Count());
        Assert.Equal(40, first.CreatedByLayer["purchase-stock-ins"] + first.ReusedByLayer["purchase-stock-ins"]);
        Assert.Equal(80, first.CreatedByLayer["purchase-stock-in-details"] + first.ReusedByLayer["purchase-stock-in-details"]);
        Assert.Equal(80, first.CreatedByLayer["stock-batches"] + first.ReusedByLayer["stock-batches"]);
        Assert.Equal(80, first.CreatedByLayer["stock-ledgers"] + first.ReusedByLayer["stock-ledgers"]);
        Assert.Equal(40, first.CreatedByLayer["supplier-bills"] + first.ReusedByLayer["supplier-bills"]);
        Assert.Equal(80, first.CreatedByLayer["supplier-bill-details"] + first.ReusedByLayer["supplier-bill-details"]);
        Assert.Equal(0, second.CreatedByLayer["purchase-stock-ins"]);
        Assert.Equal(0, second.CreatedByLayer["purchase-stock-in-details"]);
        Assert.Equal(0, second.CreatedByLayer["stock-batches"]);
        Assert.Equal(0, second.CreatedByLayer["stock-ledgers"]);
        Assert.Equal(0, second.CreatedByLayer["supplier-bills"]);
        Assert.Equal(0, second.CreatedByLayer["supplier-bill-details"]);
        Assert.Equal(80, ledgers.Count);
        Assert.Equal(40, supplierBills.Count);
        Assert.All(stockIns, order =>
        {
            Assert.Equal(StockInOrderType.Purchase, order.OrderType);
            Assert.Equal(StockDocumentStatus.Audited, order.BusinessStatus);
            Assert.NotNull(order.Ware);
            Assert.NotNull(order.Supplier);
            Assert.NotNull(order.Purchaser);
            Assert.NotNull(order.PurchaseOrder);
            Assert.False(string.IsNullOrWhiteSpace(order.InNo));
            Assert.False(string.IsNullOrWhiteSpace(order.WareNameSnapshot));
            Assert.False(string.IsNullOrWhiteSpace(order.SupplierNameSnapshot));
            Assert.False(string.IsNullOrWhiteSpace(order.PurchaserNameSnapshot));
            Assert.NotNull(order.DepartmentId);
            Assert.False(string.IsNullOrWhiteSpace(order.DepartmentNameSnapshot));
            Assert.NotNull(order.ExpectedArrivalTime);
            Assert.True(order.TotalBaseQuantity > 0m);
            Assert.True(order.TotalAmount > 0m);
            Assert.NotNull(order.AuditUserId);
            Assert.False(string.IsNullOrWhiteSpace(order.AuditUserNameSnapshot));
            Assert.NotNull(order.AuditTime);
            Assert.NotNull(order.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(order.CreateName));
            Assert.Equal(2, order.Details.Count);
            Assert.All(order.Details, detail =>
            {
                Assert.NotNull(detail.PurchaseOrderDetailId);
                Assert.NotNull(detail.Goods);
                Assert.NotNull(detail.GoodsUnit);
                Assert.NotNull(detail.StockBatch);
                Assert.False(string.IsNullOrWhiteSpace(detail.GoodsNameSnapshot));
                Assert.False(string.IsNullOrWhiteSpace(detail.GoodsCodeSnapshot));
                Assert.False(string.IsNullOrWhiteSpace(detail.GoodsUnitNameSnapshot));
                Assert.False(string.IsNullOrWhiteSpace(detail.BatchNo));
                Assert.NotNull(detail.ProductDate);
                Assert.NotNull(detail.ExpireDate);
                Assert.True(detail.Quantity > 0m);
                Assert.True(detail.BaseQuantity > 0m);
                Assert.True(detail.UnitPrice > 0m);
                Assert.Equal(NumericPrecision.RoundMoney(detail.Quantity * detail.UnitPrice), detail.TotalPrice);
                Assert.False(string.IsNullOrWhiteSpace(detail.Remark));
                Assert.NotNull(detail.CreateBy);
                Assert.False(string.IsNullOrWhiteSpace(detail.CreateName));
            });
        });
        Assert.All(ledgers, ledger =>
        {
            Assert.Contains(ledger.SourceDetailId, stockInDetailIds);
            Assert.Equal(StockLedgerDirection.Increase, ledger.Direction);
            Assert.Equal(StockLedgerSourceType.PurchaseInbound, ledger.SourceType);
            Assert.True(ledger.ChangeQuantity > 0m);
            Assert.True(ledger.BalanceQuantity > 0m);
            Assert.True(ledger.UnitCost > 0m);
            Assert.True(ledger.TotalCost > 0m);
            Assert.False(string.IsNullOrWhiteSpace(ledger.Remark));
            Assert.NotNull(ledger.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(ledger.CreateName));
        });
        Assert.All(supplierBills, bill =>
        {
            Assert.Equal(SupplierBillSourceType.PurchaseStockIn, bill.SourceType);
            Assert.NotNull(bill.StockInOrderId);
            Assert.True(bill.PayableAmount > 0m);
            Assert.Equal(0m, bill.SettledAmount);
            Assert.NotEmpty(bill.Details);
            Assert.NotNull(bill.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(bill.CreateName));
        });
    }

    /// <summary>
    ///     生成器必须基于已审核采购入库形成的批次，为已审核受管销售订单创建销售出库，审核后扣减库存并追加销售出库流水。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_CreatesManagedSaleStockOutsWithBatchesAndLedgers_AndSecondRunIsIdempotent()
    {
        var first = await fixture.GenerateDemoDataAsync();
        var second = await fixture.GenerateDemoDataAsync();

        var managedStockOutRemarks = Enumerable.Range(1, 40)
            .Select(sequence => $"{DemoDataStableKeyCatalog.Create("SALE-STOCK-OUT", sequence)} 华东联调销售出库{sequence:D2}：来源受管销售订单，用于配送、签收和客户账单链路。")
            .ToArray();

        await using var context = fixture.CreateDbContext();
        var stockOuts = await context.StockOutOrders
            .Include(order => order.Ware)
            .Include(order => order.Customer)
            .Include(order => order.Department)
            .Include(order => order.SaleOrder)
            .Include(order => order.Details)
            .ThenInclude(detail => detail.Goods)
            .Include(order => order.Details)
            .ThenInclude(detail => detail.GoodsUnit)
            .Include(order => order.Details)
            .ThenInclude(detail => detail.StockBatch)
            .Where(order => order.Remark != null && managedStockOutRemarks.Contains(order.Remark))
            .OrderBy(order => order.Remark)
            .ToListAsync();
        var stockOutIds = stockOuts.Select(order => order.Id).ToArray();
        var stockOutDetailIds = stockOuts.SelectMany(order => order.Details.Select(detail => detail.Id)).ToArray();
        var ledgers = await context.StockLedgers
            .Where(ledger => stockOutIds.Contains(ledger.SourceOrderId))
            .OrderBy(ledger => ledger.SourceDetailId)
            .ToListAsync();

        Assert.Equal(40, stockOuts.Count);
        Assert.Equal(40, stockOuts.Select(order => order.Remark).Distinct().Count());
        Assert.Equal(40, first.CreatedByLayer["sale-stock-outs"] + first.ReusedByLayer["sale-stock-outs"]);
        Assert.Equal(80, first.CreatedByLayer["sale-stock-out-details"] + first.ReusedByLayer["sale-stock-out-details"]);
        Assert.Equal(80, first.CreatedByLayer["sale-stock-out-ledgers"] + first.ReusedByLayer["sale-stock-out-ledgers"]);
        Assert.Equal(0, second.CreatedByLayer["sale-stock-outs"]);
        Assert.Equal(0, second.CreatedByLayer["sale-stock-out-details"]);
        Assert.Equal(0, second.CreatedByLayer["sale-stock-out-ledgers"]);
        Assert.Equal(80, ledgers.Count);
        Assert.All(stockOuts, order =>
        {
            Assert.Equal(StockOutOrderType.Sale, order.OrderType);
            Assert.Equal(StockDocumentStatus.Audited, order.BusinessStatus);
            Assert.NotNull(order.Ware);
            Assert.NotNull(order.Customer);
            Assert.NotNull(order.Department);
            Assert.NotNull(order.SaleOrder);
            Assert.False(string.IsNullOrWhiteSpace(order.OutNo));
            Assert.False(string.IsNullOrWhiteSpace(order.WareNameSnapshot));
            Assert.False(string.IsNullOrWhiteSpace(order.CustomerNameSnapshot));
            Assert.False(string.IsNullOrWhiteSpace(order.DepartmentNameSnapshot));
            Assert.True(order.TotalBaseQuantity > 0m);
            Assert.True(order.TotalAmount > 0m);
            Assert.NotNull(order.AuditUserId);
            Assert.False(string.IsNullOrWhiteSpace(order.AuditUserNameSnapshot));
            Assert.NotNull(order.AuditTime);
            Assert.NotNull(order.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(order.CreateName));
            Assert.Equal(2, order.Details.Count);
            Assert.All(order.Details, detail =>
            {
                Assert.NotNull(detail.SaleOrderDetailId);
                Assert.NotNull(detail.Goods);
                Assert.NotNull(detail.GoodsUnit);
                Assert.NotNull(detail.StockBatch);
                Assert.False(string.IsNullOrWhiteSpace(detail.GoodsNameSnapshot));
                Assert.False(string.IsNullOrWhiteSpace(detail.GoodsCodeSnapshot));
                Assert.False(string.IsNullOrWhiteSpace(detail.GoodsUnitNameSnapshot));
                Assert.False(string.IsNullOrWhiteSpace(detail.BatchNoSnapshot));
                Assert.True(detail.Quantity > 0m);
                Assert.True(detail.BaseQuantity > 0m);
                Assert.True(detail.UnitPrice > 0m);
                Assert.Equal(NumericPrecision.RoundMoney(detail.Quantity * detail.UnitPrice), detail.TotalPrice);
                Assert.False(string.IsNullOrWhiteSpace(detail.Remark));
                Assert.NotNull(detail.CreateBy);
                Assert.False(string.IsNullOrWhiteSpace(detail.CreateName));
            });
        });
        Assert.All(ledgers, ledger =>
        {
            Assert.Contains(ledger.SourceDetailId, stockOutDetailIds);
            Assert.Equal(StockLedgerDirection.Decrease, ledger.Direction);
            Assert.Equal(StockLedgerSourceType.SalesOutbound, ledger.SourceType);
            Assert.True(ledger.ChangeQuantity > 0m);
            Assert.True(ledger.BalanceQuantity >= 0m);
            Assert.True(ledger.UnitCost > 0m);
            Assert.True(ledger.TotalCost > 0m);
            Assert.False(string.IsNullOrWhiteSpace(ledger.Remark));
            Assert.NotNull(ledger.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(ledger.CreateName));
        });
    }

    /// <summary>
    ///     生成器必须基于已审核销售出库生成配送任务，并完成司机分配、路线规划、配送签收、回单归档和客户账单同步。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_CreatesManagedDeliveryTasksReceiptsAndCustomerBills_AndSecondRunIsIdempotent()
    {
        var first = await fixture.GenerateDemoDataAsync();
        var second = await fixture.GenerateDemoDataAsync();

        var managedDeliveryRemarks = Enumerable.Range(1, 40)
            .Select(sequence => $"{DemoDataStableKeyCatalog.Create("DELIVERY-TASK", sequence)} 华东联调配送任务{sequence:D2}：来源受管销售出库，已完成分配、路线规划、签收和回单。")
            .ToArray();
        var managedReceiptRemarks = Enumerable.Range(1, 40)
            .Select(sequence => $"SkyRoc 联调配送签收：第 {sequence:D2} 张任务客户已按出库明细完成验收。")
            .ToArray();

        await using var context = fixture.CreateDbContext();
        var tasks = await context.DeliveryTasks
            .Include(task => task.StockOutOrder)
            .ThenInclude(order => order.Details)
            .Include(task => task.SaleOrder)
            .ThenInclude(order => order.Details)
            .Include(task => task.Driver)
            .Include(task => task.Carrier)
            .Include(task => task.Route)
            .Include(task => task.Receipt)
            .ThenInclude(receipt => receipt!.CheckDetails)
            .Where(task => task.Remark != null && managedDeliveryRemarks.Contains(task.Remark))
            .OrderBy(task => task.Remark)
            .ToListAsync();
        var saleOrderIds = tasks.Select(task => task.SaleOrderId).ToArray();
        var stockOutIds = tasks.Select(task => task.StockOutOrderId).ToArray();
        var receiptIds = tasks
            .Select(task => task.Receipt?.Id)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToArray();
        var customerBills = await context.CustomerBills
            .Include(bill => bill.Details)
            .Where(bill => saleOrderIds.Contains(bill.SaleOrderId))
            .OrderBy(bill => bill.SaleOrderNoSnapshot)
            .ToListAsync();

        first.CreatedByLayer.TryGetValue("delivery-tasks", out var createdDeliveryTasks);
        first.ReusedByLayer.TryGetValue("delivery-tasks", out var reusedDeliveryTasks);
        first.CreatedByLayer.TryGetValue("order-receipts", out var createdOrderReceipts);
        first.ReusedByLayer.TryGetValue("order-receipts", out var reusedOrderReceipts);
        first.CreatedByLayer.TryGetValue("order-check-details", out var createdCheckDetails);
        first.ReusedByLayer.TryGetValue("order-check-details", out var reusedCheckDetails);
        first.CreatedByLayer.TryGetValue("customer-bills", out var createdCustomerBills);
        first.ReusedByLayer.TryGetValue("customer-bills", out var reusedCustomerBills);
        first.CreatedByLayer.TryGetValue("customer-bill-details", out var createdCustomerBillDetails);
        first.ReusedByLayer.TryGetValue("customer-bill-details", out var reusedCustomerBillDetails);

        Assert.Equal(40, tasks.Count);
        Assert.Equal(40, tasks.Select(task => task.Remark).Distinct().Count());
        Assert.Equal(40, createdDeliveryTasks + reusedDeliveryTasks);
        Assert.Equal(40, createdOrderReceipts + reusedOrderReceipts);
        Assert.Equal(80, createdCheckDetails + reusedCheckDetails);
        Assert.Equal(40, createdCustomerBills + reusedCustomerBills);
        Assert.Equal(80, createdCustomerBillDetails + reusedCustomerBillDetails);
        Assert.Equal(0, second.CreatedByLayer.GetValueOrDefault("delivery-tasks"));
        Assert.Equal(0, second.CreatedByLayer.GetValueOrDefault("order-receipts"));
        Assert.Equal(0, second.CreatedByLayer.GetValueOrDefault("order-check-details"));
        Assert.Equal(0, second.CreatedByLayer.GetValueOrDefault("customer-bills"));
        Assert.Equal(0, second.CreatedByLayer.GetValueOrDefault("customer-bill-details"));
        Assert.Equal(40, receiptIds.Length);
        Assert.Equal(40, customerBills.Count);
        Assert.All(tasks, task =>
        {
            Assert.Equal(DeliveryTaskStatus.Signed, task.DeliveryStatus);
            Assert.Contains(task.StockOutOrderId, stockOutIds);
            Assert.NotNull(task.StockOutOrder);
            Assert.NotNull(task.SaleOrder);
            Assert.NotNull(task.Driver);
            Assert.NotNull(task.Carrier);
            Assert.NotNull(task.Route);
            Assert.NotNull(task.Receipt);
            Assert.False(string.IsNullOrWhiteSpace(task.TaskNo));
            Assert.False(string.IsNullOrWhiteSpace(task.CustomerNameSnapshot));
            Assert.False(string.IsNullOrWhiteSpace(task.ContactNameSnapshot));
            Assert.False(string.IsNullOrWhiteSpace(task.ContactPhoneSnapshot));
            Assert.False(string.IsNullOrWhiteSpace(task.DeliveryAddressSnapshot));
            Assert.False(string.IsNullOrWhiteSpace(task.WareNameSnapshot));
            Assert.False(string.IsNullOrWhiteSpace(task.DriverNameSnapshot));
            Assert.False(string.IsNullOrWhiteSpace(task.DriverPhoneSnapshot));
            Assert.False(string.IsNullOrWhiteSpace(task.CarrierNameSnapshot));
            Assert.False(string.IsNullOrWhiteSpace(task.RouteNameSnapshot));
            Assert.NotNull(task.RouteSequence);
            Assert.NotNull(task.AssignedTime);
            Assert.NotNull(task.PlannedTime);
            Assert.NotNull(task.StartedTime);
            Assert.NotNull(task.SignedTime);
            Assert.NotNull(task.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(task.CreateName));
            Assert.Equal(SaleOrderStatus.Signed, task.SaleOrder.OrderStatus);
            Assert.Equal(OrderReturnStatus.Returned, task.SaleOrder.ReturnStatus);

            var receipt = task.Receipt!;
            Assert.Contains(receipt.SignRemark, managedReceiptRemarks);
            Assert.Equal(task.SaleOrderId, receipt.SaleOrderId);
            Assert.Equal(task.StockOutOrderId, receipt.StockOutOrderId);
            Assert.False(string.IsNullOrWhiteSpace(receipt.ReceiptNo));
            Assert.False(string.IsNullOrWhiteSpace(receipt.SignerName));
            Assert.False(string.IsNullOrWhiteSpace(receipt.ReceiptImageUrl));
            Assert.NotNull(receipt.ReturnedTime);
            Assert.False(string.IsNullOrWhiteSpace(receipt.ReturnRemark));
            Assert.NotNull(receipt.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(receipt.CreateName));
            Assert.Equal(task.StockOutOrder.Details.Count, receipt.CheckDetails.Count);
            Assert.All(receipt.CheckDetails, detail =>
            {
                Assert.Equal(receipt.Id, detail.OrderReceiptId);
                Assert.Equal(OrderCustomerCheckStatus.Accepted, detail.CheckStatus);
                Assert.True(detail.DeliveredBaseQuantity > 0m);
                Assert.Equal(detail.DeliveredBaseQuantity, detail.AcceptedBaseQuantity);
                Assert.True(detail.AcceptedAmount > 0m);
                Assert.False(string.IsNullOrWhiteSpace(detail.Remark));
                Assert.NotNull(detail.CreateBy);
                Assert.False(string.IsNullOrWhiteSpace(detail.CreateName));
            });
        });
        Assert.All(customerBills, bill =>
        {
            Assert.Contains(bill.SaleOrderId, saleOrderIds);
            Assert.Equal(CustomerBillStatus.Pending, bill.BillStatus);
            Assert.True(bill.OrderAmount > 0m);
            Assert.Equal(0m, bill.AfterSaleAdjustmentAmount);
            Assert.Equal(bill.OrderAmount, bill.ReceivableAmount);
            Assert.Equal(0m, bill.SettledAmount);
            Assert.Equal(2, bill.Details.Count);
            Assert.NotNull(bill.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(bill.CreateName));
            Assert.All(bill.Details, detail =>
            {
                Assert.Equal(CustomerBillDetailSourceType.OrderAcceptance, detail.SourceType);
                Assert.NotNull(detail.SaleOrderDetailId);
                Assert.True(detail.Quantity > 0m);
                Assert.True(detail.BaseQuantity > 0m);
                Assert.True(detail.UnitPrice > 0m);
                Assert.True(detail.Amount > 0m);
                Assert.False(string.IsNullOrWhiteSpace(detail.GoodsNameSnapshot));
                Assert.False(string.IsNullOrWhiteSpace(detail.GoodsCodeSnapshot));
                Assert.False(string.IsNullOrWhiteSpace(detail.GoodsUnitNameSnapshot));
                Assert.NotNull(detail.CreateBy);
                Assert.False(string.IsNullOrWhiteSpace(detail.CreateName));
            });
        });
    }
}
