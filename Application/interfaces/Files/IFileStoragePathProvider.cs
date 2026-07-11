namespace Application.interfaces;

/// <summary>
/// 向应用层提供内容根目录和 Web 根目录，用于将上传存储与静态文件托管路径隔离。
/// </summary>
public interface IFileStoragePathProvider
{
    /// <summary>
    /// 应用内容根目录，用于解析相对的文件存储配置。
    /// </summary>
    string ContentRootPath { get; }

    /// <summary>
    /// 静态 Web 根目录；未启用时为空。
    /// </summary>
    string? WebRootPath { get; }
}
