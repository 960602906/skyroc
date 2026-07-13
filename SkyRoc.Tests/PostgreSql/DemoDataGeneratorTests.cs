using Microsoft.EntityFrameworkCore;
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
}
