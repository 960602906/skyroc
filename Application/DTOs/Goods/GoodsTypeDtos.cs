namespace Application.DTOs.Goods;

/// <summary>
///     商品分类 DTO。
/// </summary>
public class GoodsTypeDto : BaseDto, ITreeNodeDto<GoodsTypeDto>
{
    /// <summary>
    ///     分类名称。
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///     分类编码。
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    ///     父级分类 ID。
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    ///     分类图片地址。
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    ///     税收分类编码。
    /// </summary>
    public string? TaxCategoryCode { get; set; }

    /// <summary>
    ///     税收分类名称。
    /// </summary>
    public string? TaxCategoryName { get; set; }

    /// <summary>
    ///     开票商品简称。
    /// </summary>
    public string? InvoiceGoodsShortName { get; set; }

    /// <summary>
    ///     默认增值税税率。
    /// </summary>
    public decimal? DefaultTaxRate { get; set; }

    /// <summary>
    ///     是否免税分类。
    /// </summary>
    public bool IsTaxExempt { get; set; }

    /// <summary>
    ///     免税或零税率政策依据。
    /// </summary>
    public string? TaxPolicyBasis { get; set; }

    /// <summary>
    ///     排序值。
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    ///     备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    ///     子分类。
    /// </summary>
    public List<GoodsTypeDto>? Children { get; set; }
}

/// <summary>
///     创建商品分类 DTO。
/// </summary>
public class CreateGoodsTypeDto : CreateNamedCodeDto
{
    /// <summary>
    ///     父级分类 ID。
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    ///     分类图片地址。
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    ///     税收分类编码。
    /// </summary>
    public string? TaxCategoryCode { get; set; }

    /// <summary>
    ///     税收分类名称。
    /// </summary>
    public string? TaxCategoryName { get; set; }

    /// <summary>
    ///     开票商品简称。
    /// </summary>
    public string? InvoiceGoodsShortName { get; set; }

    /// <summary>
    ///     默认增值税税率。
    /// </summary>
    public decimal? DefaultTaxRate { get; set; }

    /// <summary>
    ///     是否免税分类。
    /// </summary>
    public bool IsTaxExempt { get; set; }

    /// <summary>
    ///     免税或零税率政策依据。
    /// </summary>
    public string? TaxPolicyBasis { get; set; }

    /// <summary>
    ///     排序值。
    /// </summary>
    public int Sort { get; set; }
}

/// <summary>
///     更新商品分类 DTO。
/// </summary>
public class UpdateGoodsTypeDto : CreateGoodsTypeDto, IUpdateInput
{
    /// <summary>
    ///     主键 ID。
    /// </summary>
    public Guid Id { get; set; }
}
