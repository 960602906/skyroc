using Application.Serialization;
using System.Text.Json.Serialization;

namespace Application.DTOs.Customers;

/// <summary>
///     客户 DTO。
/// </summary>
public class CustomerDto : BaseDto
{
    /// <summary>
    ///     客户名称。
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///     客户编码。
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    ///     所属公司 ID。
    /// </summary>
    public Guid? CompanyId { get; set; }

    /// <summary>
    ///     默认报价单 ID。
    /// </summary>
    public Guid? QuotationId { get; set; }

    /// <summary>
    ///     默认仓库 ID。
    /// </summary>
    public Guid? DefaultWareId { get; set; }

    /// <summary>
    ///     统一社会信用代码。
    /// </summary>
    public string? UnifiedSocialCreditCode { get; set; }

    /// <summary>
    ///     法定代表人。
    /// </summary>
    public string? LegalRepresentative { get; set; }

    /// <summary>
    ///     注册资本。
    /// </summary>
    public string? RegisteredCapital { get; set; }

    /// <summary>
    ///     成立日期。
    /// </summary>
    [JsonConverter(typeof(FixedNullableDateTimeJsonConverter))]
    public DateTime? EstablishDate { get; set; }

    /// <summary>
    ///     营业期限。
    /// </summary>
    public string? BusinessTerm { get; set; }

    /// <summary>
    ///     登记状态。
    /// </summary>
    public string? RegistrationStatus { get; set; }

    /// <summary>
    ///     登记机关。
    /// </summary>
    public string? RegistrationAuthority { get; set; }

    /// <summary>
    ///     注册地址。
    /// </summary>
    public string? RegisteredAddress { get; set; }

    /// <summary>
    ///     经营范围。
    /// </summary>
    public string? BusinessScope { get; set; }

    /// <summary>
    ///     发票抬头名称。
    /// </summary>
    public string? InvoiceTitle { get; set; }

    /// <summary>
    ///     纳税人识别号。
    /// </summary>
    public string? TaxpayerIdentificationNumber { get; set; }

    /// <summary>
    ///     开票注册地址。
    /// </summary>
    public string? InvoiceAddress { get; set; }

    /// <summary>
    ///     开票电话。
    /// </summary>
    public string? InvoicePhone { get; set; }

    /// <summary>
    ///     开户银行。
    /// </summary>
    public string? BankName { get; set; }

    /// <summary>
    ///     银行账号。
    /// </summary>
    public string? BankAccount { get; set; }

    /// <summary>
    ///     发票收件人。
    /// </summary>
    public string? InvoiceReceiverName { get; set; }

    /// <summary>
    ///     发票收件电话。
    /// </summary>
    public string? InvoiceReceiverPhone { get; set; }

    /// <summary>
    ///     发票收件地址。
    /// </summary>
    public string? InvoiceReceiverAddress { get; set; }

    /// <summary>
    ///     发票邮箱。
    /// </summary>
    public string? InvoiceEmail { get; set; }

    /// <summary>
    ///     联系人。
    /// </summary>
    public string? ContactName { get; set; }

    /// <summary>
    ///     联系电话。
    /// </summary>
    public string? ContactPhone { get; set; }

    /// <summary>
    ///     客户地址。
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    ///     备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    ///     所属公司名称。
    /// </summary>
    public string? CompanyName { get; set; }

    /// <summary>
    ///     默认报价单名称。
    /// </summary>
    public string? QuotationName { get; set; }

    /// <summary>
    ///     默认仓库名称。
    /// </summary>
    public string? DefaultWareName { get; set; }

    /// <summary>
    ///     客户标签 ID。
    /// </summary>
    public List<Guid>? TagIds { get; set; }
}

