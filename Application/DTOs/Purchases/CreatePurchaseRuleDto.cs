namespace Application.DTOs.Purchases;

/// <summary>
///     创建采购规则 DTO。
/// </summary>
public class CreatePurchaseRuleDto : CreateNamedCodeDto
{
    /// <summary>
    ///     默认供应商 ID。
    /// </summary>
    public Guid? SupplierId { get; set; }

    /// <summary>
    ///     默认采购员 ID。
    /// </summary>
    public Guid? PurchaserId { get; set; }

    /// <summary>
    ///     适用仓库 ID。
    /// </summary>
    public Guid? WareId { get; set; }

    /// <summary>
    ///     适用商品分类 ID。
    /// </summary>
    public Guid? GoodsTypeId { get; set; }

    /// <summary>
    ///     采购模式，通常 1 为供应商直供，2 为市场自采。
    /// </summary>
    public int PurchasePattern { get; set; } = 1;

    /// <summary>
    ///     适用商品 ID。
    /// </summary>
    public List<Guid>? GoodsIds { get; set; }

    /// <summary>
    ///     适用客户 ID。
    /// </summary>
    public List<Guid>? CustomerIds { get; set; }
}

