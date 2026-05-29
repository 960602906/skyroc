using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Purchases;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace Domain.Entities.Storage;

/// <summary>
/// 仓库实体，维护库存业务使用的仓库基础资料。
/// </summary>
public class Ware : BaseEntity
{
    /// <summary>
    /// 仓库名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 仓库编码。
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
    /// 仓库地址。
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// 排序值。
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 将该仓库作为默认仓库的商品集合。
    /// </summary>
    public virtual ICollection<GoodsEntity> DefaultGoods { get; set; } = new List<GoodsEntity>();

    /// <summary>
    /// 将该仓库作为默认仓库的客户集合。
    /// </summary>
    public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();

    /// <summary>
    /// 仓库关联的采购规则集合。
    /// </summary>
    public virtual ICollection<PurchaseRule> PurchaseRules { get; set; } = new List<PurchaseRule>();
}
