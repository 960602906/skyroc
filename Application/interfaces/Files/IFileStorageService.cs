using Application.DTOs.Files;

namespace Application.interfaces;

/// <summary>
/// 安全文件存储应用服务，验证允许的文件类型、大小和创建人访问范围。
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// 验证并保存一个 PDF、PNG 或 JPEG 文件，返回不含物理路径的下载信息。
    /// </summary>
    /// <param name="upload">包含原始名称、声明类型、长度和内容流的上传请求。</param>
    /// <param name="cancellationToken">取消当前上传读写的令牌。</param>
    /// <returns>已持久化文件的安全展示和下载信息。</returns>
    Task<StoredFileDto> UploadAsync(FileUploadRequest upload, CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取当前创建人拥有的文件内容；其他用户和不存在的文件都按不存在处理。
    /// </summary>
    /// <param name="id">文件元数据主键。</param>
    /// <param name="cancellationToken">取消当前文件读取的令牌。</param>
    /// <returns>供受保护 HTTP 响应输出的文件流与 MIME 元数据。</returns>
    Task<StoredFileContent> DownloadAsync(Guid id, CancellationToken cancellationToken = default);
}
