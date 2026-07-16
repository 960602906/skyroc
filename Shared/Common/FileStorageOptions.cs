namespace Shared.Common;

/// <summary>
/// 受保护文件上传的固定业务限制；实际对象存放由 <see cref="RustFsOptions"/> 配置。
/// </summary>
public static class FileStorageOptions
{
    /// <summary>
    /// 单文件允许的最大字节数，固定为 10 MiB。
    /// </summary>
    public const long MaxUploadSizeBytes = 10 * 1024 * 1024;
}
