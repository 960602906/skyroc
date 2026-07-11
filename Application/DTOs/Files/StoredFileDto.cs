namespace Application.DTOs.Files;

/// <summary>
/// 已上传文件的安全响应模型，不暴露服务器物理路径或存储键。
/// </summary>
public class StoredFileDto
{
    /// <summary>
    /// 文件元数据主键，用于受保护下载。
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// 原始文件名，供客户端显示和下载保存。
    /// </summary>
    public string FileName { get; init; } = string.Empty;

    /// <summary>
    /// 经服务端签名验证的 MIME 类型。
    /// </summary>
    public string ContentType { get; init; } = string.Empty;

    /// <summary>
    /// 文件实际大小，单位为字节。
    /// </summary>
    public long Size { get; init; }

    /// <summary>
    /// 需授权访问的下载相对地址。
    /// </summary>
    public string DownloadUrl => $"/api/files/{Id}/download";
}
