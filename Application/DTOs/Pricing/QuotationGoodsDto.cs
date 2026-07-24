using Application.DTOs.Goods;
namespace Application.DTOs.Pricing;

/// <summary>
///     报价商品 DTO。
/// </summary>
public class QuotationGoodsDto : BaseDto
{
    /// <summary>
    ///     报价单 ID。
    /// </summary>
    public Guid QuotationId { get; set; }

    /// <summary>
    ///     商品 ID。
    /// </summary>
    public Guid GoodsId { get; set; }

    /// <summary>
    ///     报价单位 ID。
    /// </summary>
    public Guid GoodsUnitId { get; set; }

    /// <summary>
    ///     销售单价。
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    ///     最小起订数量。
    /// </summary>
    public decimal? MinOrderQuantity { get; set; }

    /// <summary>
    ///     是否在报价单内上架。
    /// </summary>
    public bool IsOnSale { get; set; }

    /// <summary>
    ///     备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    ///     商品名称。
    /// </summary>
    public string? GoodsName { get; set; }

    /// <summary>
    ///     商品编码。
    /// </summary>
    public string? GoodsCode { get; set; }

    /// <summary>
    ///     报价单位名称。
    /// </summary>
    public string? GoodsUnitName { get; set; }
}

