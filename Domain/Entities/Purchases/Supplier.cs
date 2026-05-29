using Domain.Entities.Goods;

namespace Domain.Entities.Purchases;

/// <summary>
/// 供应商实体，维护采购供货方基础资料。
/// </summary>
public class Supplier : BaseEntity
{
    /// <summary>
    /// 供应商名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 供应商编码。
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 联系人。
    /// </summary>
    public string? ContactName { get; set; }

    /// <summary>
    /// 联系电话。
    /// </summary>
    public string? ContactPhone { get; set; }

    /// <summary>
    /// 地址。
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// 开户行。
    /// </summary>
    public string? BankName { get; set; }

    /// <summary>
    /// 银行账号。
    /// </summary>
    public string? BankAccount { get; set; }

    /// <summary>
    /// 税号。
    /// </summary>
    public string? TaxNo { get; set; }

    /// <summary>
    /// 备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 可供货商品关系集合。
    /// </summary>
    public virtual ICollection<GoodsSupplierRelation> GoodsRelations { get; set; } = new List<GoodsSupplierRelation>();

    /// <summary>
    /// 供应商关联的采购规则集合。
    /// </summary>
    public virtual ICollection<PurchaseRule> PurchaseRules { get; set; } = new List<PurchaseRule>();
}
