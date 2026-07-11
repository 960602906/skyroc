namespace Domain.Entities.Files;

/// <summary>
/// 受保护的已上传文件元数据，物理文件只可通过授权下载接口访问。
/// </summary>
public class StoredFile : BaseEntity
{
    /// <summary>
    /// 服务端生成的相对存储键，不包含用户提供的路径或文件名。
    /// </summary>
    public string StorageKey { get; set; } = string.Empty;

    /// <summary>
    /// 上传时保留的原始文件名，仅用于下载展示，最大 255 个字符。
    /// </summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// 由文件签名字节识别出的 MIME 类型，不信任客户端声明值。
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// 已验证文件的实际字节数，必须大于零且不超过服务端上传上限。
    /// </summary>
    public long FileSize { get; set; }
}
