using System.Text;
using Application.DTOs.ImportExport;
using Application.Exceptions;
using Application.Services;
using Domain.Entities.Goods;
using Domain.Entities.ImportExport;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace SkyRoc.Tests.ImportExport;

/// <summary>
/// 导入导出任务服务回归测试。
/// </summary>
public class ImportExportJobServiceTests
{
    [Fact]
    public async Task DownloadTemplate_ReturnsGoodsHeadersAndExampleRow()
    {
        await using var context = CreateDbContext();
        var service = CreateService(context);

        var result = await service.DownloadTemplateAsync(ImportExportJobType.Goods);
        var text = Encoding.UTF8.GetString(result.Content);

        Assert.Equal("goods-import-template.csv", result.FileName);
        Assert.StartsWith("Name,Code,GoodsTypeId,Spec,Brand,Origin,TaxRate,IsOnSale,Remark", text);
        Assert.Contains("示例商品", text);
    }

    [Fact]
    public async Task ImportGoods_ValidRows_CreateGoodsAndCompleteJob()
    {
        await using var context = CreateDbContext();
        var type = new GoodsType { Id = Guid.NewGuid(), Name = "蔬菜", Code = "VEGETABLE" };
        await context.GoodsTypes.AddAsync(type);
        await context.SaveChangesAsync();
        var service = CreateService(context);
        var csv = $"Name,Code,GoodsTypeId,Spec,Brand,Origin,TaxRate,IsOnSale,Remark\n\"圣女,番茄\",TOMATO,{type.Id},,,,6.0000,true,测试导入\n";

        var result = await service.ImportAsync(
            ImportExportJobType.Goods,
            "goods.csv",
            new MemoryStream(Encoding.UTF8.GetBytes(csv)));

        var goods = await context.Goods.SingleAsync();
        Assert.Equal("圣女,番茄", goods.Name);
        Assert.Equal("TOMATO", goods.Code);
        Assert.Equal(ImportExportJobStatus.Succeeded, result.JobStatus);
        Assert.Equal(1, result.TotalRows);
        Assert.Equal(1, result.SuccessRows);
        Assert.Equal(0, result.FailureRows);
    }

    [Fact]
    public async Task ImportGoods_InvalidRows_RecordFailureWithoutWritingGoods()
    {
        await using var context = CreateDbContext();
        var type = new GoodsType { Id = Guid.NewGuid(), Name = "水果", Code = "FRUIT" };
        await context.GoodsTypes.AddAsync(type);
        await context.SaveChangesAsync();
        var service = CreateService(context);
        var csv = $"Name,Code,GoodsTypeId,Spec,Brand,Origin,TaxRate,IsOnSale,Remark\n苹果,APPLE,{type.Id},,,,6,true,\n梨,APPLE,{type.Id},,,,6,true,\n";

        var result = await service.ImportAsync(
            ImportExportJobType.Goods,
            "goods.csv",
            new MemoryStream(Encoding.UTF8.GetBytes(csv)));

        Assert.Empty(context.Goods);
        Assert.Equal(ImportExportJobStatus.Failed, result.JobStatus);
        Assert.Equal(2, result.TotalRows);
        Assert.Equal(0, result.SuccessRows);
        Assert.Equal(2, result.FailureRows);
        Assert.Contains("商品编码重复", result.ErrorSummary);
    }

    [Fact]
    public async Task ImportGoods_ExistingName_FailsWithoutWritingAnyRows()
    {
        await using var context = CreateDbContext();
        var type = new GoodsType { Id = Guid.NewGuid(), Name = "叶菜", Code = "LEAF" };
        await context.GoodsTypes.AddAsync(type);
        await context.Goods.AddAsync(new GoodsEntity { Id = Guid.NewGuid(), Name = "油麦菜", Code = "LETTUCE", GoodsTypeId = type.Id });
        await context.SaveChangesAsync();
        var service = CreateService(context);
        var csv = $"Name,Code,GoodsTypeId,Spec,Brand,Origin,TaxRate,IsOnSale,Remark\n油麦菜,LETTUCE_NEW,{type.Id},,,,6,true,\n";

        var result = await service.ImportAsync(
            ImportExportJobType.Goods,
            "goods.csv",
            new MemoryStream(Encoding.UTF8.GetBytes(csv)));

        Assert.Single(context.Goods);
        Assert.Equal(ImportExportJobStatus.Failed, result.JobStatus);
        Assert.Contains("商品名称已存在", result.ErrorSummary);
    }

    [Fact]
    public async Task ImportGoods_OverlongPersistedField_FailsAndPersistsFailedJob()
    {
        await using var context = CreateDbContext();
        var type = new GoodsType { Id = Guid.NewGuid(), Name = "根茎", Code = "ROOT" };
        await context.GoodsTypes.AddAsync(type);
        await context.SaveChangesAsync();
        var service = CreateService(context);
        var csv = $"Name,Code,GoodsTypeId,Spec,Brand,Origin,TaxRate,IsOnSale,Remark\n萝卜,RADISH,{type.Id},{new string('规', 101)},,,6,true,\n";

        var result = await service.ImportAsync(
            ImportExportJobType.Goods,
            "goods.csv",
            new MemoryStream(Encoding.UTF8.GetBytes(csv)));

        Assert.Empty(context.Goods);
        Assert.Equal(ImportExportJobStatus.Failed, result.JobStatus);
        Assert.Contains("商品规格不得超过 100", result.ErrorSummary);
        Assert.Equal(ImportExportJobStatus.Failed, (await context.ImportExportJobs.SingleAsync()).JobStatus);
    }

    [Fact]
    public async Task ImportGoods_UppercaseBooleanLiteral_FailsWithoutWritingGoods()
    {
        await using var context = CreateDbContext();
        var type = new GoodsType { Id = Guid.NewGuid(), Name = "菌菇", Code = "MUSHROOM" };
        await context.GoodsTypes.AddAsync(type);
        await context.SaveChangesAsync();
        var service = CreateService(context);
        var csv = $"Name,Code,GoodsTypeId,Spec,Brand,Origin,TaxRate,IsOnSale,Remark\n香菇,SHIITAKE,{type.Id},,,,6,TRUE,\n";

        var result = await service.ImportAsync(
            ImportExportJobType.Goods,
            "goods.csv",
            new MemoryStream(Encoding.UTF8.GetBytes(csv)));

        Assert.Empty(context.Goods);
        Assert.Equal(ImportExportJobStatus.Failed, result.JobStatus);
        Assert.Equal(1, result.TotalRows);
        Assert.Equal(1, result.FailureRows);
        Assert.Contains("true 或 false", result.ErrorSummary);
    }

    [Fact]
    public async Task ImportGoods_MisplacedCsvQuote_FailsWithoutWritingGoods()
    {
        await using var context = CreateDbContext();
        var type = new GoodsType { Id = Guid.NewGuid(), Name = "瓜果", Code = "MELON" };
        await context.GoodsTypes.AddAsync(type);
        await context.SaveChangesAsync();
        var service = CreateService(context);
        var csv = $"Name,Code,GoodsTypeId,Spec,Brand,Origin,TaxRate,IsOnSale,Remark\n错误\"引号,CUCUMBER,{type.Id},,,,6,true,\n";

        var result = await service.ImportAsync(
            ImportExportJobType.Goods,
            "goods.csv",
            new MemoryStream(Encoding.UTF8.GetBytes(csv)));

        Assert.Empty(context.Goods);
        Assert.Equal(ImportExportJobStatus.Failed, result.JobStatus);
        Assert.Contains("引号只能出现在字段开头", result.ErrorSummary);
    }

    [Fact]
    public async Task GetById_OtherUsersJob_ThrowsNotFound()
    {
        await using var context = CreateDbContext();
        var job = new ImportExportJob
        {
            Id = Guid.NewGuid(), JobNo = "IE-OTHER", JobType = ImportExportJobType.Goods,
            JobDirection = ImportExportDirection.Import, JobStatus = ImportExportJobStatus.Succeeded,
            SourceFileName = "other.csv", JobStartedAt = DateTime.UtcNow, JobFinishedAt = DateTime.UtcNow,
            CreateBy = Guid.NewGuid()
        };
        await context.ImportExportJobs.AddAsync(job);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(job.Id));

        Assert.Equal("导入导出任务不存在", exception.Message);
    }

    [Fact]
    public async Task ExportGoods_CreatesCsvAndCompletesJob()
    {
        await using var context = CreateDbContext();
        var type = new GoodsType { Id = Guid.NewGuid(), Name = "蔬菜", Code = "VEGETABLE" };
        await context.GoodsTypes.AddAsync(type);
        await context.Goods.AddAsync(new GoodsEntity
        {
            Id = Guid.NewGuid(),
            Name = "番茄",
            Code = "TOMATO",
            GoodsTypeId = type.Id,
            TaxRate = 6m,
            IsOnSale = true
        });
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.ExportAsync(ImportExportJobType.Goods);
        var text = Encoding.UTF8.GetString(result.Content);
        var job = await service.GetByIdAsync(result.Job.Id);

        Assert.Contains("番茄,TOMATO", text);
        Assert.Equal("goods-export.csv", result.FileName);
        Assert.Equal(ImportExportJobStatus.Succeeded, job.JobStatus);
        Assert.Equal(1, job.SuccessRows);
    }

    [Fact]
    public async Task ExportGoods_NeutralizesSpreadsheetFormulaCells()
    {
        await using var context = CreateDbContext();
        var type = new GoodsType { Id = Guid.NewGuid(), Name = "水果", Code = "FRUIT" };
        await context.GoodsTypes.AddAsync(type);
        await context.Goods.AddAsync(new GoodsEntity
        {
            Id = Guid.NewGuid(), Name = "=HYPERLINK(\"https://unsafe\")", Code = "FORMULA", GoodsTypeId = type.Id, IsOnSale = true
        });
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.ExportAsync(ImportExportJobType.Goods);
        var text = Encoding.UTF8.GetString(result.Content);

        Assert.Contains("'=HYPERLINK", text);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ApplicationDbContext(options);
    }

    private static ImportExportJobService CreateService(ApplicationDbContext context)
    {
        return new ImportExportJobService(
            new ImportExportJobRepository(context),
            new GoodsRepository(context),
            new GoodsTypeRepository(context),
            new UnitOfWork(context),
            new TestCurrentUserService());
    }

    private sealed class TestCurrentUserService : Application.interfaces.ICurrentUserService
    {
        public Guid? GetUserId() => Guid.Parse("11111111-1111-1111-1111-111111111111");
        public string? GetUserName() => "import-export-test";
        public string? GetEmail() => null;
        public string? GetRole() => "admin";
        public IReadOnlyList<string> GetRoles() => ["admin"];
        public bool HasClaim(string claimType, string claimValue) => false;
    }
}
