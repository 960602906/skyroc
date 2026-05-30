namespace Application.interfaces;

/// <summary>
///     企业工商信息提供者。
/// </summary>
public interface ICompanyInfoProvider
{
    /// <summary>
    ///     按企业名称或关键字查询工商信息。
    /// </summary>
    Task<CompanyBusinessInfo?> GetCompanyInfoAsync(string keyword, CancellationToken cancellationToken = default);
}

/// <summary>
///     企业工商信息。
/// </summary>
public sealed class CompanyBusinessInfo
{
    public string? ExternalId { get; set; }

    public string? Name { get; set; }

    public string? UnifiedSocialCreditCode { get; set; }

    public string? LegalRepresentative { get; set; }

    public string? RegisteredCapital { get; set; }

    public DateTime? EstablishDate { get; set; }

    public string? BusinessTerm { get; set; }

    public string? RegistrationStatus { get; set; }

    public string? RegistrationAuthority { get; set; }

    public string? RegisteredAddress { get; set; }

    public string? BusinessScope { get; set; }

    public string? ContactPhone { get; set; }

    public string? Email { get; set; }
}
