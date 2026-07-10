using Microsoft.AspNetCore.Http;

namespace Application.DTOs.ImportExport;

/// <summary>导入任务的 multipart 请求体，仅在本次请求生命周期内读取 CSV 文件流而不落盘。</summary>
public class ImportExportUploadDto
{
    /// <summary>不超过 2 MiB 的 CSV 文件，扩展名必须为 .csv。</summary>
    public IFormFile File { get; set; } = null!;
}
