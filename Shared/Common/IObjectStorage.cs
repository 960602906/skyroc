namespace Shared.Common;

/// <summary>
/// 对象存储抽象，按键读写二进制内容；实现可对接 RustFS 等 S3 兼容后端。
/// </summary>
public interface IObjectStorage
{
    /// <summary>
    /// 以指定对象键写入内容流；同键覆盖由实现决定，默认创建或覆盖。
    /// </summary>
    /// <param name="key">对象键，对应 <c>StoredFile.StorageKey</c>。</param>
    /// <param name="content">可读内容流，调用方负责在调用前定位到起始位置。</param>
    /// <param name="contentType">经验证的 MIME 类型。</param>
    /// <param name="length">内容字节长度。</param>
    /// <param name="cancellationToken">取消读写的令牌。</param>
    Task PutAsync(string key, Stream content, string contentType, long length, CancellationToken cancellationToken = default);

    /// <summary>
    /// 打开指定对象键的只读流；对象不存在时抛出 <see cref="FileNotFoundException"/>。
    /// </summary>
    /// <param name="key">对象键。</param>
    /// <param name="cancellationToken">取消读取的令牌。</param>
    /// <returns>调用方负责释放的内容流。</returns>
    Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除指定对象键；对象不存在时视为成功。
    /// </summary>
    /// <param name="key">对象键。</param>
    /// <param name="cancellationToken">取消删除的令牌。</param>
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 判断对象键是否存在，供联调数据校验等场景使用。
    /// </summary>
    /// <param name="key">对象键。</param>
    /// <param name="cancellationToken">取消探测的令牌。</param>
    /// <returns>存在则为 <c>true</c>。</returns>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}
