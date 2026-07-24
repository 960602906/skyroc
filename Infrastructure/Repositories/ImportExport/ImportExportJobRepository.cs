using Microsoft.EntityFrameworkCore;
using Domain.Entities.ImportExport;
using Domain.Interfaces;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

/// <summary>
/// 导入导出任务仓储，实现统一任务记录的持久化访问。
/// </summary>
public class ImportExportJobRepository(ApplicationDbContext context)
    : Repository<ImportExportJob>(context), IImportExportJobRepository
{
    /// <inheritdoc />
    public Task<ImportExportJob?> GetByJobNoAsync(string jobNo)
    {
        var normalizedJobNo = jobNo.Trim();
        return DbSet.FirstOrDefaultAsync(x => x.JobNo == normalizedJobNo);
    }
}
