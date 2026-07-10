using Domain.Entities.ImportExport;

namespace Domain.Interfaces;

/// <summary>
/// 导入导出任务仓储，保存任务状态并按主键查询处理结果。
/// </summary>
public interface IImportExportJobRepository : IRepository<ImportExportJob>
{
}
