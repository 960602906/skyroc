using Microsoft.AspNetCore.Http;

namespace Application.DTOs.Files;

/// <summary>
/// 安全文件上传的 multipart 请求体，文件字段固定为 <c>file</c>。
/// </summary>
public class FileUploadDto
{
    /// <summary>
    /// 待验证的 PDF、PNG 或 JPEG 文件，单文件不得超过 10 MiB。
    /// </summary>
    public IFormFile? File { get; set; }
}
