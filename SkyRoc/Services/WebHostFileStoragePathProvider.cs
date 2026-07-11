using Application.interfaces;
using Microsoft.AspNetCore.Hosting;

namespace SkyRoc.Services;

/// <summary>
/// 将 ASP.NET Core 宿主目录适配为应用层文件存储路径抽象。
/// </summary>
public class WebHostFileStoragePathProvider(IWebHostEnvironment environment) : IFileStoragePathProvider
{
    /// <summary>
    /// 当前应用的内容根目录。
    /// </summary>
    public string ContentRootPath => environment.ContentRootPath;

    /// <summary>
    /// 当前应用的静态 Web 根目录；未配置时为空。
    /// </summary>
    public string? WebRootPath => environment.WebRootPath;
}
