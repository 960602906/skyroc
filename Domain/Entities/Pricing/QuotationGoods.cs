using Domain.Entities.Goods;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace Domain.Entities.Pricing;

/// <summary>
/// 报价商品实体，记录报价单内商品、单位和销售单价。
/// </summary>
public class QuotationGoods : BaseEntity
{
    /// <summary>
    /// 报价单 ID。
    /// </summary>
    public Guid QuotationId { get; set; }

    /// <summary>
    /// 商品 ID。
    /// </summary>
    public Guid GoodsId { get; set; }

    /// <summary>
    /// 报价单位 ID。
    /// </summary>
    public Guid GoodsUnitId { get; set; }

    /// <summary>
    /// 销售单价。
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// 最小起订数量。
    /// </summary>
    public decimal? MinOrderQuantity { get; set; }

    /// <summary>
    /// 是否在报价单内上架。
    /// </summary>
    public bool IsOnSale { get; set; } = true;

    /// <summary>
    /// 备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 所属报价单。
    /// </summary>
    public virtual Quotation Quotation { get; set; } = null!;

    /// <summary>
    /// 商品。
    /// </summary>
    public virtual GoodsEntity Goods { get; set; } = null!;

    /// <summary>
    /// 报价单位。
    /// </summary>
    public virtual GoodsUnit GoodsUnit { get; set; } = null!;
}
