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
///     带名称、编码和状态的基础资料分页查询参数。
/// </summary>
public abstract class NamedCodeQueryParameters : PagedQueryParameters
{
    /// <summary>
    ///     名称，支持模糊查询。
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///     编码，支持模糊查询。
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    ///     启用禁用状态。
    /// </summary>
    public Status? Status { get; set; }
}

/// <summary>
///     商品分类查询参数。
/// </summary>
public class GoodsTypeQueryParameters : NamedCodeQueryParameters
{
    /// <summary>
    ///     父级分类 ID。
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    ///     税收分类编码。
    /// </summary>
    public string? TaxCategoryCode { get; set; }

    /// <summary>
    ///     构建查询表达式。
    /// </summary>
    public Expression<Func<GoodsType, bool>> QueryBuild()
    {
        return x =>
            (string.IsNullOrWhiteSpace(Name) || x.Name.Contains(Name.Trim())) &&
            (string.IsNullOrWhiteSpace(Code) || x.Code.Contains(Code.Trim())) &&
            (string.IsNullOrWhiteSpace(TaxCategoryCode) || (x.TaxCategoryCode != null && x.TaxCategoryCode.Contains(TaxCategoryCode.Trim()))) &&
            (!ParentId.HasValue || x.ParentId == ParentId.Value) &&
            (!Status.HasValue || x.Status == Status.Value);
    }
}

/// <summary>
///     商品查询参数。
/// </summary>
public class GoodsQueryParameters : NamedCodeQueryParameters
{
    /// <summary>
    ///     商品分类 ID。
    /// </summary>
    public Guid? GoodsTypeId { get; set; }

    /// <summary>
    ///     默认供应商 ID。
    /// </summary>
    public Guid? DefaultSupplierId { get; set; }

    /// <summary>
    ///     默认仓库 ID。
    /// </summary>
    public Guid? DefaultWareId { get; set; }

    /// <summary>
    ///     是否上架。
    /// </summary>
    public bool? IsOnSale { get; set; }

    /// <summary>
    ///     构建查询表达式。
    /// </summary>
    public Expression<Func<GoodsEntity, bool>> QueryBuild()
    {
        return x =>
            (string.IsNullOrWhiteSpace(Name) || x.Name.Contains(Name.Trim())) &&
            (string.IsNullOrWhiteSpace(Code) || x.Code.Contains(Code.Trim())) &&
            (!GoodsTypeId.HasValue || x.GoodsTypeId == GoodsTypeId.Value) &&
            (!DefaultSupplierId.HasValue || x.DefaultSupplierId == DefaultSupplierId.Value) &&
            (!DefaultWareId.HasValue || x.DefaultWareId == DefaultWareId.Value) &&
            (!IsOnSale.HasValue || x.IsOnSale == IsOnSale.Value) &&
            (!Status.HasValue || x.Status == Status.Value);
    }
}

/// <summary>
///     商品单位查询参数。
/// </summary>
public class GoodsUnitQueryParameters : PagedQueryParameters
{
    /// <summary>
    ///     商品 ID。
    /// </summary>
    public Guid? GoodsId { get; set; }

    /// <summary>
    ///     单位名称。
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///     启用禁用状态。
    /// </summary>
    public Status? Status { get; set; }

    /// <summary>
    ///     构建查询表达式。
    /// </summary>
    public Expression<Func<GoodsUnit, bool>> QueryBuild()
    {
        return x =>
            (!GoodsId.HasValue || x.GoodsId == GoodsId.Value) &&
            (string.IsNullOrWhiteSpace(Name) || x.Name.Contains(Name.Trim())) &&
            (!Status.HasValue || x.Status == Status.Value);
    }
}

/// <summary>
///     公司查询参数。
/// </summary>
public class CompanyQueryParameters : NamedCodeQueryParameters
{
    /// <summary>
    ///     构建查询表达式。
    /// </summary>
    public Expression<Func<Company, bool>> QueryBuild()
    {
        return x =>
            (string.IsNullOrWhiteSpace(Name) || x.Name.Contains(Name.Trim())) &&
            (string.IsNullOrWhiteSpace(Code) || x.Code.Contains(Code.Trim())) &&
            (!Status.HasValue || x.Status == Status.Value);
    }
}

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

/// <summary>
///     客户标签查询参数。
/// </summary>
public class CustomerTagQueryParameters : NamedCodeQueryParameters
{
    /// <summary>
    ///     父级标签 ID。
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    ///     构建查询表达式。
    /// </summary>
    public Expression<Func<CustomerTag, bool>> QueryBuild()
    {
        return x =>
            (string.IsNullOrWhiteSpace(Name) || x.Name.Contains(Name.Trim())) &&
            (string.IsNullOrWhiteSpace(Code) || x.Code.Contains(Code.Trim())) &&
            (!ParentId.HasValue || x.ParentId == ParentId.Value) &&
            (!Status.HasValue || x.Status == Status.Value);
    }
}

/// <summary>
///     客户子账号查询参数。
/// </summary>
public class CustomerSubAccountQueryParameters : PagedQueryParameters
{
    /// <summary>
    ///     所属公司 ID。
    /// </summary>
    public Guid? CompanyId { get; set; }

    /// <summary>
    ///     客户 ID。
    /// </summary>
    public Guid? CustomerId { get; set; }

    /// <summary>
    ///     登录账号。
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    ///     昵称。
    /// </summary>
    public string? NickName { get; set; }

    /// <summary>
    ///     启用禁用状态。
    /// </summary>
    public Status? Status { get; set; }

    /// <summary>
    ///     构建查询表达式。
    /// </summary>
    public Expression<Func<CustomerSubAccount, bool>> QueryBuild()
    {
        return x =>
            (!CompanyId.HasValue || x.CompanyId == CompanyId.Value) &&
            (!CustomerId.HasValue || x.CustomerId == CustomerId.Value) &&
            (string.IsNullOrWhiteSpace(Username) || x.Username.Contains(Username.Trim())) &&
            (string.IsNullOrWhiteSpace(NickName) || x.NickName.Contains(NickName.Trim())) &&
            (!Status.HasValue || x.Status == Status.Value);
    }
}

/// <summary>
///     供应商查询参数。
/// </summary>
public class SupplierQueryParameters : NamedCodeQueryParameters
{
    /// <summary>
    ///     构建查询表达式。
    /// </summary>
    public Expression<Func<Supplier, bool>> QueryBuild()
    {
        return x =>
            (string.IsNullOrWhiteSpace(Name) || x.Name.Contains(Name.Trim())) &&
            (string.IsNullOrWhiteSpace(Code) || x.Code.Contains(Code.Trim())) &&
            (!Status.HasValue || x.Status == Status.Value);
    }
}

/// <summary>
///     采购员查询参数。
/// </summary>
public class PurchaserQueryParameters : NamedCodeQueryParameters
{
    /// <summary>
    ///     所属部门 ID。
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    ///     构建查询表达式。
    /// </summary>
    public Expression<Func<Purchaser, bool>> QueryBuild()
    {
        return x =>
            (string.IsNullOrWhiteSpace(Name) || x.Name.Contains(Name.Trim())) &&
            (string.IsNullOrWhiteSpace(Code) || x.Code.Contains(Code.Trim())) &&
            (!DepartmentId.HasValue || x.DepartmentId == DepartmentId.Value) &&
            (!Status.HasValue || x.Status == Status.Value);
    }
}

/// <summary>
///     仓库查询参数。
/// </summary>
public class WareQueryParameters : NamedCodeQueryParameters
{
    /// <summary>
    ///     构建查询表达式。
    /// </summary>
    public Expression<Func<Ware, bool>> QueryBuild()
    {
        return x =>
            (string.IsNullOrWhiteSpace(Name) || x.Name.Contains(Name.Trim())) &&
            (string.IsNullOrWhiteSpace(Code) || x.Code.Contains(Code.Trim())) &&
            (!Status.HasValue || x.Status == Status.Value);
    }
}

/// <summary>
///     报价单查询参数。
/// </summary>
public class QuotationQueryParameters : NamedCodeQueryParameters
{
    /// <summary>
    ///     是否已审核。
    /// </summary>
    public bool? IsAudited { get; set; }

    /// <summary>
    ///     构建查询表达式。
    /// </summary>
    public Expression<Func<Quotation, bool>> QueryBuild()
    {
        return x =>
            (string.IsNullOrWhiteSpace(Name) || x.Name.Contains(Name.Trim())) &&
            (string.IsNullOrWhiteSpace(Code) || x.Code.Contains(Code.Trim())) &&
            (!IsAudited.HasValue || x.IsAudited == IsAudited.Value) &&
            (!Status.HasValue || x.Status == Status.Value);
    }
}

/// <summary>
///     报价商品查询参数。
/// </summary>
public class QuotationGoodsQueryParameters : PagedQueryParameters
{
    /// <summary>
    ///     报价单 ID。
    /// </summary>
    public Guid? QuotationId { get; set; }

    /// <summary>
    ///     商品 ID。
    /// </summary>
    public Guid? GoodsId { get; set; }

    /// <summary>
    ///     是否在报价单内上架。
    /// </summary>
    public bool? IsOnSale { get; set; }

    /// <summary>
    ///     启用禁用状态。
    /// </summary>
    public Status? Status { get; set; }

    /// <summary>
    ///     构建查询表达式。
    /// </summary>
    public Expression<Func<QuotationGoods, bool>> QueryBuild()
    {
        return x =>
            (!QuotationId.HasValue || x.QuotationId == QuotationId.Value) &&
            (!GoodsId.HasValue || x.GoodsId == GoodsId.Value) &&
            (!IsOnSale.HasValue || x.IsOnSale == IsOnSale.Value) &&
            (!Status.HasValue || x.Status == Status.Value);
    }
}

/// <summary>
///     客户协议价查询参数。
/// </summary>
public class CustomerProtocolQueryParameters : NamedCodeQueryParameters
{
    /// <summary>
    ///     关联报价单 ID。
    /// </summary>
    public Guid? QuotationId { get; set; }

    /// <summary>
    ///     构建查询表达式。
    /// </summary>
    public Expression<Func<CustomerProtocol, bool>> QueryBuild()
    {
        return x =>
            (string.IsNullOrWhiteSpace(Name) || x.Name.Contains(Name.Trim())) &&
            (string.IsNullOrWhiteSpace(Code) || x.Code.Contains(Code.Trim())) &&
            (!QuotationId.HasValue || x.QuotationId == QuotationId.Value) &&
            (!Status.HasValue || x.Status == Status.Value);
    }
}

/// <summary>
///     客户协议价商品查询参数。
/// </summary>
public class CustomerProtocolGoodsQueryParameters : PagedQueryParameters
{
    /// <summary>
    ///     客户协议价 ID。
    /// </summary>
    public Guid? CustomerProtocolId { get; set; }

    /// <summary>
    ///     商品 ID。
    /// </summary>
    public Guid? GoodsId { get; set; }

    /// <summary>
    ///     启用禁用状态。
    /// </summary>
    public Status? Status { get; set; }

    /// <summary>
    ///     构建查询表达式。
    /// </summary>
    public Expression<Func<CustomerProtocolGoods, bool>> QueryBuild()
    {
        return x =>
            (!CustomerProtocolId.HasValue || x.CustomerProtocolId == CustomerProtocolId.Value) &&
            (!GoodsId.HasValue || x.GoodsId == GoodsId.Value) &&
            (!Status.HasValue || x.Status == Status.Value);
    }
}

/// <summary>
///     采购规则查询参数。
/// </summary>
public class PurchaseRuleQueryParameters : NamedCodeQueryParameters
{
    /// <summary>
    ///     供应商 ID。
    /// </summary>
    public Guid? SupplierId { get; set; }

    /// <summary>
    ///     采购员 ID。
    /// </summary>
    public Guid? PurchaserId { get; set; }

    /// <summary>
    ///     仓库 ID。
    /// </summary>
    public Guid? WareId { get; set; }

    /// <summary>
    ///     商品分类 ID。
    /// </summary>
    public Guid? GoodsTypeId { get; set; }

    /// <summary>
    ///     采购模式。
    /// </summary>
    public int? PurchasePattern { get; set; }

    /// <summary>
    ///     构建查询表达式。
    /// </summary>
    public Expression<Func<PurchaseRule, bool>> QueryBuild()
    {
        return x =>
            (string.IsNullOrWhiteSpace(Name) || x.Name.Contains(Name.Trim())) &&
            (string.IsNullOrWhiteSpace(Code) || x.Code.Contains(Code.Trim())) &&
            (!SupplierId.HasValue || x.SupplierId == SupplierId.Value) &&
            (!PurchaserId.HasValue || x.PurchaserId == PurchaserId.Value) &&
            (!WareId.HasValue || x.WareId == WareId.Value) &&
            (!GoodsTypeId.HasValue || x.GoodsTypeId == GoodsTypeId.Value) &&
            (!PurchasePattern.HasValue || x.PurchasePattern == PurchasePattern.Value) &&
            (!Status.HasValue || x.Status == Status.Value);
    }
}
