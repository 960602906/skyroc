using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Storage;

namespace Domain.Entities.Purchases;

/// <summary>
/// 采购规则实体，用于按客户、商品、分类等条件指定供应商、采购员和采购模式。
/// </summary>
public class PurchaseRule : BaseEntity
{
    /// <summary>
    /// 规则名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 规则编码。
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 默认供应商 ID。
    /// </summary>
    public Guid? SupplierId { get; set; }

    /// <summary>
    /// 默认采购员 ID。
    /// </summary>
    public Guid? PurchaserId { get; set; }

    /// <summary>
    /// 适用仓库 ID。
    /// </summary>
    public Guid? WareId { get; set; }

    /// <summary>
    /// 适用商品分类 ID。
    /// </summary>
    public Guid? GoodsTypeId { get; set; }

    /// <summary>
    /// 采购模式，通常 1 为供应商直供，2 为市场自采。
    /// </summary>
    public int PurchasePattern { get; set; }

    /// <summary>
    /// 备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 默认供应商。
    /// </summary>
    public virtual Supplier? Supplier { get; set; }

    /// <summary>
    /// 默认采购员。
    /// </summary>
    public virtual Purchaser? Purchaser { get; set; }

    /// <summary>
    /// 适用仓库。
    /// </summary>
    public virtual Ware? Ware { get; set; }

    /// <summary>
    /// 适用商品分类。
    /// </summary>
    public virtual GoodsType? GoodsType { get; set; }

    /// <summary>
    /// 适用商品关系集合。
    /// </summary>
    public virtual ICollection<PurchaseRuleGoods> Goods { get; set; } = new List<PurchaseRuleGoods>();

    /// <summary>
    /// 适用客户关系集合。
    /// </summary>
    public virtual ICollection<PurchaseRuleCustomer> Customers { get; set; } = new List<PurchaseRuleCustomer>();
}
