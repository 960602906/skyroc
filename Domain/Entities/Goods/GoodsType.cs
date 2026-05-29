namespace Domain.Entities.Goods;

/// <summary>
/// 商品分类实体，支持树形分类结构，并承载税务局生鲜分类和默认开票税率。
/// </summary>
public class GoodsType : BaseEntity
{
    /// <summary>
    /// 分类名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 分类编码。
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 父级分类 ID。
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// 分类图片地址。
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// 税收分类编码。
    /// </summary>
    public string? TaxCategoryCode { get; set; }

    /// <summary>
    /// 税收分类名称。
    /// </summary>
    public string? TaxCategoryName { get; set; }

    /// <summary>
    /// 开票商品简称，例如 *蔬菜*、*水果*。
    /// </summary>
    public string? InvoiceGoodsShortName { get; set; }

    /// <summary>
    /// 默认增值税税率。
    /// </summary>
    public decimal? DefaultTaxRate { get; set; }

    /// <summary>
    /// 是否免税分类。
    /// </summary>
    public bool IsTaxExempt { get; set; }

    /// <summary>
    /// 免税或零税率政策依据。
    /// </summary>
    public string? TaxPolicyBasis { get; set; }

    /// <summary>
    /// 排序值。
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 父级分类。
    /// </summary>
    public virtual GoodsType? Parent { get; set; }

    /// <summary>
    /// 子级分类集合。
    /// </summary>
    public virtual ICollection<GoodsType> Children { get; set; } = new List<GoodsType>();

    /// <summary>
    /// 分类下的商品集合。
    /// </summary>
    public virtual ICollection<Goods> Goods { get; set; } = new List<Goods>();
}
