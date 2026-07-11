using System.Text.RegularExpressions;
using Xunit;

namespace SkyRoc.Tests.Documentation;

/// <summary>
///     验证项目收口文档与实际联调脚本和已交付阶段保持一致。
/// </summary>
public class ProjectClosureDocumentationTests
{
    /// <summary>
    ///     README 记录的 HTTP 请求数量应与联调脚本中的真实请求数量一致。
    /// </summary>
    [Fact]
    public void Readme_ReportsActualHttpRequestCount()
    {
        var readme = File.ReadAllText(GetRepositoryFile("README.md"));
        var http = File.ReadAllText(GetRepositoryFile("SkyRoc", "SkyRoc.http"));
        var requestCount = Regex.Matches(http, @"^(GET|POST|PUT|PATCH|DELETE) ", RegexOptions.Multiline).Count;

        Assert.Contains($"HTTP 脚本当前共记录 {requestCount} 个请求", readme);
    }

    /// <summary>
    ///     README 不应继续宣称已交付模块尚未实现或指向早已完成的开发任务。
    /// </summary>
    [Fact]
    public void Readme_DoesNotContainStaleDeliveryBreakpoints()
    {
        var readme = File.ReadAllText(GetRepositoryFile("README.md"));

        Assert.DoesNotContain("报表和溯源 API 尚未实现", readme);
        Assert.DoesNotContain("当前任务是 **P4-02", readme);
        Assert.DoesNotContain("还没有完整操作审计链路", readme);
    }

    private static string GetRepositoryFile(params string[] pathSegments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SkyRoc.sln")))
            directory = directory.Parent;

        Assert.NotNull(directory);
        return Path.Combine([directory.FullName, .. pathSegments]);
    }
}
