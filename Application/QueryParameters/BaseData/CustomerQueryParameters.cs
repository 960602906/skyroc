using System.Linq.Expressions;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using Shared.Constants;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace Application.QueryParameters;

/// <summary>
///     客户查询参数。
/// </summary>
public class CustomerQueryParameters : NamedCodeQueryParameters
{
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
    ///     纳税人识别号。
    /// </summary>
    public string? TaxpayerIdentificationNumber { get; set; }

    /// <summary>
    ///     统一社会信用代码。
    /// </summary>
    public string? UnifiedSocialCreditCode { get; set; }

    /// <summary>
    ///     构建查询表达式。
    /// </summary>
    public Expression<Func<Customer, bool>> QueryBuild()
    {
        return x =>
            (string.IsNullOrWhiteSpace(Name) || x.Name.Contains(Name.Trim())) &&
            (string.IsNullOrWhiteSpace(Code) || x.Code.Contains(Code.Trim())) &&
            (!CompanyId.HasValue || x.CompanyId == CompanyId.Value) &&
            (!QuotationId.HasValue || x.QuotationId == QuotationId.Value) &&
            (!DefaultWareId.HasValue || x.DefaultWareId == DefaultWareId.Value) &&
            (string.IsNullOrWhiteSpace(TaxpayerIdentificationNumber) || (x.TaxpayerIdentificationNumber != null && x.TaxpayerIdentificationNumber.Contains(TaxpayerIdentificationNumber.Trim()))) &&
            (string.IsNullOrWhiteSpace(UnifiedSocialCreditCode) || (x.UnifiedSocialCreditCode != null && x.UnifiedSocialCreditCode.Contains(UnifiedSocialCreditCode.Trim()))) &&
            (!Status.HasValue || x.Status == Status.Value);
    }
}

