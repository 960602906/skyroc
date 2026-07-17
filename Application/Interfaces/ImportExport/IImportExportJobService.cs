using Application.DTOs.ImportExport;
using Domain.Entities.ImportExport;

namespace Application.Interfaces;

/// <summary>统一导入导出任务应用服务，提供模板、同步 CSV 导入、导出和任务状态查询。</summary>
public interface IImportExportJobService
{
    /// <summary>下载指定业务类型的 CSV 导入模板。</summary>
    /// <param name="jobType">模板所属业务类型。</param>
    /// <returns>带表头和示例行的 UTF-8 CSV 文件。</returns>
    Task<ImportExportFileDto> DownloadTemplateAsync(ImportExportJobType jobType);

    /// <summary>读取并校验 CSV 后原子导入业务数据；校验失败不写入任何业务行。</summary>
    /// <param name="jobType">导入业务类型。</param>
    /// <param name="fileName">用户上传的原始文件名。</param>
    /// <param name="content">未落盘的上传文件流。</param>
    /// <returns>已成功或失败的导入任务状态。</returns>
    Task<ImportExportJobDto> ImportAsync(ImportExportJobType jobType, string fileName, Stream content);

    /// <summary>按业务类型生成当前数据的 CSV 导出文件并保存任务结果。</summary>
    /// <param name="jobType">导出业务类型。</param>
    /// <returns>UTF-8 CSV 文件及完成后的导出任务。</returns>
    Task<ImportExportFileDto> ExportAsync(ImportExportJobType jobType);

    /// <summary>查询当前操作人创建的任务状态，避免泄露其他用户的导入导出记录。</summary>
    /// <param name="id">任务主键。</param>
    /// <returns>任务进度、行数和错误摘要。</returns>
    Task<ImportExportJobDto> GetByIdAsync(Guid id);
}
