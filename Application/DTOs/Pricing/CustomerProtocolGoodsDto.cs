namespace Application.DTOs.Pricing;

/// <summary>
///     客户协议价商品 DTO。
/// </summary>
public class CustomerProtocolGoodsDto : BaseDto
{
    /// <summary>
    ///     客户协议价 ID。
    /// </summary>
    public Guid CustomerProtocolId { get; set; }

    /// <summary>
    ///     商品 ID。
    /// </summary>
    public Guid GoodsId { get; set; }

    /// <summary>
    ///     协议价单位 ID。
    /// </summary>
    public Guid GoodsUnitId { get; set; }

    /// <summary>
    ///     协议单价。
    /// </summary>
    public decimal ProtocolPrice { get; set; }

    /// <summary>
    ///     最小起订数量。
    /// </summary>
    public decimal? MinOrderQuantity { get; set; }

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
    ///     协议价单位名称。
    /// </summary>
    public string? GoodsUnitName { get; set; }
}

