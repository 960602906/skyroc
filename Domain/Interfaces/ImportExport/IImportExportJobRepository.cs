using Domain.Entities.ImportExport;

namespace Domain.Interfaces;

/// <summary>
/// 导入导出任务仓储，保存任务状态并按主键查询处理结果。
/// </summary>
public interface IImportExportJobRepository : IRepository<ImportExportJob>
{
    /// <summary>
    /// 根据任务编号查询导入导出任务。
    /// </summary>
    /// <param name="jobNo">任务编号。</param>
    /// <returns>导入导出任务；不存在时返回 <c>null</c>。</returns>
    Task<ImportExportJob?> GetByJobNoAsync(string jobNo);
}
