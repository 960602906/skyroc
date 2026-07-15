using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Domain.Entities.AfterSales;
using Domain.Entities.Delivery;
using Domain.Entities.Orders;
using Domain.Entities.Printing;
using Domain.Entities.Purchases;
using Domain.Entities.Finance;
using Domain.Entities.ImportExport;
using Domain.Entities.Storage;
using Domain.Entities.System;
using Domain.Entities.Traceability;
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
            Assert.Equal(4, customer.TagRelations.Count);
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
            Assert.Equal(4, item.SupplierRelations.Count);
            Assert.Equal(1, item.SupplierRelations.Count(relation => relation.IsDefault));
            Assert.Equal(item.DefaultSupplierId, item.SupplierRelations.Single(relation => relation.IsDefault).SupplierId);
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
    ///     生成器必须把受管客户标签关系与商品供应商关系补齐到明细关系目标下限：每名受管客户绑定 4 个标签、每个受管商品绑定 4 个供应商，重复运行不得改写既有主键组合。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_CreatesManagedCustomerTagAndGoodsSupplierRelations_AndSecondRunIsIdempotent()
    {
        var first = await fixture.GenerateDemoDataAsync();
        var second = await fixture.GenerateDemoDataAsync();

        var managedCustomerCodes = Enumerable.Range(1, 30)
            .Select(sequence => DemoDataStableKeyCatalog.Create("CUSTOMER", sequence))
            .ToArray();
        var managedGoodsCodes = Enumerable.Range(1, 30)
            .Select(sequence => DemoDataStableKeyCatalog.Create("GOODS", sequence))
            .ToArray();
        var managedTagCodes = Enumerable.Range(1, 30)
            .Select(sequence => DemoDataStableKeyCatalog.Create("CUSTOMER-TAG", sequence))
            .ToArray();
        var managedSupplierCodes = Enumerable.Range(1, 30)
            .Select(sequence => DemoDataStableKeyCatalog.Create("SUPPLIER", sequence))
            .ToArray();

        await using var context = fixture.CreateDbContext();
        var customers = await context.Customers
            .Include(customer => customer.TagRelations)
            .ThenInclude(relation => relation.CustomerTag)
            .Where(customer => managedCustomerCodes.Contains(customer.Code))
            .OrderBy(customer => customer.Code)
            .ToListAsync();
        var goods = await context.Goods
            .Include(item => item.SupplierRelations)
            .ThenInclude(relation => relation.Supplier)
            .Where(item => managedGoodsCodes.Contains(item.Code))
            .OrderBy(item => item.Code)
            .ToListAsync();

        var managedCustomerTagRelations = customers
            .SelectMany(customer => customer.TagRelations)
            .ToList();
        var managedGoodsSupplierRelations = goods
            .SelectMany(item => item.SupplierRelations)
            .ToList();

        Assert.Equal(30, customers.Count);
        Assert.Equal(30, goods.Count);
        Assert.Equal(120, managedCustomerTagRelations.Count);
        Assert.Equal(120, managedGoodsSupplierRelations.Count);
        Assert.Equal(120, first.CreatedByLayer["customer-tag-relations"] + first.ReusedByLayer["customer-tag-relations"]);
        Assert.Equal(120, first.CreatedByLayer["goods-supplier-relations"] + first.ReusedByLayer["goods-supplier-relations"]);
        Assert.Equal(0, second.CreatedByLayer["customer-tag-relations"]);
        Assert.Equal(0, second.CreatedByLayer["goods-supplier-relations"]);
        Assert.All(customers, customer =>
        {
            Assert.Equal(4, customer.TagRelations.Count);
            Assert.All(customer.TagRelations, relation =>
            {
                Assert.NotNull(relation.CustomerTag);
                Assert.Contains(relation.CustomerTag.Code, managedTagCodes);
            });
        });
        Assert.All(goods, item =>
        {
            Assert.Equal(4, item.SupplierRelations.Count);
            Assert.Equal(1, item.SupplierRelations.Count(relation => relation.IsDefault));
            Assert.Equal(item.DefaultSupplierId, item.SupplierRelations.Single(relation => relation.IsDefault).SupplierId);
            Assert.All(item.SupplierRelations, relation =>
            {
                Assert.NotNull(relation.Supplier);
                Assert.Contains(relation.Supplier.Code, managedSupplierCodes);
            });
        });
    }

    /// <summary>
    ///     生成器必须通过安全文件服务保存真实 PNG，并将受保护下载地址作为商品图片绑定到受管商品；重复运行不得改写文件、图片主键或审计快照。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_CreatesManagedStoredFilesAndGoodsImages_AndSecondRunIsIdempotent()
    {
        var managedFileNames = Enumerable.Range(1, 30)
            .Select(sequence => $"{DemoDataStableKeyCatalog.Create("STORED-FILE", sequence)}.png")
            .ToArray();
        var managedGoodsCodes = Enumerable.Range(1, 30)
            .Select(sequence => DemoDataStableKeyCatalog.Create("GOODS", sequence))
            .ToArray();

        var first = await fixture.GenerateDemoDataAsync();
        await using var firstContext = fixture.CreateDbContext();
        var firstFiles = await firstContext.StoredFiles
            .Where(file => managedFileNames.Contains(file.OriginalFileName))
            .OrderBy(file => file.OriginalFileName)
            .Select(file => new
            {
                file.Id,
                file.StorageKey,
                file.OriginalFileName,
                file.ContentType,
                file.FileSize,
                file.CreateTime,
                file.CreateBy,
                file.CreateName
            })
            .ToArrayAsync();
        var firstImages = await firstContext.GoodsImages
            .Where(image => image.FileName != null && managedFileNames.Contains(image.FileName))
            .OrderBy(image => image.FileName)
            .Select(image => new
            {
                image.Id,
                image.GoodsId,
                image.Url,
                image.FileName,
                image.Sort,
                image.IsPrimary,
                image.CreateTime,
                image.CreateBy,
                image.CreateName
            })
            .ToArrayAsync();

        var second = await fixture.GenerateDemoDataAsync();
        await using var secondContext = fixture.CreateDbContext();
        var files = await secondContext.StoredFiles
            .Where(file => managedFileNames.Contains(file.OriginalFileName))
            .OrderBy(file => file.OriginalFileName)
            .ToArrayAsync();
        var images = await secondContext.GoodsImages
            .Include(image => image.Goods)
            .Where(image => image.FileName != null && managedFileNames.Contains(image.FileName))
            .OrderBy(image => image.FileName)
            .ToArrayAsync();

        Assert.Equal(30, files.Length);
        Assert.Equal(30, images.Length);
        Assert.Equal(30, files.Select(file => file.StorageKey).Distinct().Count());
        Assert.Equal(30, images.Select(image => image.GoodsId).Distinct().Count());
        Assert.Equal(30, first.CreatedByLayer["stored-files"] + first.ReusedByLayer["stored-files"]);
        Assert.Equal(30, first.CreatedByLayer["goods-images"] + first.ReusedByLayer["goods-images"]);
        Assert.Equal(0, second.CreatedByLayer["stored-files"]);
        Assert.Equal(0, second.CreatedByLayer["goods-images"]);
        Assert.Equal(
            firstFiles,
            files.Select(file => new
            {
                file.Id,
                file.StorageKey,
                file.OriginalFileName,
                file.ContentType,
                file.FileSize,
                file.CreateTime,
                file.CreateBy,
                file.CreateName
            }).ToArray());
        Assert.Equal(
            firstImages,
            images.Select(image => new
            {
                image.Id,
                image.GoodsId,
                image.Url,
                image.FileName,
                image.Sort,
                image.IsPrimary,
                image.CreateTime,
                image.CreateBy,
                image.CreateName
            }).ToArray());

        var storageRoot = Path.Combine(fixture.Settings.ReportDirectory, "demo-files");
        Assert.All(files, file =>
        {
            Assert.Equal("image/png", file.ContentType);
            Assert.True(file.FileSize > 8);
            Assert.NotNull(file.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(file.CreateName));
            var physicalPath = Path.Combine(storageRoot, file.StorageKey.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(physicalPath));
            Assert.Equal(file.FileSize, new FileInfo(physicalPath).Length);
        });
        Assert.All(images, image =>
        {
            Assert.NotNull(image.Goods);
            Assert.Contains(image.Goods.Code, managedGoodsCodes);
            Assert.Equal($"/api/files/{files.Single(file => file.OriginalFileName == image.FileName).Id}/download", image.Url);
            Assert.Equal(1, image.Sort);
            Assert.True(image.IsPrimary);
            Assert.NotNull(image.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(image.CreateName));
        });
    }

    /// <summary>
    ///     生成器必须补齐稳定编号的导入导出任务，覆盖导入、导出和全部执行状态；重复运行不得改写任务主键、状态或审计快照。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_CreatesManagedImportExportJobsWithStatusCoverage_AndSecondRunIsIdempotent()
    {
        var managedJobNos = Enumerable.Range(1, 30)
            .Select(sequence => DemoDataStableKeyCatalog.Create("IMPORT-EXPORT-JOB", sequence))
            .ToArray();

        var first = await fixture.GenerateDemoDataAsync();
        await using var firstContext = fixture.CreateDbContext();
        var firstJobs = await firstContext.ImportExportJobs
            .AsNoTracking()
            .Where(job => managedJobNos.Contains(job.JobNo))
            .OrderBy(job => job.JobNo)
            .Select(job => new
            {
                job.Id,
                job.JobNo,
                job.JobType,
                job.JobDirection,
                job.JobStatus,
                job.SourceFileName,
                job.TotalRows,
                job.SuccessRows,
                job.FailureRows,
                job.ErrorSummary,
                job.JobStartedAt,
                job.JobFinishedAt,
                job.CreateTime,
                job.CreateBy,
                job.CreateName,
                job.UpdateTime,
                job.UpdateBy,
                job.UpdateName,
                job.Status
            })
            .ToArrayAsync();

        var second = await fixture.GenerateDemoDataAsync();
        await using var secondContext = fixture.CreateDbContext();
        var jobs = await secondContext.ImportExportJobs
            .AsNoTracking()
            .Where(job => managedJobNos.Contains(job.JobNo))
            .OrderBy(job => job.JobNo)
            .ToArrayAsync();

        Assert.Equal(30, jobs.Length);
        Assert.Equal(managedJobNos, jobs.Select(job => job.JobNo).ToArray());
        Assert.Equal(15, jobs.Count(job => job.JobDirection == ImportExportDirection.Import));
        Assert.Equal(15, jobs.Count(job => job.JobDirection == ImportExportDirection.Export));
        Assert.Equal(10, jobs.Count(job => job.JobStatus == ImportExportJobStatus.Processing));
        Assert.Equal(10, jobs.Count(job => job.JobStatus == ImportExportJobStatus.Succeeded));
        Assert.Equal(10, jobs.Count(job => job.JobStatus == ImportExportJobStatus.Failed));
        Assert.Equal(30, jobs.Select(job => job.JobStartedAt).Distinct().Count());
        Assert.Equal(
            30,
            first.CreatedByLayer.GetValueOrDefault("import-export-jobs")
            + first.ReusedByLayer.GetValueOrDefault("import-export-jobs"));
        Assert.Equal(0, second.CreatedByLayer.GetValueOrDefault("import-export-jobs"));
        Assert.Equal(30, second.ReusedByLayer.GetValueOrDefault("import-export-jobs"));
        Assert.Equal(
            firstJobs,
            jobs.Select(job => new
            {
                job.Id,
                job.JobNo,
                job.JobType,
                job.JobDirection,
                job.JobStatus,
                job.SourceFileName,
                job.TotalRows,
                job.SuccessRows,
                job.FailureRows,
                job.ErrorSummary,
                job.JobStartedAt,
                job.JobFinishedAt,
                job.CreateTime,
                job.CreateBy,
                job.CreateName,
                job.UpdateTime,
                job.UpdateBy,
                job.UpdateName,
                job.Status
            }).ToArray());

        Assert.All(jobs, job =>
        {
            Assert.Equal(ImportExportJobType.Goods, job.JobType);
            Assert.EndsWith(".csv", job.SourceFileName, StringComparison.Ordinal);
            Assert.NotNull(job.CreateTime);
            Assert.NotNull(job.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(job.CreateName));
            Assert.NotNull(job.UpdateTime);
            Assert.Equal(job.CreateBy, job.UpdateBy);
            Assert.Equal(job.CreateName, job.UpdateName);
            Assert.Equal(Status.Enable, job.Status);

            switch (job.JobStatus)
            {
                case ImportExportJobStatus.Processing:
                    Assert.Equal(0, job.TotalRows);
                    Assert.Equal(0, job.SuccessRows);
                    Assert.Equal(0, job.FailureRows);
                    Assert.Null(job.ErrorSummary);
                    Assert.Null(job.JobFinishedAt);
                    break;
                case ImportExportJobStatus.Succeeded:
                    Assert.True(job.TotalRows > 0);
                    Assert.Equal(job.TotalRows, job.SuccessRows);
                    Assert.Equal(0, job.FailureRows);
                    Assert.Null(job.ErrorSummary);
                    Assert.NotNull(job.JobFinishedAt);
                    break;
                case ImportExportJobStatus.Failed:
                    Assert.True(job.TotalRows > 0);
                    Assert.Equal(0, job.SuccessRows);
                    Assert.Equal(job.TotalRows, job.FailureRows);
                    Assert.False(string.IsNullOrWhiteSpace(job.ErrorSummary));
                    Assert.NotNull(job.JobFinishedAt);
                    break;
                default:
                    throw new InvalidOperationException($"未覆盖的导入导出任务状态：{job.JobStatus}。");
            }
        });
    }

    /// <summary>
    ///     生成器必须补齐关联真实订单、检测报告和溯源记录的脱敏外部报送日志，覆盖全部状态且重复运行不改写只追加事实。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_CreatesManagedExternalPushLogsWithSourceAndStatusCoverage_AndSecondRunIsIdempotent()
    {
        const string managedRequestPrefix = "{\"demoKey\":\"SKYROC-DEMO-EXTERNAL-PUSH-LOG-";

        var first = await fixture.GenerateDemoDataAsync();
        await using var firstContext = fixture.CreateDbContext();
        var firstLogs = await firstContext.ExternalPushLogs
            .AsNoTracking()
            .Where(log => log.RequestContent != null && log.RequestContent.StartsWith(managedRequestPrefix))
            .OrderBy(log => log.RequestContent)
            .Select(log => new
            {
                log.Id,
                log.BusinessType,
                log.BusinessId,
                log.BusinessNoSnapshot,
                log.PlatformCode,
                log.PushStatus,
                log.PushTime,
                log.ResponseTime,
                log.RequestContent,
                log.ResponseContent,
                log.ErrorMessage,
                log.RetryCount,
                log.CreateTime,
                log.CreateBy,
                log.CreateName,
                log.UpdateTime,
                log.UpdateBy,
                log.UpdateName,
                log.Status
            })
            .ToArrayAsync();

        var second = await fixture.GenerateDemoDataAsync();
        await using var secondContext = fixture.CreateDbContext();
        var logs = await secondContext.ExternalPushLogs
            .AsNoTracking()
            .Where(log => log.RequestContent != null && log.RequestContent.StartsWith(managedRequestPrefix))
            .OrderBy(log => log.RequestContent)
            .ToArrayAsync();

        Assert.Equal(120, logs.Length);
        Assert.Equal(40, logs.Count(log => log.BusinessType == ExternalPushBusinessType.SaleOrder));
        Assert.Equal(40, logs.Count(log => log.BusinessType == ExternalPushBusinessType.InspectionReport));
        Assert.Equal(40, logs.Count(log => log.BusinessType == ExternalPushBusinessType.TraceRecord));
        Assert.Equal(40, logs.Count(log => log.PushStatus == ExternalPushStatus.Pending));
        Assert.Equal(40, logs.Count(log => log.PushStatus == ExternalPushStatus.Success));
        Assert.Equal(40, logs.Count(log => log.PushStatus == ExternalPushStatus.Failed));
        Assert.Equal(3, logs.Select(log => log.PlatformCode).Distinct().Count());
        Assert.Equal(
            120,
            first.CreatedByLayer.GetValueOrDefault("external-push-logs")
            + first.ReusedByLayer.GetValueOrDefault("external-push-logs"));
        Assert.Equal(0, second.CreatedByLayer.GetValueOrDefault("external-push-logs"));
        Assert.Equal(120, second.ReusedByLayer.GetValueOrDefault("external-push-logs"));
        Assert.Equal(
            firstLogs,
            logs.Select(log => new
            {
                log.Id,
                log.BusinessType,
                log.BusinessId,
                log.BusinessNoSnapshot,
                log.PlatformCode,
                log.PushStatus,
                log.PushTime,
                log.ResponseTime,
                log.RequestContent,
                log.ResponseContent,
                log.ErrorMessage,
                log.RetryCount,
                log.CreateTime,
                log.CreateBy,
                log.CreateName,
                log.UpdateTime,
                log.UpdateBy,
                log.UpdateName,
                log.Status
            }).ToArray());

        var saleOrderNos = await secondContext.SaleOrders
            .AsNoTracking()
            .ToDictionaryAsync(order => order.Id, order => order.OrderNo);
        var inspectionNos = await secondContext.InspectionReports
            .AsNoTracking()
            .ToDictionaryAsync(report => report.Id, report => report.InspectionNo);
        var traceNos = await secondContext.TraceRecords
            .AsNoTracking()
            .ToDictionaryAsync(trace => trace.Id, trace => trace.TraceNo);

        Assert.All(logs, log =>
        {
            var expectedBusinessNo = log.BusinessType switch
            {
                ExternalPushBusinessType.SaleOrder => saleOrderNos.GetValueOrDefault(log.BusinessId),
                ExternalPushBusinessType.InspectionReport => inspectionNos.GetValueOrDefault(log.BusinessId),
                ExternalPushBusinessType.TraceRecord => traceNos.GetValueOrDefault(log.BusinessId),
                _ => null
            };
            Assert.Equal(expectedBusinessNo, log.BusinessNoSnapshot);
            Assert.False(string.IsNullOrWhiteSpace(log.RequestContent));
            Assert.DoesNotContain("password", log.RequestContent, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("authorization", log.RequestContent, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("accessToken", log.RequestContent, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("signingKey", log.RequestContent, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("connectionString", log.RequestContent, StringComparison.OrdinalIgnoreCase);
            Assert.InRange(log.RetryCount, 0, 3);
            Assert.NotNull(log.CreateTime);
            Assert.NotNull(log.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(log.CreateName));
            Assert.NotNull(log.UpdateTime);
            Assert.Equal(log.CreateBy, log.UpdateBy);
            Assert.Equal(log.CreateName, log.UpdateName);
            Assert.Equal(Status.Enable, log.Status);

            switch (log.PushStatus)
            {
                case ExternalPushStatus.Pending:
                    Assert.Null(log.ResponseTime);
                    Assert.Null(log.ResponseContent);
                    Assert.Null(log.ErrorMessage);
                    break;
                case ExternalPushStatus.Success:
                    Assert.True(log.ResponseTime > log.PushTime);
                    Assert.False(string.IsNullOrWhiteSpace(log.ResponseContent));
                    Assert.Null(log.ErrorMessage);
                    break;
                case ExternalPushStatus.Failed:
                    Assert.True(log.ResponseTime > log.PushTime);
                    Assert.False(string.IsNullOrWhiteSpace(log.ResponseContent));
                    Assert.False(string.IsNullOrWhiteSpace(log.ErrorMessage));
                    break;
                default:
                    throw new InvalidOperationException($"未覆盖的外部报送状态：{log.PushStatus}。");
            }
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
    ///     生成器必须仅向既有管理菜单补齐完整稳定编码的权限按钮，按钮描述和创建审计完整，重复运行不得新增或修改非受管菜单按钮。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_CreatesManagedMenuButtons_AndSecondRunIsIdempotent()
    {
        var first = await fixture.GenerateDemoDataAsync();
        var second = await fixture.GenerateDemoDataAsync();

        var managedCodes = Enumerable.Range(1, 30)
            .Select(sequence => DemoDataStableKeyCatalog.Create("MENU-BUTTON", sequence))
            .ToArray();
        await using var context = fixture.CreateDbContext();
        var buttons = await context.MenuButtons
            .Include(button => button.Menu)
            .Where(button => managedCodes.Contains(button.Code))
            .OrderBy(button => button.Code)
            .ToListAsync();

        Assert.Equal(30, buttons.Count);
        Assert.Equal(30, buttons.Select(button => button.Code).Distinct().Count());
        Assert.Equal(30, first.CreatedByLayer["menu-buttons"] + first.ReusedByLayer["menu-buttons"]);
        Assert.Equal(0, second.CreatedByLayer["menu-buttons"]);
        Assert.All(buttons, button =>
        {
            Assert.NotNull(button.Menu);
            Assert.Equal("manage", button.Menu.Name);
            Assert.Equal($"前端联调管理权限按钮{int.Parse(button.Code[^3..]):D2}", button.Desc);
            Assert.NotNull(button.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(button.CreateName));
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

    /// <summary>
    ///     生成器必须基于受管溯源销售出库补齐配送异常任务，均衡覆盖待处理和已处理异常，并在重复运行时保持任务与异常事实不变。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_CreatesManagedDeliveryExceptionsWithPendingAndHandledStates_AndSecondRunIsIdempotent()
    {
        const string managedExceptionPrefix = "SKYROC-DEMO-DELIVERY-EXCEPTION-";
        const string managedTaskPrefix = "SKYROC-DEMO-DELIVERY-EXCEPTION-TASK-";

        var first = await fixture.GenerateDemoDataAsync();
        await using var firstContext = fixture.CreateDbContext();
        var firstExceptions = await firstContext.DeliveryExceptions
            .AsNoTracking()
            .Where(exception => exception.Description.StartsWith(managedExceptionPrefix))
            .OrderBy(exception => exception.Description)
            .Select(exception => new
            {
                exception.Id,
                exception.ExceptionNo,
                exception.DeliveryTaskId,
                exception.DriverId,
                exception.CustomerId,
                exception.Description,
                exception.HandleStatus,
                exception.HandleRemark,
                exception.HandleTime,
                exception.CreateTime,
                exception.CreateBy,
                exception.CreateName,
                exception.UpdateTime,
                exception.UpdateBy,
                exception.UpdateName,
                exception.Status
            })
            .ToArrayAsync();
        var firstTasks = await firstContext.DeliveryTasks
            .AsNoTracking()
            .Where(task => task.Remark != null && task.Remark.StartsWith(managedTaskPrefix))
            .OrderBy(task => task.Remark)
            .Select(task => new
            {
                task.Id,
                task.TaskNo,
                task.StockOutOrderId,
                task.SaleOrderId,
                task.CustomerId,
                task.DriverId,
                task.CarrierId,
                task.RouteId,
                task.DeliveryStatus,
                task.AssignedTime,
                task.PlannedTime,
                task.StartedTime,
                task.SignedTime,
                task.Remark,
                task.CreateTime,
                task.CreateBy,
                task.CreateName,
                task.UpdateTime,
                task.UpdateBy,
                task.UpdateName,
                task.Status
            })
            .ToArrayAsync();

        var second = await fixture.GenerateDemoDataAsync();
        await using var secondContext = fixture.CreateDbContext();
        var exceptions = await secondContext.DeliveryExceptions
            .AsNoTracking()
            .Where(exception => exception.Description.StartsWith(managedExceptionPrefix))
            .OrderBy(exception => exception.Description)
            .ToArrayAsync();
        var tasks = await secondContext.DeliveryTasks
            .AsNoTracking()
            .Where(task => task.Remark != null && task.Remark.StartsWith(managedTaskPrefix))
            .OrderBy(task => task.Remark)
            .ToArrayAsync();

        Assert.Equal(40, exceptions.Length);
        Assert.Equal(20, exceptions.Count(exception => exception.HandleStatus == DeliveryExceptionStatus.Pending));
        Assert.Equal(20, exceptions.Count(exception => exception.HandleStatus == DeliveryExceptionStatus.Handled));
        Assert.Equal(20, exceptions.Select(exception => exception.DeliveryTaskId).Distinct().Count());
        Assert.Equal(20, tasks.Length);
        Assert.Equal(10, tasks.Count(task => task.DeliveryStatus == DeliveryTaskStatus.Delivering));
        Assert.Equal(10, tasks.Count(task => task.DeliveryStatus == DeliveryTaskStatus.Exception));
        Assert.Equal(
            40,
            first.CreatedByLayer.GetValueOrDefault("delivery-exceptions")
            + first.ReusedByLayer.GetValueOrDefault("delivery-exceptions"));
        Assert.Equal(
            20,
            first.CreatedByLayer.GetValueOrDefault("delivery-exception-tasks")
            + first.ReusedByLayer.GetValueOrDefault("delivery-exception-tasks"));
        Assert.Equal(0, second.CreatedByLayer.GetValueOrDefault("delivery-exceptions"));
        Assert.Equal(40, second.ReusedByLayer.GetValueOrDefault("delivery-exceptions"));
        Assert.Equal(0, second.CreatedByLayer.GetValueOrDefault("delivery-exception-tasks"));
        Assert.Equal(20, second.ReusedByLayer.GetValueOrDefault("delivery-exception-tasks"));
        Assert.Equal(
            firstExceptions,
            exceptions.Select(exception => new
            {
                exception.Id,
                exception.ExceptionNo,
                exception.DeliveryTaskId,
                exception.DriverId,
                exception.CustomerId,
                exception.Description,
                exception.HandleStatus,
                exception.HandleRemark,
                exception.HandleTime,
                exception.CreateTime,
                exception.CreateBy,
                exception.CreateName,
                exception.UpdateTime,
                exception.UpdateBy,
                exception.UpdateName,
                exception.Status
            }).ToArray());
        Assert.Equal(
            firstTasks,
            tasks.Select(task => new
            {
                task.Id,
                task.TaskNo,
                task.StockOutOrderId,
                task.SaleOrderId,
                task.CustomerId,
                task.DriverId,
                task.CarrierId,
                task.RouteId,
                task.DeliveryStatus,
                task.AssignedTime,
                task.PlannedTime,
                task.StartedTime,
                task.SignedTime,
                task.Remark,
                task.CreateTime,
                task.CreateBy,
                task.CreateName,
                task.UpdateTime,
                task.UpdateBy,
                task.UpdateName,
                task.Status
            }).ToArray());

        Assert.All(tasks, task =>
        {
            Assert.NotEqual(Guid.Empty, task.StockOutOrderId);
            Assert.NotEqual(Guid.Empty, task.SaleOrderId);
            Assert.NotEqual(Guid.Empty, task.CustomerId);
            Assert.NotNull(task.DriverId);
            Assert.NotNull(task.CarrierId);
            Assert.NotNull(task.RouteId);
            Assert.NotNull(task.AssignedTime);
            Assert.NotNull(task.PlannedTime);
            Assert.NotNull(task.StartedTime);
            Assert.Null(task.SignedTime);
            Assert.NotNull(task.CreateTime);
            Assert.NotNull(task.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(task.CreateName));
            Assert.Equal(Status.Enable, task.Status);
            Assert.Equal(2, exceptions.Count(exception => exception.DeliveryTaskId == task.Id));
        });
        Assert.All(exceptions, exception =>
        {
            var task = tasks.Single(task => task.Id == exception.DeliveryTaskId);
            Assert.Equal(task.DriverId, exception.DriverId);
            Assert.Equal(task.CustomerId, exception.CustomerId);
            Assert.False(string.IsNullOrWhiteSpace(exception.ExceptionNo));
            Assert.NotNull(exception.CreateTime);
            Assert.NotNull(exception.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(exception.CreateName));
            Assert.Equal(Status.Enable, exception.Status);
            if (exception.HandleStatus == DeliveryExceptionStatus.Handled)
            {
                Assert.False(string.IsNullOrWhiteSpace(exception.HandleRemark));
                Assert.NotNull(exception.HandleTime);
                Assert.NotNull(exception.UpdateTime);
                Assert.Equal(exception.CreateBy, exception.UpdateBy);
                Assert.Equal(exception.CreateName, exception.UpdateName);
            }
            else
            {
                Assert.Null(exception.HandleRemark);
                Assert.Null(exception.HandleTime);
            }
        });
    }

    /// <summary>
    ///     生成器必须基于已签收订单形成售后草稿、审核、取货、销售退货入库和账单冲减链路，并在第二次运行时保持幂等。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_CreatesManagedAfterSalesPickupAndSalesReturns_AndSecondRunIsIdempotent()
    {
        var first = await fixture.GenerateDemoDataAsync();
        var second = await fixture.GenerateDemoDataAsync();

        var managedAfterSaleRemarks = Enumerable.Range(1, 40)
            .Select(sequence =>
                $"{DemoDataStableKeyCatalog.Create("AFTER-SALE", sequence)} 华东联调售后单{sequence:D2}：基于已签收销售订单形成退款、退货、取货和账单冲减样本。")
            .ToArray();
        var managedSalesReturnRemarks = Enumerable.Range(21, 20)
            .Select(sequence =>
                $"{DemoDataStableKeyCatalog.Create("SALES-RETURN-STOCK-IN", sequence)} 华东联调销售退货入库{sequence:D2}：来源受管售后取货任务，审核后回补库存并支撑售后完成。")
            .ToArray();

        await using var context = fixture.CreateDbContext();
        var afterSales = await context.AfterSales
            .Include(afterSale => afterSale.Goods)
            .Include(afterSale => afterSale.AuditLogs)
            .Include(afterSale => afterSale.PickupTasks)
            .ThenInclude(task => task.StockInDetail)
            .ThenInclude(detail => detail!.StockInOrder)
            .Where(afterSale => afterSale.Remark != null && managedAfterSaleRemarks.Contains(afterSale.Remark))
            .OrderBy(afterSale => afterSale.Remark)
            .ToListAsync();
        var salesReturns = await context.StockInOrders
            .Include(order => order.Details)
            .Where(order => order.Remark != null
                            && managedSalesReturnRemarks.Contains(order.Remark)
                            && order.OrderType == StockInOrderType.SalesReturn)
            .OrderBy(order => order.Remark)
            .ToListAsync();
        var completedSaleOrderIds = afterSales
            .Where(afterSale => afterSale.AfterStatus == AfterSaleStatus.Completed)
            .Select(afterSale => afterSale.SaleOrderId!.Value)
            .ToArray();
        var customerBills = await context.CustomerBills
            .Include(bill => bill.Details)
            .Where(bill => completedSaleOrderIds.Contains(bill.SaleOrderId))
            .ToListAsync();

        first.CreatedByLayer.TryGetValue("after-sales", out var createdAfterSales);
        first.ReusedByLayer.TryGetValue("after-sales", out var reusedAfterSales);
        first.CreatedByLayer.TryGetValue("after-sale-goods", out var createdAfterSaleGoods);
        first.ReusedByLayer.TryGetValue("after-sale-goods", out var reusedAfterSaleGoods);
        first.CreatedByLayer.TryGetValue("after-sale-audit-logs", out var createdAfterSaleAuditLogs);
        first.ReusedByLayer.TryGetValue("after-sale-audit-logs", out var reusedAfterSaleAuditLogs);
        first.CreatedByLayer.TryGetValue("pickup-tasks", out var createdPickupTasks);
        first.ReusedByLayer.TryGetValue("pickup-tasks", out var reusedPickupTasks);
        first.CreatedByLayer.TryGetValue("sales-return-stock-ins", out var createdSalesReturnStockIns);
        first.ReusedByLayer.TryGetValue("sales-return-stock-ins", out var reusedSalesReturnStockIns);
        first.CreatedByLayer.TryGetValue("sales-return-stock-in-details", out var createdSalesReturnStockInDetails);
        first.ReusedByLayer.TryGetValue("sales-return-stock-in-details", out var reusedSalesReturnStockInDetails);

        Assert.Equal(40, afterSales.Count);
        Assert.Equal(40, afterSales.Select(afterSale => afterSale.Remark).Distinct().Count());
        Assert.Equal(40, createdAfterSales + reusedAfterSales);
        Assert.Equal(40, createdAfterSaleGoods + reusedAfterSaleGoods);
        Assert.True(createdAfterSaleAuditLogs + reusedAfterSaleAuditLogs >= 55);
        Assert.Equal(25, createdPickupTasks + reusedPickupTasks);
        Assert.Equal(20, createdSalesReturnStockIns + reusedSalesReturnStockIns);
        Assert.Equal(20, createdSalesReturnStockInDetails + reusedSalesReturnStockInDetails);
        Assert.Equal(0, second.CreatedByLayer.GetValueOrDefault("after-sales"));
        Assert.Equal(0, second.CreatedByLayer.GetValueOrDefault("after-sale-goods"));
        Assert.Equal(0, second.CreatedByLayer.GetValueOrDefault("after-sale-audit-logs"));
        Assert.Equal(0, second.CreatedByLayer.GetValueOrDefault("pickup-tasks"));
        Assert.Equal(0, second.CreatedByLayer.GetValueOrDefault("sales-return-stock-ins"));
        Assert.Equal(0, second.CreatedByLayer.GetValueOrDefault("sales-return-stock-in-details"));
        Assert.Equal(5, afterSales.Count(afterSale => afterSale.AfterStatus == AfterSaleStatus.Draft));
        Assert.Equal(5, afterSales.Count(afterSale => afterSale.AfterStatus == AfterSaleStatus.PendingAudit));
        Assert.Equal(5, afterSales.Count(afterSale => afterSale.AfterStatus == AfterSaleStatus.RefundPending));
        Assert.Equal(5, afterSales.Count(afterSale => afterSale.AfterStatus == AfterSaleStatus.ReturnPending));
        Assert.Equal(20, afterSales.Count(afterSale => afterSale.AfterStatus == AfterSaleStatus.Completed));
        Assert.Equal(20, salesReturns.Count);
        Assert.All(afterSales, afterSale =>
        {
            Assert.False(string.IsNullOrWhiteSpace(afterSale.AfterSaleNo));
            Assert.NotNull(afterSale.SaleOrderId);
            Assert.False(string.IsNullOrWhiteSpace(afterSale.CustomerNameSnapshot));
            Assert.False(string.IsNullOrWhiteSpace(afterSale.Source));
            Assert.False(string.IsNullOrWhiteSpace(afterSale.ContactNameSnapshot));
            Assert.False(string.IsNullOrWhiteSpace(afterSale.ContactPhoneSnapshot));
            Assert.False(string.IsNullOrWhiteSpace(afterSale.PickupAddressSnapshot));
            Assert.False(string.IsNullOrWhiteSpace(afterSale.Remark));
            Assert.NotNull(afterSale.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(afterSale.CreateName));
            Assert.Single(afterSale.Goods);
            Assert.All(afterSale.Goods, goods =>
            {
                Assert.True(goods.ActualRefundQuantity > 0m);
                Assert.True(goods.BaseRefundQuantity > 0m);
                Assert.True(goods.UnitPrice > 0m);
                Assert.False(string.IsNullOrWhiteSpace(goods.GoodsNameSnapshot));
                Assert.False(string.IsNullOrWhiteSpace(goods.GoodsCodeSnapshot));
                Assert.False(string.IsNullOrWhiteSpace(goods.GoodsUnitNameSnapshot));
                Assert.False(string.IsNullOrWhiteSpace(goods.Remark));
                Assert.NotNull(goods.SupplierId);
                Assert.NotNull(goods.DepartmentId);
                Assert.NotNull(goods.CreateBy);
                Assert.False(string.IsNullOrWhiteSpace(goods.CreateName));
            });
            Assert.All(afterSale.AuditLogs, log =>
            {
                Assert.False(string.IsNullOrWhiteSpace(log.AuditUserNameSnapshot));
                Assert.False(string.IsNullOrWhiteSpace(log.Remark));
                Assert.NotNull(log.CreateBy);
                Assert.False(string.IsNullOrWhiteSpace(log.CreateName));
            });
        });
        Assert.All(
            afterSales.Where(afterSale => afterSale.AfterStatus is AfterSaleStatus.ReturnPending or AfterSaleStatus.Completed),
            afterSale =>
            {
                var task = Assert.Single(afterSale.PickupTasks);
                Assert.False(string.IsNullOrWhiteSpace(task.TaskNo));
                Assert.False(string.IsNullOrWhiteSpace(task.PickupAddressSnapshot));
                Assert.False(string.IsNullOrWhiteSpace(task.Remark));
                Assert.NotNull(task.CreateBy);
                Assert.False(string.IsNullOrWhiteSpace(task.CreateName));
            });
        Assert.All(
            afterSales.Where(afterSale => afterSale.AfterStatus == AfterSaleStatus.Completed),
            afterSale =>
            {
                var task = Assert.Single(afterSale.PickupTasks);
                Assert.Equal(PickupTaskStatus.Completed, task.PickupStatus);
                Assert.NotNull(task.DriverId);
                Assert.NotNull(task.AssignedTime);
                Assert.NotNull(task.StartedTime);
                Assert.NotNull(task.CompletedTime);
                Assert.NotNull(task.StockInDetail);
                Assert.Equal(StockDocumentStatus.Audited, task.StockInDetail!.StockInOrder.BusinessStatus);
            });
        Assert.All(salesReturns, order =>
        {
            Assert.Equal(StockInOrderType.SalesReturn, order.OrderType);
            Assert.Equal(StockDocumentStatus.Audited, order.BusinessStatus);
            Assert.NotNull(order.AfterSaleId);
            Assert.NotNull(order.CustomerId);
            Assert.NotNull(order.DepartmentId);
            Assert.False(string.IsNullOrWhiteSpace(order.Remark));
            Assert.NotNull(order.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(order.CreateName));
            Assert.Single(order.Details);
            Assert.All(order.Details, detail =>
            {
                Assert.NotNull(detail.PickupTaskId);
                Assert.True(detail.Quantity > 0m);
                Assert.True(detail.UnitPrice > 0m);
                Assert.False(string.IsNullOrWhiteSpace(detail.BatchNo));
                Assert.False(string.IsNullOrWhiteSpace(detail.Remark));
                Assert.NotNull(detail.CreateBy);
                Assert.False(string.IsNullOrWhiteSpace(detail.CreateName));
            });
        });
        Assert.All(customerBills, bill =>
        {
            Assert.True(bill.AfterSaleAdjustmentAmount < 0m);
            Assert.Contains(
                bill.Details,
                detail => detail.SourceType == CustomerBillDetailSourceType.AfterSaleAdjustment
                          && detail.Amount < 0m
                          && detail.AfterSaleId.HasValue
                          && detail.AfterSaleGoodsId.HasValue);
        });
    }

    /// <summary>
    ///     生成器必须通过客户结款服务形成部分结款、全额结款和作废凭证，并在第二次运行时不重复核销账单余额。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_CreatesManagedCustomerSettlementsWithPartialSettledAndVoidedStates_AndSecondRunIsIdempotent()
    {
        var first = await fixture.GenerateDemoDataAsync();
        var firstDatabaseSnapshot = await CaptureManagedCustomerSettlementDatabaseSnapshotAsync();
        var second = await fixture.GenerateDemoDataAsync();
        var secondDatabaseSnapshot = await CaptureManagedCustomerSettlementDatabaseSnapshotAsync();

        var managedSerialNumbers = Enumerable.Range(1, 100)
            .Select(sequence => DemoDataStableKeyCatalog.Create("CUSTOMER-SETTLEMENT", sequence))
            .ToArray();
        var expectedSaleOrderKeys = Enumerable.Range(1, 60)
            .Where(sequence => sequence % 3 != 0)
            .Select(sequence => DemoDataStableKeyCatalog.Create("SALE-ORDER", sequence))
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();

        await using var context = fixture.CreateDbContext();
        var settlements = await context.CustomerSettlements
            .Include(settlement => settlement.Details)
            .Where(settlement => settlement.SerialNo != null
                                 && managedSerialNumbers.Contains(settlement.SerialNo))
            .OrderBy(settlement => settlement.SerialNo)
            .ToListAsync();
        var customerBillIds = settlements
            .SelectMany(settlement => settlement.Details)
            .Select(detail => detail.CustomerBillId)
            .Distinct()
            .ToArray();
        var customerBills = await context.CustomerBills
            .Include(bill => bill.SaleOrder)
            .Where(bill => customerBillIds.Contains(bill.Id))
            .OrderBy(bill => bill.SaleOrderNoSnapshot)
            .ToListAsync();
        var actualSaleOrderKeys = customerBills
            .Select(bill => bill.SaleOrder.InnerRemark!)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();
        var activeAppliedAmountsByBill = settlements
            .Where(settlement => settlement.SettlementStatus != CustomerSettlementStatus.Voided)
            .SelectMany(settlement => settlement.Details)
            .GroupBy(detail => detail.CustomerBillId)
            .ToDictionary(
                group => group.Key,
                group => NumericPrecision.RoundMoney(group.Sum(detail => detail.AppliedAmount)));

        first.CreatedByLayer.TryGetValue("customer-settlements", out var createdSettlements);
        first.ReusedByLayer.TryGetValue("customer-settlements", out var reusedSettlements);
        first.CreatedByLayer.TryGetValue("customer-settlement-details", out var createdSettlementDetails);
        first.ReusedByLayer.TryGetValue("customer-settlement-details", out var reusedSettlementDetails);

        Assert.Equal(100, settlements.Count);
        Assert.Equal(100, settlements.Select(settlement => settlement.SerialNo).Distinct().Count());
        Assert.Equal(100, createdSettlements + reusedSettlements);
        Assert.Equal(100, createdSettlementDetails + reusedSettlementDetails);
        Assert.Equal(0, second.CreatedByLayer.GetValueOrDefault("customer-settlements"));
        Assert.Equal(0, second.CreatedByLayer.GetValueOrDefault("customer-settlement-details"));
        Assert.Equal(firstDatabaseSnapshot, secondDatabaseSnapshot);
        Assert.Equal(expectedSaleOrderKeys, actualSaleOrderKeys);
        Assert.Equal(20, settlements.Count(settlement => settlement.SettlementStatus == CustomerSettlementStatus.Voided));
        Assert.Equal(
            60,
            settlements.Count(settlement => settlement.SettlementStatus == CustomerSettlementStatus.PartiallySettled));
        Assert.Equal(20, settlements.Count(settlement => settlement.SettlementStatus == CustomerSettlementStatus.Settled));
        Assert.Equal(40, customerBills.Count);
        Assert.Equal(20, customerBills.Count(bill => bill.BillStatus == CustomerBillStatus.PartiallySettled));
        Assert.Equal(20, customerBills.Count(bill => bill.BillStatus == CustomerBillStatus.Settled));
        Assert.All(settlements, settlement =>
        {
            Assert.False(string.IsNullOrWhiteSpace(settlement.SettlementNo));
            Assert.False(string.IsNullOrWhiteSpace(settlement.CustomerNameSnapshot));
            Assert.False(string.IsNullOrWhiteSpace(settlement.SerialNo));
            Assert.True(settlement.ShouldAmount > 0m);
            Assert.True(settlement.PaymentAmount > 0m);
            Assert.True(settlement.DiscountAmount > 0m);
            Assert.Equal(
                settlement.AppliedAmount,
                NumericPrecision.RoundMoney(settlement.PaymentAmount + settlement.DiscountAmount));
            Assert.True(settlement.AppliedAmount > 0m);
            Assert.True(settlement.RemainingAmount >= 0m);
            Assert.False(string.IsNullOrWhiteSpace(settlement.Remark));
            Assert.NotNull(settlement.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(settlement.CreateName));
            Assert.Single(settlement.Details);

            if (settlement.SettlementStatus == CustomerSettlementStatus.Voided)
            {
                Assert.NotNull(settlement.VoidedTime);
                Assert.NotNull(settlement.VoidedBy);
                Assert.False(string.IsNullOrWhiteSpace(settlement.VoidedByNameSnapshot));
                Assert.NotNull(settlement.UpdateBy);
                Assert.False(string.IsNullOrWhiteSpace(settlement.UpdateName));
            }
            else
            {
                Assert.Null(settlement.VoidedTime);
                Assert.Null(settlement.VoidedBy);
                Assert.Null(settlement.VoidedByNameSnapshot);
            }

            Assert.All(settlement.Details, detail =>
            {
                Assert.Equal(settlement.Id, detail.CustomerSettlementId);
                Assert.False(string.IsNullOrWhiteSpace(detail.CustomerBillNoSnapshot));
                Assert.False(string.IsNullOrWhiteSpace(detail.SaleOrderNoSnapshot));
                Assert.True(detail.ReceivableAmountSnapshot > 0m);
                Assert.True(detail.PreviousSettledAmount >= 0m);
                Assert.True(detail.PaymentAmount > 0m);
                Assert.True(detail.DiscountAmount > 0m);
                Assert.Equal(
                    detail.AppliedAmount,
                    NumericPrecision.RoundMoney(detail.PaymentAmount + detail.DiscountAmount));
                Assert.Equal(
                    detail.CurrentSettledAmount,
                    NumericPrecision.RoundMoney(detail.PreviousSettledAmount + detail.AppliedAmount));
                Assert.True(detail.RemainingAmount >= 0m);
                Assert.False(string.IsNullOrWhiteSpace(detail.Remark));
                Assert.NotNull(detail.CreateBy);
                Assert.False(string.IsNullOrWhiteSpace(detail.CreateName));
            });
        });
        Assert.All(customerBills, bill =>
        {
            Assert.True(bill.SettledAmount > 0m);
            Assert.True(bill.SettledAmount <= bill.ReceivableAmount);
            Assert.Equal(
                activeAppliedAmountsByBill.GetValueOrDefault(bill.Id),
                NumericPrecision.RoundMoney(bill.SettledAmount));
            Assert.NotNull(bill.UpdateBy);
            Assert.False(string.IsNullOrWhiteSpace(bill.UpdateName));
        });
    }

    private async Task<CustomerSettlementDatabaseSnapshot> CaptureManagedCustomerSettlementDatabaseSnapshotAsync()
    {
        var managedSerialNumbers = Enumerable.Range(1, 100)
            .Select(sequence => DemoDataStableKeyCatalog.Create("CUSTOMER-SETTLEMENT", sequence))
            .ToArray();
        await using var context = fixture.CreateDbContext();
        var settlements = await context.CustomerSettlements
            .AsNoTracking()
            .Include(settlement => settlement.Details)
            .Where(settlement => settlement.SerialNo != null
                                 && managedSerialNumbers.Contains(settlement.SerialNo))
            .OrderBy(settlement => settlement.SerialNo)
            .ToListAsync();
        var customerBillIds = settlements
            .SelectMany(settlement => settlement.Details)
            .Select(detail => detail.CustomerBillId)
            .Distinct()
            .ToArray();
        var customerBills = await context.CustomerBills
            .AsNoTracking()
            .Where(bill => customerBillIds.Contains(bill.Id))
            .OrderBy(bill => bill.SaleOrderNoSnapshot)
            .ToListAsync();

        var settlementState = string.Join(
            Environment.NewLine,
            settlements.Select(settlement =>
            {
                var detail = Assert.Single(settlement.Details);
                return string.Join(
                    '|',
                    settlement.Id,
                    settlement.SettlementNo,
                    settlement.CustomerId,
                    settlement.CustomerNameSnapshot,
                    settlement.SettlementDate.ToString("O"),
                    settlement.SerialNo,
                    settlement.ShouldAmount,
                    settlement.PaymentAmount,
                    settlement.DiscountAmount,
                    settlement.AppliedAmount,
                    settlement.RemainingAmount,
                    settlement.SettlementStatus,
                    settlement.VoidedTime?.ToString("O"),
                    settlement.VoidedBy,
                    settlement.VoidedByNameSnapshot,
                    settlement.Remark,
                    settlement.CreateTime?.ToString("O"),
                    settlement.CreateBy,
                    settlement.CreateName,
                    settlement.UpdateTime?.ToString("O"),
                    settlement.UpdateBy,
                    settlement.UpdateName,
                    settlement.Status,
                    detail.Id,
                    detail.CustomerBillId,
                    detail.CustomerBillNoSnapshot,
                    detail.SaleOrderId,
                    detail.SaleOrderNoSnapshot,
                    detail.ReceivableAmountSnapshot,
                    detail.PreviousSettledAmount,
                    detail.PaymentAmount,
                    detail.DiscountAmount,
                    detail.AppliedAmount,
                    detail.CurrentSettledAmount,
                    detail.RemainingAmount,
                    detail.Remark,
                    detail.CreateTime?.ToString("O"),
                    detail.CreateBy,
                    detail.CreateName,
                    detail.UpdateTime?.ToString("O"),
                    detail.UpdateBy,
                    detail.UpdateName,
                    detail.Status);
            }));
        var billState = string.Join(
            Environment.NewLine,
            customerBills.Select(bill => string.Join(
                '|',
                bill.Id,
                bill.BillNo,
                bill.CustomerId,
                bill.SaleOrderId,
                bill.SaleOrderNoSnapshot,
                bill.ReceivableAmount,
                bill.SettledAmount,
                bill.BillStatus,
                bill.UpdateTime?.ToString("O"),
                bill.UpdateBy,
                bill.UpdateName,
                bill.Status)));

        return new CustomerSettlementDatabaseSnapshot(settlementState, billState);
    }

    private sealed record CustomerSettlementDatabaseSnapshot(string Settlements, string Bills);

    /// <summary>
    ///     生成器必须通过供应商结算服务形成部分结款、全额结款和作废凭证，并在第二次运行时不重复核销待结单据余额。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_CreatesManagedSupplierSettlementsWithPartialSettledAndVoidedStates_AndSecondRunIsIdempotent()
    {
        var first = await fixture.GenerateDemoDataAsync();
        var firstDatabaseSnapshot = await CaptureManagedSupplierSettlementDatabaseSnapshotAsync();
        var second = await fixture.GenerateDemoDataAsync();
        var secondDatabaseSnapshot = await CaptureManagedSupplierSettlementDatabaseSnapshotAsync();

        var managedSerialNumbers = Enumerable.Range(1, 100)
            .Select(sequence => DemoDataStableKeyCatalog.Create("SUPPLIER-SETTLEMENT", sequence))
            .ToArray();
        var expectedStockInKeys = Enumerable.Range(1, 40)
            .Select(sequence => DemoDataStableKeyCatalog.Create("PURCHASE-STOCK-IN", sequence))
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();

        await using var context = fixture.CreateDbContext();
        var settlements = await context.SupplierSettlements
            .Include(settlement => settlement.Details)
            .Where(settlement => settlement.SerialNo != null
                                 && managedSerialNumbers.Contains(settlement.SerialNo))
            .OrderBy(settlement => settlement.SerialNo)
            .ToListAsync();
        var supplierBillIds = settlements
            .SelectMany(settlement => settlement.Details)
            .Select(detail => detail.SupplierBillId)
            .Distinct()
            .ToArray();
        var supplierBills = await context.SupplierBills
            .Include(bill => bill.StockInOrder)
            .Where(bill => supplierBillIds.Contains(bill.Id))
            .OrderBy(bill => bill.SourceDocumentNoSnapshot)
            .ToListAsync();
        var actualStockInKeys = supplierBills
            .Select(bill => bill.StockInOrder!.Remark![..DemoDataStableKeyCatalog.Create("PURCHASE-STOCK-IN", 1).Length])
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();
        var activeAppliedAmountsByBill = settlements
            .Where(settlement => settlement.SettlementStatus != SupplierSettlementStatus.Voided)
            .SelectMany(settlement => settlement.Details)
            .GroupBy(detail => detail.SupplierBillId)
            .ToDictionary(
                group => group.Key,
                group => NumericPrecision.RoundMoney(group.Sum(detail => detail.AppliedAmount)));

        first.CreatedByLayer.TryGetValue("supplier-settlements", out var createdSettlements);
        first.ReusedByLayer.TryGetValue("supplier-settlements", out var reusedSettlements);
        first.CreatedByLayer.TryGetValue("supplier-settlement-details", out var createdSettlementDetails);
        first.ReusedByLayer.TryGetValue("supplier-settlement-details", out var reusedSettlementDetails);

        Assert.Equal(100, settlements.Count);
        Assert.Equal(100, settlements.Select(settlement => settlement.SerialNo).Distinct().Count());
        Assert.Equal(100, createdSettlements + reusedSettlements);
        Assert.Equal(100, createdSettlementDetails + reusedSettlementDetails);
        Assert.Equal(0, second.CreatedByLayer.GetValueOrDefault("supplier-settlements"));
        Assert.Equal(0, second.CreatedByLayer.GetValueOrDefault("supplier-settlement-details"));
        Assert.Equal(firstDatabaseSnapshot, secondDatabaseSnapshot);
        Assert.Equal(expectedStockInKeys, actualStockInKeys);
        Assert.Equal(20, settlements.Count(settlement => settlement.SettlementStatus == SupplierSettlementStatus.Voided));
        Assert.Equal(
            60,
            settlements.Count(settlement => settlement.SettlementStatus == SupplierSettlementStatus.PartiallySettled));
        Assert.Equal(20, settlements.Count(settlement => settlement.SettlementStatus == SupplierSettlementStatus.Settled));
        Assert.Equal(40, supplierBills.Count);
        Assert.Equal(20, supplierBills.Count(bill => bill.BillStatus == SupplierBillStatus.PartiallySettled));
        Assert.Equal(20, supplierBills.Count(bill => bill.BillStatus == SupplierBillStatus.Settled));
        Assert.All(settlements, settlement =>
        {
            Assert.False(string.IsNullOrWhiteSpace(settlement.SettlementNo));
            Assert.False(string.IsNullOrWhiteSpace(settlement.SupplierNameSnapshot));
            Assert.False(string.IsNullOrWhiteSpace(settlement.SerialNo));
            Assert.True(settlement.ShouldAmount > 0m);
            Assert.True(settlement.PaymentAmount > 0m);
            Assert.True(settlement.DiscountAmount > 0m);
            Assert.Equal(
                settlement.AppliedAmount,
                NumericPrecision.RoundMoney(settlement.PaymentAmount + settlement.DiscountAmount));
            Assert.True(settlement.AppliedAmount > 0m);
            Assert.True(settlement.RemainingAmount >= 0m);
            Assert.False(string.IsNullOrWhiteSpace(settlement.Remark));
            Assert.NotNull(settlement.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(settlement.CreateName));
            Assert.Single(settlement.Details);

            if (settlement.SettlementStatus == SupplierSettlementStatus.Voided)
            {
                Assert.NotNull(settlement.VoidedTime);
                Assert.NotNull(settlement.VoidedBy);
                Assert.False(string.IsNullOrWhiteSpace(settlement.VoidedByNameSnapshot));
                Assert.NotNull(settlement.UpdateBy);
                Assert.False(string.IsNullOrWhiteSpace(settlement.UpdateName));
            }
            else
            {
                Assert.Null(settlement.VoidedTime);
                Assert.Null(settlement.VoidedBy);
                Assert.Null(settlement.VoidedByNameSnapshot);
            }

            Assert.All(settlement.Details, detail =>
            {
                Assert.Equal(settlement.Id, detail.SupplierSettlementId);
                Assert.False(string.IsNullOrWhiteSpace(detail.SupplierBillNoSnapshot));
                Assert.False(string.IsNullOrWhiteSpace(detail.SourceDocumentNoSnapshot));
                Assert.Equal(SupplierBillSourceType.PurchaseStockIn, detail.SourceType);
                Assert.NotNull(detail.StockInOrderId);
                Assert.Null(detail.StockOutOrderId);
                Assert.True(detail.PayableAmountSnapshot > 0m);
                Assert.True(detail.PreviousSettledAmount >= 0m);
                Assert.True(detail.PaymentAmount > 0m);
                Assert.True(detail.DiscountAmount > 0m);
                Assert.Equal(
                    detail.AppliedAmount,
                    NumericPrecision.RoundMoney(detail.PaymentAmount + detail.DiscountAmount));
                Assert.Equal(
                    detail.CurrentSettledAmount,
                    NumericPrecision.RoundMoney(detail.PreviousSettledAmount + detail.AppliedAmount));
                Assert.True(detail.RemainingAmount >= 0m);
                Assert.False(string.IsNullOrWhiteSpace(detail.Remark));
                Assert.NotNull(detail.CreateBy);
                Assert.False(string.IsNullOrWhiteSpace(detail.CreateName));
            });
        });
        Assert.All(supplierBills, bill =>
        {
            Assert.True(bill.SettledAmount > 0m);
            Assert.True(bill.SettledAmount <= bill.DocumentAmount);
            Assert.Equal(
                activeAppliedAmountsByBill.GetValueOrDefault(bill.Id),
                NumericPrecision.RoundMoney(bill.SettledAmount));
            Assert.NotNull(bill.UpdateBy);
            Assert.False(string.IsNullOrWhiteSpace(bill.UpdateName));
        });
    }

    private async Task<SupplierSettlementDatabaseSnapshot> CaptureManagedSupplierSettlementDatabaseSnapshotAsync()
    {
        var managedSerialNumbers = Enumerable.Range(1, 100)
            .Select(sequence => DemoDataStableKeyCatalog.Create("SUPPLIER-SETTLEMENT", sequence))
            .ToArray();
        await using var context = fixture.CreateDbContext();
        var settlements = await context.SupplierSettlements
            .AsNoTracking()
            .Include(settlement => settlement.Details)
            .Where(settlement => settlement.SerialNo != null
                                 && managedSerialNumbers.Contains(settlement.SerialNo))
            .OrderBy(settlement => settlement.SerialNo)
            .ToListAsync();
        var supplierBillIds = settlements
            .SelectMany(settlement => settlement.Details)
            .Select(detail => detail.SupplierBillId)
            .Distinct()
            .ToArray();
        var supplierBills = await context.SupplierBills
            .AsNoTracking()
            .Where(bill => supplierBillIds.Contains(bill.Id))
            .OrderBy(bill => bill.SourceDocumentNoSnapshot)
            .ToListAsync();

        var settlementState = string.Join(
            Environment.NewLine,
            settlements.Select(settlement =>
            {
                var detail = Assert.Single(settlement.Details);
                return string.Join(
                    '|',
                    settlement.Id,
                    settlement.SettlementNo,
                    settlement.SupplierId,
                    settlement.SupplierNameSnapshot,
                    settlement.SettlementDate.ToString("O"),
                    settlement.SerialNo,
                    settlement.ShouldAmount,
                    settlement.PaymentAmount,
                    settlement.DiscountAmount,
                    settlement.AppliedAmount,
                    settlement.RemainingAmount,
                    settlement.SettlementStatus,
                    settlement.VoidedTime?.ToString("O"),
                    settlement.VoidedBy,
                    settlement.VoidedByNameSnapshot,
                    settlement.Remark,
                    settlement.CreateTime?.ToString("O"),
                    settlement.CreateBy,
                    settlement.CreateName,
                    settlement.UpdateTime?.ToString("O"),
                    settlement.UpdateBy,
                    settlement.UpdateName,
                    settlement.Status,
                    detail.Id,
                    detail.SupplierBillId,
                    detail.SupplierBillNoSnapshot,
                    detail.SourceType,
                    detail.SourceDocumentNoSnapshot,
                    detail.StockInOrderId,
                    detail.StockOutOrderId,
                    detail.PayableAmountSnapshot,
                    detail.PreviousSettledAmount,
                    detail.PaymentAmount,
                    detail.DiscountAmount,
                    detail.AppliedAmount,
                    detail.CurrentSettledAmount,
                    detail.RemainingAmount,
                    detail.Remark,
                    detail.CreateTime?.ToString("O"),
                    detail.CreateBy,
                    detail.CreateName,
                    detail.UpdateTime?.ToString("O"),
                    detail.UpdateBy,
                    detail.UpdateName,
                    detail.Status);
            }));
        var billState = string.Join(
            Environment.NewLine,
            supplierBills.Select(bill => string.Join(
                '|',
                bill.Id,
                bill.BillNo,
                bill.SupplierId,
                bill.SourceType,
                bill.StockInOrderId,
                bill.SourceDocumentNoSnapshot,
                bill.DocumentAmount,
                bill.PayableAmount,
                bill.SettledAmount,
                bill.BillStatus,
                bill.UpdateTime?.ToString("O"),
                bill.UpdateBy,
                bill.UpdateName,
                bill.Status)));

        return new SupplierSettlementDatabaseSnapshot(settlementState, billState);
    }

    private sealed record SupplierSettlementDatabaseSnapshot(string Settlements, string Bills);

    /// <summary>
    ///     生成器必须基于受管库存批次形成盘盈、盘亏和零差异盘点，并在重复运行时保持盘点快照与调整流水不变。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_CreatesManagedStocktakingOrdersWithAdjustments_AndSecondRunIsIdempotent()
    {
        var expectedRemarks = Enumerable.Range(1, 60)
            .Select(CreateStocktakingRemark)
            .ToArray();

        var first = await fixture.GenerateDemoDataAsync();
        var firstDatabaseSnapshot = await CaptureManagedStocktakingDatabaseSnapshotAsync(expectedRemarks);
        var second = await fixture.GenerateDemoDataAsync();
        var secondDatabaseSnapshot = await CaptureManagedStocktakingDatabaseSnapshotAsync(expectedRemarks);

        await using var context = fixture.CreateDbContext();
        var orders = await context.StocktakingOrders
            .AsNoTracking()
            .Include(order => order.Details)
            .Where(order => order.Remark != null && expectedRemarks.Contains(order.Remark))
            .OrderBy(order => order.Remark)
            .ToListAsync();
        var orderIds = orders.Select(order => order.Id).ToArray();
        var ledgers = await context.StockLedgers
            .AsNoTracking()
            .Where(ledger => ledger.SourceType == StockLedgerSourceType.Stocktaking
                             && orderIds.Contains(ledger.SourceOrderId))
            .OrderBy(ledger => ledger.SourceOrderId)
            .ThenBy(ledger => ledger.SourceDetailId)
            .ToListAsync();

        Assert.Equal(60, orders.Count);
        Assert.Equal(120, orders.Sum(order => order.Details.Count));
        Assert.Equal(40, orders.Count(order => order.BusinessStatus == StockDocumentStatus.Audited));
        Assert.Equal(20, orders.Count(order => order.BusinessStatus == StockDocumentStatus.Draft));
        Assert.Equal(40, orders.SelectMany(order => order.Details).Count(detail => detail.DifferenceQuantity > 0m));
        Assert.Equal(40, orders.SelectMany(order => order.Details).Count(detail => detail.DifferenceQuantity < 0m));
        Assert.Equal(40, orders.SelectMany(order => order.Details).Count(detail => detail.DifferenceQuantity == 0m));
        Assert.Equal(80, ledgers.Count);
        Assert.Equal(40, ledgers.Count(ledger => ledger.Direction == StockLedgerDirection.Increase));
        Assert.Equal(40, ledgers.Count(ledger => ledger.Direction == StockLedgerDirection.Decrease));
        Assert.Equal(
            60,
            first.CreatedByLayer.GetValueOrDefault("stocktaking-orders")
            + first.ReusedByLayer.GetValueOrDefault("stocktaking-orders"));
        Assert.Equal(
            120,
            first.CreatedByLayer.GetValueOrDefault("stocktaking-details")
            + first.ReusedByLayer.GetValueOrDefault("stocktaking-details"));
        Assert.Equal(
            80,
            first.CreatedByLayer.GetValueOrDefault("stocktaking-ledgers")
            + first.ReusedByLayer.GetValueOrDefault("stocktaking-ledgers"));
        Assert.Equal(0, second.CreatedByLayer.GetValueOrDefault("stocktaking-orders"));
        Assert.Equal(60, second.ReusedByLayer.GetValueOrDefault("stocktaking-orders"));
        Assert.Equal(0, second.CreatedByLayer.GetValueOrDefault("stocktaking-details"));
        Assert.Equal(120, second.ReusedByLayer.GetValueOrDefault("stocktaking-details"));
        Assert.Equal(0, second.CreatedByLayer.GetValueOrDefault("stocktaking-ledgers"));
        Assert.Equal(80, second.ReusedByLayer.GetValueOrDefault("stocktaking-ledgers"));
        Assert.Equal(firstDatabaseSnapshot, secondDatabaseSnapshot);

        Assert.All(orders, order =>
        {
            Assert.False(string.IsNullOrWhiteSpace(order.StocktakingNo));
            Assert.False(string.IsNullOrWhiteSpace(order.WareNameSnapshot));
            Assert.NotNull(order.CreateTime);
            Assert.NotNull(order.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(order.CreateName));
            Assert.Equal(Status.Enable, order.Status);
            Assert.Equal(2, order.Details.Count);
            Assert.Equal(2, order.Details.Select(detail => detail.StockBatchId).Distinct().Count());
            Assert.Equal(order.TotalBookQuantity, order.Details.Sum(detail => detail.BookQuantity));
            Assert.Equal(order.TotalActualQuantity, order.Details.Sum(detail => detail.ActualQuantity));
            Assert.Equal(order.TotalDifferenceQuantity, order.Details.Sum(detail => detail.DifferenceQuantity));
            Assert.All(order.Details, detail =>
            {
                Assert.False(string.IsNullOrWhiteSpace(detail.GoodsNameSnapshot));
                Assert.False(string.IsNullOrWhiteSpace(detail.GoodsCodeSnapshot));
                Assert.False(string.IsNullOrWhiteSpace(detail.BatchNoSnapshot));
                Assert.False(string.IsNullOrWhiteSpace(detail.BaseUnitNameSnapshot));
                Assert.False(string.IsNullOrWhiteSpace(detail.Remark));
                Assert.True(detail.BookQuantity >= 0m);
                Assert.True(detail.ActualQuantity >= 0m);
                Assert.True(detail.UnitCost >= 0m);
                Assert.NotNull(detail.CreateTime);
                Assert.NotNull(detail.CreateBy);
                Assert.False(string.IsNullOrWhiteSpace(detail.CreateName));
                Assert.Equal(Status.Enable, detail.Status);
            });

            if (order.BusinessStatus == StockDocumentStatus.Audited)
            {
                Assert.True(order.IsAdjustmentApplied);
                Assert.NotNull(order.AdjustmentTime);
                Assert.NotNull(order.AuditUserId);
                Assert.False(string.IsNullOrWhiteSpace(order.AuditUserNameSnapshot));
                Assert.NotNull(order.AuditTime);
                Assert.Equal(2, ledgers.Count(ledger => ledger.SourceOrderId == order.Id));
            }
            else
            {
                Assert.False(order.IsAdjustmentApplied);
                Assert.Null(order.AdjustmentTime);
                Assert.Null(order.AuditUserId);
                Assert.Null(order.AuditUserNameSnapshot);
                Assert.Null(order.AuditTime);
                Assert.DoesNotContain(ledgers, ledger => ledger.SourceOrderId == order.Id);
            }
        });
        Assert.All(ledgers, ledger =>
        {
            Assert.Contains(ledger.SourceOrderId, orderIds);
            Assert.NotEqual(Guid.Empty, ledger.SourceDetailId);
            Assert.True(ledger.ChangeQuantity > 0m);
            Assert.True(ledger.BalanceQuantity >= 0m);
            Assert.True(ledger.TotalCost >= 0m);
            Assert.False(string.IsNullOrWhiteSpace(ledger.Remark));
            Assert.NotNull(ledger.CreateTime);
            Assert.NotNull(ledger.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(ledger.CreateName));
            Assert.Equal(Status.Enable, ledger.Status);
        });
    }

    private async Task<StocktakingDatabaseSnapshot> CaptureManagedStocktakingDatabaseSnapshotAsync(
        string[] expectedRemarks)
    {
        await using var context = fixture.CreateDbContext();
        var orders = await context.StocktakingOrders
            .AsNoTracking()
            .Include(order => order.Details)
            .Where(order => order.Remark != null && expectedRemarks.Contains(order.Remark))
            .OrderBy(order => order.Remark)
            .ToListAsync();
        var orderIds = orders.Select(order => order.Id).ToArray();
        var ledgers = await context.StockLedgers
            .AsNoTracking()
            .Where(ledger => ledger.SourceType == StockLedgerSourceType.Stocktaking
                             && orderIds.Contains(ledger.SourceOrderId))
            .OrderBy(ledger => ledger.SourceOrderId)
            .ThenBy(ledger => ledger.SourceDetailId)
            .ToListAsync();

        var orderState = string.Join(
            Environment.NewLine,
            orders.Select(order => string.Join(
                '|',
                order.Id,
                order.StocktakingNo,
                order.BusinessStatus,
                order.WareId,
                order.StocktakingTime.ToString("O"),
                order.TotalBookQuantity,
                order.TotalActualQuantity,
                order.TotalDifferenceQuantity,
                order.IsAdjustmentApplied,
                order.AdjustmentTime?.ToString("O"),
                order.AuditUserId,
                order.AuditUserNameSnapshot,
                order.AuditTime?.ToString("O"),
                order.Remark,
                order.CreateTime?.ToString("O"),
                order.CreateBy,
                order.CreateName,
                order.UpdateTime?.ToString("O"),
                order.UpdateBy,
                order.UpdateName,
                string.Join(',', order.Details.OrderBy(detail => detail.StockBatchId).Select(detail => string.Join(
                    ':',
                    detail.Id,
                    detail.StockBatchId,
                    detail.BookQuantity,
                    detail.ActualQuantity,
                    detail.DifferenceQuantity,
                    detail.UnitCost,
                    detail.DifferenceAmount,
                    detail.Remark,
                    detail.CreateTime?.ToString("O"),
                    detail.CreateBy,
                    detail.CreateName,
                    detail.UpdateTime?.ToString("O"),
                    detail.UpdateBy,
                    detail.UpdateName))))));
        var ledgerState = string.Join(
            Environment.NewLine,
            ledgers.Select(ledger => string.Join(
                '|',
                ledger.Id,
                ledger.SourceOrderId,
                ledger.SourceDetailId,
                ledger.StockBatchId,
                ledger.Direction,
                ledger.ChangeQuantity,
                ledger.BalanceQuantity,
                ledger.UnitCost,
                ledger.TotalCost,
                ledger.OccurredTime.ToString("O"),
                ledger.Remark,
                ledger.CreateTime?.ToString("O"),
                ledger.CreateBy,
                ledger.CreateName)));
        return new StocktakingDatabaseSnapshot(orderState, ledgerState);
    }

    private static string CreateStocktakingRemark(int sequence)
    {
        var stableKey = DemoDataStableKeyCatalog.Create("STOCKTAKING", sequence);
        return $"{stableKey} 华东联调库存盘点{sequence:D2}：核对受管采购批次账实数量与调整流水。";
    }

    private sealed record StocktakingDatabaseSnapshot(string Orders, string Ledgers);

    /// <summary>
    ///     生成器必须从受管采购入库形成检测报告及附件，并通过真实采购批次销售出库生成可重复复用的溯源记录。
    /// </summary>
    [Fact]
    public async Task GenerateAsync_CreatesManagedInspectionReportsAttachmentsAndTraceRecords_AndSecondRunIsIdempotent()
    {
        var first = await fixture.GenerateDemoDataAsync();
        var firstDatabaseSnapshot = await CaptureManagedTraceabilityDatabaseSnapshotAsync();
        var second = await fixture.GenerateDemoDataAsync();
        var secondDatabaseSnapshot = await CaptureManagedTraceabilityDatabaseSnapshotAsync();

        var expectedReportRemarks = Enumerable.Range(1, 50)
            .Select(CreateInspectionReportRemark)
            .ToArray();
        var expectedTraceOrderKeys = Enumerable.Range(1, 20)
            .Select(sequence => DemoDataStableKeyCatalog.Create("TRACE-SALE-ORDER", sequence))
            .ToArray();
        var expectedTraceStockOutRemarks = Enumerable.Range(1, 20)
            .Select(CreateTraceStockOutRemark)
            .ToArray();
        var expectedTraceRemarks = Enumerable.Range(1, 20)
            .Select(CreateTraceRecordRemark)
            .ToArray();

        await using var context = fixture.CreateDbContext();
        var reports = await context.InspectionReports
            .AsNoTracking()
            .Include(report => report.StockInOrder)
            .Include(report => report.Goods)
            .Include(report => report.Attachments)
            .Where(report => report.Remark != null && expectedReportRemarks.Contains(report.Remark))
            .OrderBy(report => report.Remark)
            .ToListAsync();
        var traceOrders = await context.SaleOrders
            .AsNoTracking()
            .Include(order => order.Details)
            .Where(order => order.InnerRemark != null && expectedTraceOrderKeys.Contains(order.InnerRemark))
            .OrderBy(order => order.InnerRemark)
            .ToListAsync();
        var traceOrderIds = traceOrders.Select(order => order.Id).ToArray();
        var stockOuts = await context.StockOutOrders
            .AsNoTracking()
            .Include(order => order.Details)
            .Where(order => order.Remark != null && expectedTraceStockOutRemarks.Contains(order.Remark))
            .OrderBy(order => order.Remark)
            .ToListAsync();
        var traces = await context.TraceRecords
            .AsNoTracking()
            .Include(trace => trace.StockInDetail)
            .ThenInclude(detail => detail!.StockBatch)
            .Include(trace => trace.InspectionReport)
            .ThenInclude(report => report!.Goods)
            .Where(trace => traceOrderIds.Contains(trace.SaleOrderId))
            .OrderBy(trace => trace.Remark)
            .ToListAsync();

        Assert.Equal(50, reports.Count);
        Assert.Equal(100, reports.Sum(report => report.Goods.Count));
        Assert.Equal(100, reports.Sum(report => report.Attachments.Count));
        Assert.Equal(40, reports.Count(report => report.Conclusion == InspectionConclusion.Qualified));
        Assert.Equal(5, reports.Count(report => report.Conclusion == InspectionConclusion.Pending));
        Assert.Equal(5, reports.Count(report => report.Conclusion == InspectionConclusion.Unqualified));
        Assert.Equal(20, traceOrders.Count);
        Assert.Equal(20, traceOrders.Sum(order => order.Details.Count));
        Assert.Equal(20, stockOuts.Count);
        Assert.Equal(20, stockOuts.Sum(order => order.Details.Count));
        Assert.Equal(20, traces.Count);
        Assert.Equal(expectedTraceRemarks, traces.Select(trace => trace.Remark).ToArray());
        Assert.Equal(50, first.CreatedByLayer.GetValueOrDefault("inspection-reports")
                         + first.ReusedByLayer.GetValueOrDefault("inspection-reports"));
        Assert.Equal(100, first.CreatedByLayer.GetValueOrDefault("inspection-report-goods")
                         + first.ReusedByLayer.GetValueOrDefault("inspection-report-goods"));
        Assert.Equal(100, first.CreatedByLayer.GetValueOrDefault("inspection-attachments")
                         + first.ReusedByLayer.GetValueOrDefault("inspection-attachments"));
        Assert.Equal(20, first.CreatedByLayer.GetValueOrDefault("trace-records")
                         + first.ReusedByLayer.GetValueOrDefault("trace-records"));
        Assert.Equal(0, second.CreatedByLayer.GetValueOrDefault("inspection-reports"));
        Assert.Equal(0, second.CreatedByLayer.GetValueOrDefault("inspection-report-goods"));
        Assert.Equal(0, second.CreatedByLayer.GetValueOrDefault("inspection-attachments"));
        Assert.Equal(0, second.CreatedByLayer.GetValueOrDefault("trace-sale-orders"));
        Assert.Equal(0, second.CreatedByLayer.GetValueOrDefault("trace-stock-outs"));
        Assert.Equal(0, second.CreatedByLayer.GetValueOrDefault("trace-records"));
        Assert.Equal(firstDatabaseSnapshot, secondDatabaseSnapshot);

        Assert.All(reports, report =>
        {
            Assert.Equal(StockInOrderType.Purchase, report.StockInOrder.OrderType);
            Assert.Equal(StockDocumentStatus.Audited, report.StockInOrder.BusinessStatus);
            Assert.False(string.IsNullOrWhiteSpace(report.InspectionNo));
            Assert.False(string.IsNullOrWhiteSpace(report.InspectionOrg));
            Assert.NotNull(report.SampleTime);
            Assert.True(report.SampleTime < report.InspectTime);
            Assert.NotNull(report.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(report.CreateName));
            Assert.Equal(2, report.Goods.Count);
            Assert.Equal(2, report.Attachments.Count);
            Assert.All(report.Goods, goods =>
            {
                Assert.True(goods.SampleQuantity > 0m);
                Assert.False(string.IsNullOrWhiteSpace(goods.GoodsNameSnapshot));
                Assert.False(string.IsNullOrWhiteSpace(goods.GoodsCodeSnapshot));
                Assert.False(string.IsNullOrWhiteSpace(goods.GoodsTypeNameSnapshot));
                Assert.False(string.IsNullOrWhiteSpace(goods.GoodsUnitNameSnapshot));
                Assert.False(string.IsNullOrWhiteSpace(goods.BatchNoSnapshot));
                Assert.False(string.IsNullOrWhiteSpace(goods.Remark));
                Assert.NotNull(goods.CreateBy);
                Assert.False(string.IsNullOrWhiteSpace(goods.CreateName));
            });
            Assert.Contains(report.Attachments, attachment => attachment.AttachmentType == InspectionAttachmentType.Report);
            Assert.Contains(report.Attachments, attachment => attachment.AttachmentType == InspectionAttachmentType.Image);
            Assert.All(report.Attachments, attachment =>
            {
                Assert.False(string.IsNullOrWhiteSpace(attachment.FileName));
                Assert.False(string.IsNullOrWhiteSpace(attachment.FileUrl));
                Assert.True(attachment.FileSize > 0);
                Assert.NotNull(attachment.CreateBy);
                Assert.False(string.IsNullOrWhiteSpace(attachment.CreateName));
            });
        });

        var stockOutBySaleOrderId = stockOuts.ToDictionary(order => order.SaleOrderId!.Value);
        Assert.All(traces, trace =>
        {
            Assert.False(string.IsNullOrWhiteSpace(trace.TraceNo));
            Assert.NotNull(trace.StockInDetailId);
            Assert.NotNull(trace.StockInDetail);
            Assert.NotNull(trace.InspectionReportId);
            Assert.NotNull(trace.InspectionReport);
            Assert.False(string.IsNullOrWhiteSpace(trace.SupplierNameSnapshot));
            Assert.False(string.IsNullOrWhiteSpace(trace.WareNameSnapshot));
            Assert.False(string.IsNullOrWhiteSpace(trace.BatchNoSnapshot));
            Assert.NotNull(trace.CreateBy);
            Assert.False(string.IsNullOrWhiteSpace(trace.CreateName));
            Assert.Contains(
                trace.InspectionReport!.Goods,
                goods => goods.StockInDetailId == trace.StockInDetailId);
            var stockOut = stockOutBySaleOrderId[trace.SaleOrderId];
            var stockOutDetail = Assert.Single(stockOut.Details);
            Assert.Equal(stockOutDetail.StockBatchId, trace.StockInDetail!.StockBatchId);
        });
    }

    private async Task<TraceabilityDatabaseSnapshot> CaptureManagedTraceabilityDatabaseSnapshotAsync()
    {
        var reportRemarks = Enumerable.Range(1, 50)
            .Select(CreateInspectionReportRemark)
            .ToArray();
        var traceOrderKeys = Enumerable.Range(1, 20)
            .Select(sequence => DemoDataStableKeyCatalog.Create("TRACE-SALE-ORDER", sequence))
            .ToArray();
        await using var context = fixture.CreateDbContext();
        var reports = await context.InspectionReports
            .AsNoTracking()
            .Include(report => report.Goods)
            .Include(report => report.Attachments)
            .Where(report => report.Remark != null && reportRemarks.Contains(report.Remark))
            .OrderBy(report => report.Remark)
            .ToListAsync();
        var traceOrderIds = await context.SaleOrders
            .AsNoTracking()
            .Where(order => order.InnerRemark != null && traceOrderKeys.Contains(order.InnerRemark))
            .OrderBy(order => order.InnerRemark)
            .Select(order => order.Id)
            .ToArrayAsync();
        var traces = await context.TraceRecords
            .AsNoTracking()
            .Where(trace => traceOrderIds.Contains(trace.SaleOrderId))
            .OrderBy(trace => trace.Remark)
            .ToListAsync();

        var reportState = string.Join(
            Environment.NewLine,
            reports.Select(report => string.Join(
                '|',
                report.Id,
                report.InspectionNo,
                report.StockInOrderId,
                report.InspectionOrg,
                report.SampleTime?.ToString("O"),
                report.InspectTime.ToString("O"),
                report.Conclusion,
                report.Remark,
                report.CreateTime?.ToString("O"),
                report.CreateBy,
                report.CreateName,
                string.Join(',', report.Goods.OrderBy(goods => goods.StockInDetailId).Select(goods => goods.Id)),
                string.Join(',', report.Attachments.OrderBy(attachment => attachment.Sort).Select(attachment => attachment.Id)))));
        var traceState = string.Join(
            Environment.NewLine,
            traces.Select(trace => string.Join(
                '|',
                trace.Id,
                trace.TraceNo,
                trace.SaleOrderId,
                trace.SaleOrderDetailId,
                trace.StockInDetailId,
                trace.InspectionReportId,
                trace.BatchNoSnapshot,
                trace.Remark,
                trace.CreateTime?.ToString("O"),
                trace.CreateBy,
                trace.CreateName)));

        return new TraceabilityDatabaseSnapshot(reportState, traceState);
    }

    private static string CreateInspectionReportRemark(int sequence)
    {
        var stableKey = DemoDataStableKeyCatalog.Create("INSPECTION-REPORT", sequence);
        return $"{stableKey} 华东联调检测报告{sequence:D2}：记录受管采购入库商品抽检结论与附件快照。";
    }

    private static string CreateTraceStockOutRemark(int sequence)
    {
        var stableKey = DemoDataStableKeyCatalog.Create("TRACE-STOCK-OUT", sequence);
        return $"{stableKey} 华东联调溯源销售出库{sequence:D2}：从已检测采购批次出库以形成真实溯源来源。";
    }

    private static string CreateTraceRecordRemark(int sequence)
    {
        var stableKey = DemoDataStableKeyCatalog.Create("TRACE-RECORD", sequence);
        return $"{stableKey} 华东联调溯源记录{sequence:D2}：串联销售商品、采购批次与检测报告。";
    }

    private sealed record TraceabilityDatabaseSnapshot(string Reports, string Traces);
}
