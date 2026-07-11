using Domain.Entities.Printing;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// 打印模板仓储实现，统一读取设计 JSON 和已排序的字段定义。
/// </summary>
public class PrintTemplateRepository(ApplicationDbContext context)
    : Repository<PrintTemplate>(context), IPrintTemplateRepository
{
    /// <inheritdoc />
    public override Task<PrintTemplate?> GetByIdAsync(Guid id) => GetWithFieldsAsync(id);

    /// <inheritdoc />
    public Task<PrintTemplate?> GetWithFieldsAsync(Guid id)
    {
        return BuildDetailQuery().FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <inheritdoc />
    public Task<PrintTemplate?> GetByCodeAsync(string templateCode)
    {
        var normalizedCode = templateCode.Trim();
        return BuildDetailQuery().AsNoTracking().FirstOrDefaultAsync(x => x.TemplateCode == normalizedCode && x.IsEnabled);
    }

    /// <inheritdoc />
    public Task<bool> ExistsTemplateCodeAsync(string templateCode, Guid? excludeId = null)
    {
        var normalizedCode = templateCode.Trim();
        return DbSet.AnyAsync(x => x.TemplateCode == normalizedCode && (!excludeId.HasValue || x.Id != excludeId));
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<PrintTemplate> Data, int Total)> GetPagedWithFieldsAsync(int pageNumber, int pageSize)
    {
        var total = await DbSet.CountAsync();
        var data = await BuildDetailQuery()
            .AsNoTracking()
            .OrderBy(template => template.TemplateCode)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (data, total);
    }

    /// <inheritdoc />
    public Task RemoveFieldsAsync(IEnumerable<PrintTemplateField> fields)
    {
        Context.Set<PrintTemplateField>().RemoveRange(fields);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task AddFieldsAsync(IEnumerable<PrintTemplateField> fields)
    {
        await Context.Set<PrintTemplateField>().AddRangeAsync(fields);
    }

    /// <summary>构建包含字段并按显示位置排序的模板聚合查询。</summary>
    /// <returns>可用于查询模板完整配置的实体查询。</returns>
    private IQueryable<PrintTemplate> BuildDetailQuery()
    {
        return DbSet.Include(x => x.Fields.OrderBy(field => field.DisplayOrder)).AsSplitQuery();
    }
}
