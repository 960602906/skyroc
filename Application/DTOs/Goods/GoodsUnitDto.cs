using Shared.Constants;

namespace Application.DTOs.Goods;

/// <summary>
///     商品单位 DTO。
/// </summary>
public class GoodsUnitDto : BaseDto
{
    /// <summary>
    ///     商品 ID。
    /// </summary>
    public Guid GoodsId { get; set; }

    /// <summary>
    ///     单位名称。
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///     单位编码。
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    ///     相对基础单位的换算比例。
    /// </summary>
    public decimal ConversionRate { get; set; }

    /// <summary>
    ///     是否基础单位。
    /// </summary>
    public bool IsBaseUnit { get; set; }

    /// <summary>
    ///     排序值。
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    ///     备注。
    /// </summary>
    public string? Remark { get; set; }
}

