using Domain.Entities.Purchases;

namespace Domain.Entities.Goods;

/// <summary>
/// 商品供应商关系实体，表示商品可由哪些供应商供货。
/// </summary>
public class GoodsSupplierRelation
{
    /// <summary>
    /// 商品 ID。
    /// </summary>
    public Guid GoodsId { get; set; }

    /// <summary>
    /// 供应商 ID。
    /// </summary>
    public Guid SupplierId { get; set; }

    /// <summary>
    /// 是否默认供应商。
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// 商品。
    /// </summary>
    public virtual Goods Goods { get; set; } = null!;

    /// <summary>
    /// 供应商。
    /// </summary>
    public virtual Supplier Supplier { get; set; } = null!;
}
