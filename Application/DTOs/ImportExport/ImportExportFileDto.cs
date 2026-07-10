using Domain.Entities.ImportExport;

namespace Application.DTOs.ImportExport;

/// <summary>供控制器下载的 CSV 文件及关联任务结果。</summary>
public class ImportExportFileDto
{
    /// <summary>UTF-8 编码的 CSV 文件内容。</summary>
    public byte[] Content { get; set; } = [];
    /// <summary>响应下载文件名。</summary>
    public string FileName { get; set; } = string.Empty;
    /// <summary>生成本文件的导出任务结果。</summary>
    public ImportExportJobDto Job { get; set; } = new();
}
