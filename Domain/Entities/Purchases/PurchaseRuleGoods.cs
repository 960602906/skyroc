using Domain.Entities.Goods;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace Domain.Entities.Purchases;

/// <summary>
/// 采购规则与商品的绑定关系实体。
/// </summary>
public class PurchaseRuleGoods
{
    /// <summary>
    /// 采购规则 ID。
    /// </summary>
    public Guid PurchaseRuleId { get; set; }

    /// <summary>
    /// 商品 ID。
    /// </summary>
    public Guid GoodsId { get; set; }

    /// <summary>
    /// 采购规则。
    /// </summary>
    public virtual PurchaseRule PurchaseRule { get; set; } = null!;

    /// <summary>
    /// 商品。
    /// </summary>
    public virtual GoodsEntity Goods { get; set; } = null!;
}
