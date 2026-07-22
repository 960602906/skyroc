using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Shared.Options;

namespace Application.AI.Configuration;

/// <summary>校验 MCP 来源白名单、Token 哈希和内部委托令牌签名密钥。</summary>
internal sealed class McpOptionsValidator : IValidateOptions<McpOptions>
{
    private static readonly Regex EnvironmentVariableNamePattern =
        new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.CultureInvariant);

    /// <summary>外部 MCP 关闭时不会读取 Token 哈希密钥环境变量。</summary>
    public ValidateOptionsResult Validate(string? name, McpOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var failures = new List<string>();

        foreach (var origin in options.AllowedOrigins)
        {
            if (!Uri.TryCreate(origin, UriKind.Absolute, out var originUri) ||
                (originUri.Scheme != Uri.UriSchemeHttp && originUri.Scheme != Uri.UriSchemeHttps) ||
                originUri.AbsolutePath != "/" || !string.IsNullOrEmpty(originUri.Query) ||
                !string.IsNullOrEmpty(originUri.Fragment))
            {
                failures.Add($"Mcp:AllowedOrigins 中的 '{origin}' 必须是无路径、查询和片段的 HTTP/HTTPS 来源。");
            }
        }

        if (options.AllowedOrigins.Count != options.AllowedOrigins.Distinct(StringComparer.OrdinalIgnoreCase).Count())
            failures.Add("Mcp:AllowedOrigins 不能包含重复来源。");

        if (!options.ExternalEnabled)
            return Result(failures);

        ValidateKeyEnvironmentVariable(
            failures,
            nameof(options.TokenHashKeyEnvironmentVariable),
            options.TokenHashKeyEnvironmentVariable);
        ValidateKeyEnvironmentVariable(
            failures,
            nameof(options.DelegationSigningKeyEnvironmentVariable),
            options.DelegationSigningKeyEnvironmentVariable);

        return Result(failures);
    }

    private static void ValidateKeyEnvironmentVariable(
        ICollection<string> failures,
        string propertyName,
        string environmentVariableName)
    {
        var path = $"Mcp:{propertyName}";
        if (string.IsNullOrWhiteSpace(environmentVariableName))
        {
            failures.Add($"{path} 在外部 MCP 启用时不能为空。");
            return;
        }

        if (!EnvironmentVariableNamePattern.IsMatch(environmentVariableName))
        {
            failures.Add($"{path} 不是有效的环境变量名称。");
            return;
        }

        var key = Environment.GetEnvironmentVariable(environmentVariableName);
        if (string.IsNullOrWhiteSpace(key))
            failures.Add($"{path} 指定的环境变量未配置。");
        else if (Encoding.UTF8.GetByteCount(key) < 32)
            failures.Add($"{path} 指定的密钥至少需要 32 个 UTF-8 字节。");
    }

    private static ValidateOptionsResult Result(ICollection<string> failures) =>
        failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
}
