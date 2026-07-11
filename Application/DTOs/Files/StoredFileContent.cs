namespace Application.DTOs.Files;

/// <summary>
/// 受保护下载接口使用的已验证文件内容和响应元数据。
/// </summary>
public class StoredFileContent : IAsyncDisposable
{
    /// <summary>
    /// 仅在当前 HTTP 响应期间读取的文件流。
    /// </summary>
    public Stream Content { get; init; } = Stream.Null;

    /// <summary>
    /// 下载时建议保存的原始文件名。
    /// </summary>
    public string FileName { get; init; } = string.Empty;

    /// <summary>
    /// 经服务端验证的响应 MIME 类型。
    /// </summary>
    public string ContentType { get; init; } = string.Empty;

    /// <inheritdoc />
    public ValueTask DisposeAsync() => Content.DisposeAsync();
}
