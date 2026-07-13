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
}
