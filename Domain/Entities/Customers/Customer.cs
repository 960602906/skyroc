using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;

namespace Domain.Entities.Customers;

/// <summary>
/// 客户实体，维护客户工商主体、开票信息、默认仓库和价格体系绑定。
/// </summary>
public class Customer : BaseEntity
{
    /// <summary>
    /// 客户名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 客户编码。
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 所属公司 ID。
    /// </summary>
    public Guid? CompanyId { get; set; }

    /// <summary>
    /// 默认报价单 ID。
    /// </summary>
    public Guid? QuotationId { get; set; }

    /// <summary>
    /// 默认仓库 ID。
    /// </summary>
    public Guid? DefaultWareId { get; set; }

    /// <summary>
    /// 统一社会信用代码。
    /// </summary>
    public string? UnifiedSocialCreditCode { get; set; }

    /// <summary>
    /// 法定代表人。
    /// </summary>
    public string? LegalRepresentative { get; set; }

    /// <summary>
    /// 注册资本。
    /// </summary>
    public string? RegisteredCapital { get; set; }

    /// <summary>
    /// 成立日期。
    /// </summary>
    public DateTime? EstablishDate { get; set; }

    /// <summary>
    /// 营业期限。
    /// </summary>
    public string? BusinessTerm { get; set; }

    /// <summary>
    /// 登记状态，例如存续、注销、吊销。
    /// </summary>
    public string? RegistrationStatus { get; set; }

    /// <summary>
    /// 登记机关。
    /// </summary>
    public string? RegistrationAuthority { get; set; }

    /// <summary>
    /// 注册地址。
    /// </summary>
    public string? RegisteredAddress { get; set; }

    /// <summary>
    /// 经营范围。
    /// </summary>
    public string? BusinessScope { get; set; }

    /// <summary>
    /// 发票抬头名称。
    /// </summary>
    public string? InvoiceTitle { get; set; }

    /// <summary>
    /// 纳税人识别号。
    /// </summary>
    public string? TaxpayerIdentificationNumber { get; set; }

    /// <summary>
    /// 开票注册地址。
    /// </summary>
    public string? InvoiceAddress { get; set; }

    /// <summary>
    /// 开票电话。
    /// </summary>
    public string? InvoicePhone { get; set; }

    /// <summary>
    /// 开户银行。
    /// </summary>
    public string? BankName { get; set; }

    /// <summary>
    /// 银行账号。
    /// </summary>
    public string? BankAccount { get; set; }

    /// <summary>
    /// 发票收件人。
    /// </summary>
    public string? InvoiceReceiverName { get; set; }

    /// <summary>
    /// 发票收件电话。
    /// </summary>
    public string? InvoiceReceiverPhone { get; set; }

    /// <summary>
    /// 发票收件地址。
    /// </summary>
    public string? InvoiceReceiverAddress { get; set; }

    /// <summary>
    /// 发票邮箱。
    /// </summary>
    public string? InvoiceEmail { get; set; }

    /// <summary>
    /// 联系人。
    /// </summary>
    public string? ContactName { get; set; }

    /// <summary>
    /// 联系电话。
    /// </summary>
    public string? ContactPhone { get; set; }

    /// <summary>
    /// 客户地址。
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// 备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 所属公司。
    /// </summary>
    public virtual Company? Company { get; set; }

    /// <summary>
    /// 默认报价单。
    /// </summary>
    public virtual Quotation? Quotation { get; set; }

    /// <summary>
    /// 默认仓库。
    /// </summary>
    public virtual Ware? DefaultWare { get; set; }

    /// <summary>
    /// 客户标签关系集合。
    /// </summary>
    public virtual ICollection<CustomerTagRelation> TagRelations { get; set; } = new List<CustomerTagRelation>();

    /// <summary>
    /// 客户绑定的报价单集合。
    /// </summary>
    public virtual ICollection<CustomerQuotation> CustomerQuotations { get; set; } = new List<CustomerQuotation>();

    /// <summary>
    /// 客户绑定的协议价集合。
    /// </summary>
    public virtual ICollection<CustomerProtocolCustomer> CustomerProtocolCustomers { get; set; } = new List<CustomerProtocolCustomer>();

    /// <summary>
    /// 客户子账号集合。
    /// </summary>
    public virtual ICollection<CustomerSubAccount> SubAccounts { get; set; } = new List<CustomerSubAccount>();

    /// <summary>
    /// 客户关联的采购规则集合。
    /// </summary>
    public virtual ICollection<PurchaseRuleCustomer> PurchaseRuleCustomers { get; set; } = new List<PurchaseRuleCustomer>();
}
