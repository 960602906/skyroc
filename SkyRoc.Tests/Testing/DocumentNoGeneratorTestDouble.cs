using Application.interfaces;
using Application.Services;

namespace SkyRoc.Tests.Testing;

/// <summary>
/// 测试中手工构造应用服务时复用的单据编号生成器实例。
/// </summary>
internal static class DocumentNoGeneratorTestDouble
{
    /// <summary>与生产一致的真实生成器，无外部依赖。</summary>
    public static IDocumentNoGenerator Instance { get; } = new DocumentNoGeneratorService();
}
