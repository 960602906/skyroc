using System.Globalization;
using System.Text.Json;
using Application.interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Common;

namespace Application.External;

/// <summary>
///     基于天眼查开放平台的企业工商信息提供者。
/// </summary>
public class TianyanchaCompanyInfoProvider(
    HttpClient httpClient,
    IOptions<TianyanchaOptions> options,
    ILogger<TianyanchaCompanyInfoProvider> logger)
    : ICompanyInfoProvider
{
    private readonly TianyanchaOptions _options = options.Value;

    public async Task<CompanyBusinessInfo?> GetCompanyInfoAsync(string keyword, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.Token) || string.IsNullOrWhiteSpace(keyword))
        {
            return null;
        }

        var searchInfo = await SearchFirstAsync(keyword.Trim(), cancellationToken);
        var baseInfo = await GetBaseInfoAsync(searchInfo?.ExternalId, searchInfo?.Name ?? keyword.Trim(), cancellationToken);

        return Merge(baseInfo, searchInfo);
    }

    private async Task<CompanyBusinessInfo?> SearchFirstAsync(string keyword, CancellationToken cancellationToken)
    {
        var uri = BuildUri(_options.SearchPath,
            ("word", keyword),
            ("pageSize", "1"),
            ("pageNum", "1"));

        var root = await GetJsonRootAsync(uri, cancellationToken);
        if (root is null)
        {
            return null;
        }

        var result = TryGet(root.Value, "result");
        if (result is null)
        {
            return null;
        }

        var first = FirstArrayItem(result.Value, "items", "list", "data");
        return first is null ? null : ParseCompanyInfo(first.Value);
    }

    private async Task<CompanyBusinessInfo?> GetBaseInfoAsync(
        string? companyId,
        string keyword,
        CancellationToken cancellationToken)
    {
        var root = await GetJsonRootAsync(
            string.IsNullOrWhiteSpace(companyId)
                ? BuildUri(_options.BaseInfoPath, ("keyword", keyword))
                : BuildUri(_options.BaseInfoPath, ("id", companyId)),
            cancellationToken);
        if (root is null)
        {
            return null;
        }

        var result = TryGet(root.Value, "result");
        return result is null ? null : ParseCompanyInfo(result.Value);
    }

    private async Task<JsonElement?> GetJsonRootAsync(string requestUri, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.TryAddWithoutValidation("Authorization", _options.Token);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("天眼查请求失败: {StatusCode} {RequestUri}", response.StatusCode, requestUri);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = document.RootElement.Clone();

            if (TryGet(root, "error_code", "errorCode") is { } errorCode && ReadString(errorCode) is { } code && code != "0")
            {
                logger.LogWarning("天眼查返回业务错误: {ErrorCode} {RequestUri}", code, requestUri);
                return null;
            }

            return root;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            logger.LogWarning(ex, "天眼查请求异常: {RequestUri}", requestUri);
            return null;
        }
    }

    private static CompanyBusinessInfo ParseCompanyInfo(JsonElement element)
    {
        var fromTime = ReadDateTime(TryGet(element, "fromTime", "businessFrom"));
        var toTime = ReadDateTime(TryGet(element, "toTime", "businessTo"));

        return new CompanyBusinessInfo
        {
            ExternalId = ReadString(TryGet(element, "id", "companyId")),
            Name = ReadString(TryGet(element, "name", "companyName")),
            UnifiedSocialCreditCode = ReadString(TryGet(element,
                "creditCode",
                "unifiedSocialCreditCode",
                "socialCreditCode")),
            LegalRepresentative = ReadString(TryGet(element,
                "legalPersonName",
                "legalRepresentative",
                "legalPerson")),
            RegisteredCapital = ReadString(TryGet(element, "regCapital", "registeredCapital")),
            EstablishDate = ReadDateTime(TryGet(element, "estiblishTime", "establishTime", "establishDate")),
            BusinessTerm = ReadString(TryGet(element, "businessTerm")) ?? BuildBusinessTerm(fromTime, toTime),
            RegistrationStatus = ReadString(TryGet(element, "regStatus", "registrationStatus")),
            RegistrationAuthority = ReadString(TryGet(element, "regInstitute", "registrationAuthority")),
            RegisteredAddress = ReadString(TryGet(element, "regLocation", "registeredAddress", "address")),
            BusinessScope = ReadString(TryGet(element, "businessScope")),
            ContactPhone = ReadString(TryGet(element, "phoneNumber", "phone", "tel")),
            Email = ReadString(TryGet(element, "email", "emails"))
        };
    }

    private static CompanyBusinessInfo? Merge(CompanyBusinessInfo? preferred, CompanyBusinessInfo? fallback)
    {
        if (preferred is null)
        {
            return fallback;
        }

        if (fallback is null)
        {
            return preferred;
        }

        preferred.ExternalId ??= fallback.ExternalId;
        preferred.Name ??= fallback.Name;
        preferred.UnifiedSocialCreditCode ??= fallback.UnifiedSocialCreditCode;
        preferred.LegalRepresentative ??= fallback.LegalRepresentative;
        preferred.RegisteredCapital ??= fallback.RegisteredCapital;
        preferred.EstablishDate ??= fallback.EstablishDate;
        preferred.BusinessTerm ??= fallback.BusinessTerm;
        preferred.RegistrationStatus ??= fallback.RegistrationStatus;
        preferred.RegistrationAuthority ??= fallback.RegistrationAuthority;
        preferred.RegisteredAddress ??= fallback.RegisteredAddress;
        preferred.BusinessScope ??= fallback.BusinessScope;
        preferred.ContactPhone ??= fallback.ContactPhone;
        preferred.Email ??= fallback.Email;

        return preferred;
    }

    private static string BuildUri(string path, params (string Key, string? Value)[] query)
    {
        var filtered = query.Where(x => !string.IsNullOrWhiteSpace(x.Value)).ToList();
        if (filtered.Count == 0)
        {
            return path;
        }

        var separator = path.Contains('?') ? "&" : "?";
        return path + separator + string.Join("&", filtered.Select(x =>
            $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value!)}"));
    }

    private static JsonElement? TryGet(JsonElement element, params string[] names)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (names.Any(name => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                return property.Value;
            }
        }

        return null;
    }

    private static JsonElement? FirstArrayItem(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            var value = TryGet(element, name);
            if (value is { ValueKind: JsonValueKind.Array } array && array.GetArrayLength() > 0)
            {
                return array[0];
            }
        }

        return null;
    }

    private static string? ReadString(JsonElement? element)
    {
        if (element is null || element.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return element.Value.ValueKind == JsonValueKind.String
            ? BlankToNull(element.Value.GetString())
            : BlankToNull(element.Value.ToString());
    }

    private static DateTime? ReadDateTime(JsonElement? element)
    {
        var value = ReadString(element);
        if (string.IsNullOrWhiteSpace(value) || value == "-")
        {
            return null;
        }

        if (long.TryParse(value, out var timestamp))
        {
            if (timestamp > 9_999_999_999)
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime;
            }

            return DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
        }

        var formats = new[] { "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss", "yyyy/MM/dd", "yyyy/MM/dd HH:mm:ss" };
        return DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture,
                   DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var exact)
               || DateTime.TryParse(value, CultureInfo.InvariantCulture,
                   DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out exact)
            ? exact
            : null;
    }

    private static string? BuildBusinessTerm(DateTime? fromTime, DateTime? toTime)
    {
        if (!fromTime.HasValue && !toTime.HasValue)
        {
            return null;
        }

        return $"{FormatTermDate(fromTime)} 至 {FormatTermDate(toTime)}";
    }

    private static string FormatTermDate(DateTime? date)
    {
        return date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "长期";
    }

    private static string? BlankToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
