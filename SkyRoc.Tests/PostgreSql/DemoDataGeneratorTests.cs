using Microsoft.EntityFrameworkCore;
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
}
