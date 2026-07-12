using Application.DTOs.Customers;
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
        var companySeeds = CreateCompanySeeds();
        var companyCodes = companySeeds.Select(seed => seed.Code).ToArray();
        var auditUser = await context.Users
            .OrderBy(user => user.Username)
            .Select(user => new DemoAuditUser(user.Id, user.Username))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("长期联调数据生成需要已存在的可审计系统用户。");
        SetAuditUser(scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>(), auditUser);
        var existingCompanies = await context.Companies
            .Where(company => companyCodes.Contains(company.Code))
            .ToDictionaryAsync(company => company.Code, StringComparer.Ordinal, cancellationToken);

        var createdCompanies = 0;
        var reusedCompanies = 0;
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

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return new DemoDataGenerationResult(
            new Dictionary<string, int>(StringComparer.Ordinal) { [CompaniesLayer] = createdCompanies },
            new Dictionary<string, int>(StringComparer.Ordinal) { [CompaniesLayer] = reusedCompanies });
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

    private sealed record DemoAuditUser(Guid Id, string Username);
}
