namespace Domain.Entities.Goods;

/// <summary>
/// 商品图片实体，保存商品多图及主图信息。
/// </summary>
public class GoodsImage : BaseEntity
{
    /// <summary>
    /// 商品 ID。
    /// </summary>
    public Guid GoodsId { get; set; }

    /// <summary>
    /// 图片访问地址。
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// 原始文件名。
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// 排序值。
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 是否主图。
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// 所属商品。
    /// </summary>
    public virtual Goods Goods { get; set; } = null!;
}
