using Application.DTOs.Files;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using Shared.Constants;
using SkyRoc.Authorization;

namespace SkyRoc.Controllers;

/// <summary>
/// 受保护文件控制器，提供 PDF、PNG 和 JPEG 的安全上传及创建人下载。
/// </summary>
[ApiController]
[Route("api/files")]
[Authorize]
[PermissionResource(PermissionCodes.Business.Files.Resource)]
public class FilesController(IFileStorageService service) : ControllerBase
{
    /// <summary>
    /// 上传一个不超过 10 MiB 的 PDF、PNG 或 JPEG；文件名、声明 MIME 类型和二进制签名必须一致。
    /// </summary>
    /// <param name="upload">multipart 请求体，其 <c>file</c> 字段内容会保存到非公开存储目录。</param>
    /// <param name="cancellationToken">客户端中止请求时取消上传读写。</param>
    /// <returns>包含文件主键、已验证 MIME 类型、字节数和受保护下载地址的响应。</returns>
    [HttpPost]
    [Authorize(Policy = PermissionCodes.Business.Files.Create)]
    [ResourcePermission(PermissionActions.Create)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(FileStorageOptions.MaxUploadSizeBytes + 64 * 1024)]
    public async Task<ActionResult<ApiResponse<StoredFileDto>>> Upload([FromForm] FileUploadDto upload, CancellationToken cancellationToken)
    {
        var file = upload.File;
        if (file is null)
        {
            return Ok(ApiResponse<StoredFileDto>.BadRequest("必须提供 multipart 字段 file"));
        }

        await using var content = file.OpenReadStream();
        var result = await service.UploadAsync(new FileUploadRequest
        {
            FileName = file.FileName,
            ContentType = file.ContentType,
            Length = file.Length,
            Content = content
        }, cancellationToken);
        return Ok(ApiResponse<StoredFileDto>.Ok(result));
    }

    /// <summary>
    /// 下载当前创建人上传的文件；不存在、物理文件缺失或其他用户文件均返回未找到，避免泄露存在性。
    /// </summary>
    /// <param name="id">上传文件元数据主键。</param>
    /// <param name="cancellationToken">客户端中止请求时取消元数据读取。</param>
    /// <returns>经验证的原始二进制文件流。</returns>
    [HttpGet("{id:guid}/download")]
    [Authorize(Policy = PermissionCodes.Business.Files.Read)]
    [ResourcePermission(PermissionActions.Read)]
    [Produces("application/pdf", "image/png", "image/jpeg")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    public async Task<FileStreamResult> Download(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.DownloadAsync(id, cancellationToken);
        return File(result.Content, result.ContentType, result.FileName, enableRangeProcessing: true);
    }
}
