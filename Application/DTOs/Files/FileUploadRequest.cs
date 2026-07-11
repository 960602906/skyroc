namespace Application.DTOs.Files;

/// <summary>
/// 应用层接收的待验证文件流，控制器负责将 multipart 文件转换为此对象。
/// </summary>
public class FileUploadRequest
{
    /// <summary>
    /// 客户端提供的原始文件名，仅用于扩展名校验和下载展示。
    /// </summary>
    public string FileName { get; init; } = string.Empty;

    /// <summary>
    /// 客户端声明的 MIME 类型，必须与服务器签名识别结果一致。
    /// </summary>
    public string ContentType { get; init; } = string.Empty;

    /// <summary>
    /// 客户端声明的文件字节数，服务还会复核流的实际字节数。
    /// </summary>
    public long Length { get; init; }

    /// <summary>
    /// 待读取的文件内容流；调用方在服务返回前保持该流可读。
    /// </summary>
    public Stream Content { get; init; } = Stream.Null;
}
