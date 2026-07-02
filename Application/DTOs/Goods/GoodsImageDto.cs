namespace Application.DTOs.Goods;

/// <summary>
///     商品图片 DTO。
/// </summary>
public class GoodsImageDto : BaseDto
{
    /// <summary>
    ///     商品 ID。
    /// </summary>
    public Guid GoodsId { get; set; }

    /// <summary>
    ///     图片访问地址。
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    ///     原始文件名。
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    ///     排序值。
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    ///     是否主图。
    /// </summary>
    public bool IsPrimary { get; set; }
}

