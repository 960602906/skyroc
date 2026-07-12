using System.Text.RegularExpressions;

namespace SkyRoc.Tests.Testing.PostgreSql;

/// <summary>
///     为长期前端联调数据提供可重复计算的精确业务键，禁止通过人工名称模糊定位记录。
/// </summary>
public static partial class DemoDataStableKeyCatalog
{
    /// <summary>
    ///     所有由自动生成器管理的长期业务编码前缀。
    /// </summary>
    public const string ManagedPrefix = "SKYROC-DEMO";

    /// <summary>
    ///     基于领域和正序号创建稳定业务键；领域码仅允许大写英文字母、数字和连字符。
    /// </summary>
    /// <param name="businessArea">业务领域的受控 ASCII 标识，例如 <c>GOODS</c>。</param>
    /// <param name="sequence">同一领域内从 1 开始的稳定序号。</param>
    /// <returns>可用于精确查询和幂等更新的长期联调业务键。</returns>
    /// <exception cref="ArgumentException">领域码为空、含非法字符或序号不为正数时抛出。</exception>
    public static string Create(string businessArea, int sequence)
    {
        if (string.IsNullOrWhiteSpace(businessArea))
            throw new ArgumentException("业务领域码不能为空。", nameof(businessArea));
        if (sequence <= 0)
            throw new ArgumentException("稳定业务键序号必须为正数。", nameof(sequence));

        var normalizedBusinessArea = businessArea.Trim().ToUpperInvariant();
        if (!BusinessAreaPattern().IsMatch(normalizedBusinessArea))
        {
            throw new ArgumentException(
                "业务领域码只能包含英文字母、数字和连字符。",
                nameof(businessArea));
        }

        return $"{ManagedPrefix}-{normalizedBusinessArea}-{sequence:D3}";
    }

    /// <summary>
    ///     判断值是否具备自动生成器的完整管理前缀；调用方仍必须用完整业务键精确查询。
    /// </summary>
    public static bool IsManaged(string? businessKey)
    {
        return businessKey?.StartsWith($"{ManagedPrefix}-", StringComparison.Ordinal) == true;
    }

    [GeneratedRegex("^[A-Z0-9]+(?:-[A-Z0-9]+)*$")]
    private static partial Regex BusinessAreaPattern();
}
