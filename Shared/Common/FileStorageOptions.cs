namespace Shared.Common;

/// <summary>
/// 受保护上传文件的磁盘存储配置；目录不会映射为静态 HTTP 资源。
/// </summary>
public class FileStorageOptions
{
    /// <summary>
    /// 配置节名称。
    /// </summary>
    public const string SectionName = "FileStorage";

    /// <summary>
    /// 文件相对或绝对存储根目录；服务会拒绝位于 Web 根目录内的配置。
    /// </summary>
    public string StorageRoot { get; set; } = "App_Data/uploads";

    /// <summary>
    /// 单文件允许的最大字节数，固定为 10 MiB。
    /// </summary>
    public const long MaxUploadSizeBytes = 10 * 1024 * 1024;
}
