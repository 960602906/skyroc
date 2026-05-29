using Domain.Entities.Goods;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace Domain.Entities.Pricing;

/// <summary>
/// 客户协议价商品实体，记录协议内商品的特殊价格。
/// </summary>
public class CustomerProtocolGoods : BaseEntity
{
    /// <summary>
    /// 客户协议价 ID。
    /// </summary>
    public Guid CustomerProtocolId { get; set; }

    /// <summary>
    /// 商品 ID。
    /// </summary>
    public Guid GoodsId { get; set; }

    /// <summary>
    /// 协议价单位 ID。
    /// </summary>
    public Guid GoodsUnitId { get; set; }

    /// <summary>
    /// 协议单价。
    /// </summary>
    public decimal ProtocolPrice { get; set; }

    /// <summary>
    /// 最小起订数量。
    /// </summary>
    public decimal? MinOrderQuantity { get; set; }

    /// <summary>
    /// 备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 所属客户协议价。
    /// </summary>
    public virtual CustomerProtocol CustomerProtocol { get; set; } = null!;

    /// <summary>
    /// 商品。
    /// </summary>
    public virtual GoodsEntity Goods { get; set; } = null!;

    /// <summary>
    /// 协议价单位。
    /// </summary>
    public virtual GoodsUnit GoodsUnit { get; set; } = null!;
}
