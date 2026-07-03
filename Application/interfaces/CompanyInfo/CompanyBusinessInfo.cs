namespace Application.interfaces;

/// <summary>
///     企业工商信息。
/// </summary>
public sealed class CompanyBusinessInfo
{
    /// <summary>
    /// 外部工商数据源中的企业标识。
    /// </summary>
    public string? ExternalId { get; set; }

    /// <summary>
    /// 企业登记名称。
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 企业统一社会信用代码。
    /// </summary>
    public string? UnifiedSocialCreditCode { get; set; }

    /// <summary>
    /// 企业法定代表人。
    /// </summary>
    public string? LegalRepresentative { get; set; }

    /// <summary>
    /// 企业登记注册资本。
    /// </summary>
    public string? RegisteredCapital { get; set; }

    /// <summary>
    /// 企业成立日期。
    /// </summary>
    public DateTime? EstablishDate { get; set; }

    /// <summary>
    /// 企业登记营业期限。
    /// </summary>
    public string? BusinessTerm { get; set; }

    /// <summary>
    /// 企业工商登记状态。
    /// </summary>
    public string? RegistrationStatus { get; set; }

    /// <summary>
    /// 企业登记机关。
    /// </summary>
    public string? RegistrationAuthority { get; set; }

    /// <summary>
    /// 企业工商注册地址。
    /// </summary>
    public string? RegisteredAddress { get; set; }

    /// <summary>
    /// 企业登记经营范围。
    /// </summary>
    public string? BusinessScope { get; set; }

    /// <summary>
    /// 工商数据源提供的联系电话。
    /// </summary>
    public string? ContactPhone { get; set; }

    /// <summary>
    /// 工商数据源提供的电子邮箱。
    /// </summary>
    public string? Email { get; set; }
}
