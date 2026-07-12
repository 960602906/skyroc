using Domain.Entities.Customers;
using Domain.Entities.Goods;
using GoodsEntity = Domain.Entities.Goods.Goods;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SkyRoc.Tests.Testing.PostgreSql;
using Xunit;

namespace SkyRoc.Tests.PostgreSql;

/// <summary>
///     在项目专用 PostgreSQL 测试库中验证宿主隔离、事务回滚、批次清理和基础质量报告。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class PostgreSqlInfrastructureTests(PostgreSqlTestFixture fixture)
{
    /// <summary>
    ///     Web 测试宿主解析出的 DbContext 必须仍指向已确认的白名单 PostgreSQL 数据库。
    /// </summary>
    [Fact]
    public void PostgreSqlTestHost_UsesAllowlistedDatabase()
    {
        using var factory = fixture.CreateWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Assert.Equal(fixture.DatabaseName, context.Database.GetDbConnection().Database);
        Assert.Contains("Npgsql", context.Database.ProviderName, StringComparison.Ordinal);
    }

    /// <summary>
    ///     单连接探针在业务异常后必须回滚，外部新连接不得观察到探针记录。
    /// </summary>
    [Fact]
    public async Task ExecuteInRollbackTransactionAsync_RollsBackProbeAfterBusinessFailure()
    {
        var batch = TestBatchContext.Create();
        var companyId = Guid.NewGuid();
        var companyCode = $"{batch.Id}-TX";

        var exception = await Assert.ThrowsAsync<ProbeBusinessException>(() =>
            fixture.ExecuteInRollbackTransactionAsync(async context =>
            {
                await context.Companies.AddAsync(CreateCompany(companyId, companyCode, "事务回滚探针单位"));
                await context.SaveChangesAsync();
                Assert.True(await context.Companies.AnyAsync(company => company.Id == companyId));
                throw new ProbeBusinessException();
            }));

        Assert.NotNull(exception);
        await using var verificationContext = fixture.CreateDbContext();
        Assert.False(await verificationContext.Companies.AnyAsync(company => company.Id == companyId));
    }

    /// <summary>
    ///     跨连接创建的商品父子记录应按登记逆序精确清理，重复清理不修改既有联调数据。
    /// </summary>
    [Fact]
    public async Task BatchCleanup_RemovesOnlyCurrentBatchInReverseOrderAndIsIdempotent()
    {
        var baseline = await CaptureGoodsBaselineAsync();
        var batch = TestBatchContext.Create();
        var registry = new BatchCleanupRegistry(batch);
        var goodsTypeId = Guid.NewGuid();
        var goodsId = Guid.NewGuid();
        var goodsTypeCode = $"{batch.Id}-TYPE";
        var goodsCode = $"{batch.Id}-GOODS";

        registry.Register<GoodsType>(goodsTypeId, nameof(GoodsType.Code), goodsTypeCode);
        registry.Register<GoodsEntity>(goodsId, nameof(GoodsEntity.Code), goodsCode);

        await using (var creationContext = fixture.CreateDbContext())
        {
            await creationContext.GoodsTypes.AddAsync(CreateGoodsType(goodsTypeId, goodsTypeCode));
            await creationContext.Goods.AddAsync(CreateGoods(goodsId, goodsTypeId, goodsCode));
            await creationContext.SaveChangesAsync();
        }

        await using (var committedContext = fixture.CreateDbContext())
        {
            Assert.True(await committedContext.GoodsTypes.AnyAsync(item => item.Id == goodsTypeId));
            Assert.True(await committedContext.Goods.AnyAsync(item => item.Id == goodsId));
        }

        await fixture.CleanupBatchAsync(registry);
        await fixture.CleanupBatchAsync(registry);

        await using var verificationContext = fixture.CreateDbContext();
        Assert.False(await verificationContext.GoodsTypes.AnyAsync(item => item.Id == goodsTypeId));
        Assert.False(await verificationContext.Goods.AnyAsync(item => item.Id == goodsId));
        Assert.Equal(baseline, await CaptureGoodsBaselineAsync());
    }

    /// <summary>
    ///     登记归属值与数据库实际值不一致时，清理器必须拒绝删除并保留证据记录。
    /// </summary>
    [Fact]
    public async Task BatchCleanup_RejectsOwnershipMismatchWithoutDeletingRecord()
    {
        var batch = TestBatchContext.Create();
        var companyId = Guid.NewGuid();
        var actualCode = $"{batch.Id}-ACTUAL";
        var incorrectRegistry = new BatchCleanupRegistry(batch);
        incorrectRegistry.Register<Company>(companyId, nameof(Company.Code), $"{batch.Id}-OTHER");

        await using (var creationContext = fixture.CreateDbContext())
        {
            await creationContext.Companies.AddAsync(CreateCompany(companyId, actualCode, "归属保护探针单位"));
            await creationContext.SaveChangesAsync();
        }

        try
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                fixture.CleanupBatchAsync(incorrectRegistry));
            Assert.Contains("ownership", exception.Message, StringComparison.OrdinalIgnoreCase);

            await using var verificationContext = fixture.CreateDbContext();
            Assert.True(await verificationContext.Companies.AnyAsync(company => company.Id == companyId));
        }
        finally
        {
            var correctRegistry = new BatchCleanupRegistry(batch);
            correctRegistry.Register<Company>(companyId, nameof(Company.Code), actualCode);
            await fixture.CleanupBatchAsync(correctRegistry);
        }
    }

    /// <summary>
    ///     仍被外键引用的父记录不得被清理；补齐子记录登记后才允许逆序删除。
    /// </summary>
    [Fact]
    public async Task BatchCleanup_RejectsReferencedParentAndRollsBackAttempt()
    {
        var batch = TestBatchContext.Create();
        var goodsTypeId = Guid.NewGuid();
        var goodsId = Guid.NewGuid();
        var goodsTypeCode = $"{batch.Id}-FKTYPE";
        var goodsCode = $"{batch.Id}-FKGOODS";

        await using (var creationContext = fixture.CreateDbContext())
        {
            await creationContext.GoodsTypes.AddAsync(CreateGoodsType(goodsTypeId, goodsTypeCode));
            await creationContext.Goods.AddAsync(CreateGoods(goodsId, goodsTypeId, goodsCode));
            await creationContext.SaveChangesAsync();
        }

        var incompleteRegistry = new BatchCleanupRegistry(batch);
        incompleteRegistry.Register<GoodsType>(goodsTypeId, nameof(GoodsType.Code), goodsTypeCode);

        try
        {
            await Assert.ThrowsAsync<PostgresException>(() => fixture.CleanupBatchAsync(incompleteRegistry));
            await using var verificationContext = fixture.CreateDbContext();
            Assert.True(await verificationContext.GoodsTypes.AnyAsync(item => item.Id == goodsTypeId));
            Assert.True(await verificationContext.Goods.AnyAsync(item => item.Id == goodsId));
        }
        finally
        {
            var completeRegistry = new BatchCleanupRegistry(batch);
            completeRegistry.Register<GoodsType>(goodsTypeId, nameof(GoodsType.Code), goodsTypeCode);
            completeRegistry.Register<GoodsEntity>(goodsId, nameof(GoodsEntity.Code), goodsCode);
            await fixture.CleanupBatchAsync(completeRegistry);
        }
    }

    /// <summary>
    ///     基础质量扫描必须覆盖所有要求栏目、确认无临时残留并生成两种报告文件。
    /// </summary>
    [Fact]
    public async Task GenerateQualityReportAsync_WritesCompleteReportsWithoutTemporaryResidue()
    {
        var batch = TestBatchContext.Create();

        var result = await fixture.GenerateQualityReportAsync(batch.Id);

        Assert.NotEmpty(result.Report.TableCounts);
        Assert.NotEmpty(result.Report.FieldFillRates);
        Assert.NotEmpty(result.Report.StatusDistributions);
        Assert.Empty(result.Report.OrphanForeignKeys);
        Assert.Empty(result.Report.TemporaryResidues);
        Assert.True(result.Report.BusinessConsistencyChecks["migrationHistoryMatchesModel"]);
        Assert.True(result.Report.BusinessConsistencyChecks["temporaryBatchResidueIsZero"]);
        Assert.True(File.Exists(result.Paths.JsonPath));
        Assert.True(File.Exists(result.Paths.MarkdownPath));
    }

    private async Task<IReadOnlyList<string>> CaptureGoodsBaselineAsync()
    {
        await using var context = fixture.CreateDbContext();
        var goodsTypes = await context.GoodsTypes.AsNoTracking()
            .Where(item => !item.Code.StartsWith(TestBatchContext.Prefix))
            .OrderBy(item => item.Id)
            .Select(item => $"type:{item.Id}:{item.UpdateTime:O}")
            .ToListAsync();
        var goods = await context.Goods.AsNoTracking()
            .Where(item => !item.Code.StartsWith(TestBatchContext.Prefix))
            .OrderBy(item => item.Id)
            .Select(item => $"goods:{item.Id}:{item.UpdateTime:O}")
            .ToListAsync();
        return [.. goodsTypes, .. goods];
    }

    private static Company CreateCompany(Guid id, string code, string name)
    {
        return new Company
        {
            Id = id,
            Code = code,
            Name = name,
            ContactName = "安全验证联系人",
            ContactPhone = "13900001234",
            Address = "上海市浦东新区供应链联调中心 18 号",
            Remark = "用于验证自动业务测试事务回滚与精确清理边界"
        };
    }

    private static GoodsType CreateGoodsType(Guid id, string code)
    {
        return new GoodsType
        {
            Id = id,
            Code = code,
            Name = $"安全隔离分类-{code[^8..]}",
            ImageUrl = "https://assets.skyroc.example/quality/vegetable-category.png",
            TaxCategoryCode = "1010101990000000000",
            TaxCategoryName = "其他新鲜蔬菜",
            InvoiceGoodsShortName = "蔬菜",
            DefaultTaxRate = 9m,
            IsTaxExempt = false,
            TaxPolicyBasis = "农产品增值税分类管理规则",
            Sort = 900,
            Remark = "用于验证父子临时数据的外键逆序清理"
        };
    }

    private static GoodsEntity CreateGoods(Guid id, Guid goodsTypeId, string code)
    {
        return new GoodsEntity
        {
            Id = id,
            GoodsTypeId = goodsTypeId,
            Code = code,
            Name = $"安全隔离商品-{code[^8..]}",
            Spec = "一级 500 克装",
            Brand = "青浦鲜选",
            Origin = "上海青浦",
            Description = "用于验证跨连接批次登记、外键约束和精确清理的鲜蔬商品",
            TaxRate = 9m,
            IsOnSale = true,
            Remark = "仅属于当前自动测试批次，验证结束后按主键精确清理"
        };
    }

    private sealed class ProbeBusinessException : Exception;
}
