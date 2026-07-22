using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Shared.Options;

namespace Application.AI.Configuration;

/// <summary>校验 AI 全局边界、能力网关及活动 Provider 的地址、模型和凭据来源。</summary>
internal sealed class AiOptionsValidator : IValidateOptions<AiOptions>
{
    private static readonly Regex EnvironmentVariableNamePattern =
        new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.CultureInvariant);

    /// <summary>AI 关闭时仍检查数值边界，但不会读取厂商密钥环境变量。</summary>
    public ValidateOptionsResult Validate(string? name, AiOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var failures = new List<string>();
        ValidateRange(failures, nameof(options.MaxInputCharacters), options.MaxInputCharacters, 1, 100_000);
        ValidateRange(failures, nameof(options.MaxOutputTokens), options.MaxOutputTokens, 1, 65_536);
        ValidateRange(failures, nameof(options.MaxToolIterations), options.MaxToolIterations, 1, 20);
        ValidateRange(failures, nameof(options.RequestTimeoutSeconds), options.RequestTimeoutSeconds, 1, 600);
        ValidateRange(failures, nameof(options.ConversationRetentionDays), options.ConversationRetentionDays, 1, 365);
        ValidateRange(failures, nameof(options.DraftExpiryMinutes), options.DraftExpiryMinutes, 1, 1440);
        ValidateRange(
            failures,
            $"{nameof(options.CapabilityGateway)}:{nameof(options.CapabilityGateway.SearchLimit)}",
            options.CapabilityGateway.SearchLimit,
            1,
            20);
        ValidateRange(
            failures,
            $"{nameof(options.CapabilityGateway)}:{nameof(options.CapabilityGateway.MaxToolResultBytes)}",
            options.CapabilityGateway.MaxToolResultBytes,
            1024,
            1_048_576);
        ValidateRange(
            failures,
            $"{nameof(options.CapabilityGateway)}:{nameof(options.CapabilityGateway.DelegationTokenLifetimeSeconds)}",
            options.CapabilityGateway.DelegationTokenLifetimeSeconds,
            1,
            60);
        ValidateRange(
            failures,
            $"{nameof(options.CapabilityGateway)}:{nameof(options.CapabilityGateway.HighRiskConfirmationLifetimeSeconds)}",
            options.CapabilityGateway.HighRiskConfirmationLifetimeSeconds,
            1,
            300);

        if (!options.Enabled)
            return Result(failures);

        ValidateInternalBaseUrl(failures, options.CapabilityGateway.InternalBaseUrl);

        if (string.IsNullOrWhiteSpace(options.ActiveProvider))
        {
            failures.Add("Ai:ActiveProvider 在 AI 启用时不能为空。");
            return ValidateOptionsResult.Fail(failures);
        }

        var activeProvider = options.Providers.FirstOrDefault(pair =>
            string.Equals(pair.Key, options.ActiveProvider, StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrWhiteSpace(activeProvider.Key) || activeProvider.Value is null)
        {
            failures.Add($"Ai:ActiveProvider '{options.ActiveProvider}' 未在 Ai:Providers 中定义。");
            return ValidateOptionsResult.Fail(failures);
        }

        ValidateProvider(failures, activeProvider.Key, activeProvider.Value);
        return Result(failures);
    }

    private static void ValidateProvider(
        ICollection<string> failures,
        string providerName,
        AiProviderOptions provider)
    {
        var path = $"Ai:Providers:{providerName}";
        if (string.IsNullOrWhiteSpace(provider.Adapter))
            failures.Add($"{path}:Adapter 不能为空。");

        if (string.IsNullOrWhiteSpace(provider.BaseUrl))
        {
            failures.Add($"{path}:BaseUrl 不能为空。");
        }
        else if (!Uri.TryCreate(provider.BaseUrl, UriKind.Absolute, out var baseUri) ||
                 (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
        {
            failures.Add($"{path}:BaseUrl 必须是绝对 HTTP/HTTPS 地址。");
        }

        if (string.IsNullOrWhiteSpace(provider.Model))
            failures.Add($"{path}:Model 不能为空。");

        if (!string.IsNullOrWhiteSpace(provider.ApiKey))
            return;

        if (string.IsNullOrWhiteSpace(provider.ApiKeyEnvironmentVariable))
        {
            failures.Add($"{path}:ApiKey 或 ApiKeyEnvironmentVariable 必须配置一项。");
            return;
        }

        if (!EnvironmentVariableNamePattern.IsMatch(provider.ApiKeyEnvironmentVariable))
        {
            failures.Add($"{path}:ApiKeyEnvironmentVariable 不是有效的环境变量名称。");
            return;
        }

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(provider.ApiKeyEnvironmentVariable)))
            failures.Add($"{path}:ApiKeyEnvironmentVariable 指定的环境变量未配置。");
    }

    private static void ValidateInternalBaseUrl(ICollection<string> failures, string internalBaseUrl)
    {
        const string path = "Ai:CapabilityGateway:InternalBaseUrl";
        if (string.IsNullOrWhiteSpace(internalBaseUrl))
        {
            failures.Add($"{path} 在 AI 启用时不能为空。");
            return;
        }

        if (!Uri.TryCreate(internalBaseUrl, UriKind.Absolute, out var baseUri) ||
            (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps) ||
            string.IsNullOrWhiteSpace(baseUri.Host) ||
            !string.IsNullOrEmpty(baseUri.UserInfo) ||
            baseUri.AbsolutePath != "/" ||
            !string.IsNullOrEmpty(baseUri.Query) ||
            !string.IsNullOrEmpty(baseUri.Fragment))
        {
            failures.Add($"{path} 必须是无凭据、路径、查询和片段的绝对 HTTP/HTTPS 来源。");
        }
    }

    private static void ValidateRange(
        ICollection<string> failures,
        string propertyName,
        int value,
        int minimum,
        int maximum)
    {
        if (value < minimum || value > maximum)
            failures.Add($"Ai:{propertyName} 必须在 {minimum} 到 {maximum} 之间。");
    }

    private static ValidateOptionsResult Result(ICollection<string> failures) =>
        failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
}
