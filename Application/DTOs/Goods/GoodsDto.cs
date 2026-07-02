namespace Application.DTOs.Goods;

/// <summary>
///     商品档案 DTO。
/// </summary>
public class GoodsDto : BaseDto
{
    /// <summary>
    ///     商品名称。
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///     商品编码。
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    ///     商品分类 ID。
    /// </summary>
    public Guid GoodsTypeId { get; set; }

    /// <summary>
    ///     基础单位 ID。
    /// </summary>
    public Guid? BaseUnitId { get; set; }

    /// <summary>
    ///     默认供应商 ID。
    /// </summary>
    public Guid? DefaultSupplierId { get; set; }

    /// <summary>
    ///     默认仓库 ID。
    /// </summary>
    public Guid? DefaultWareId { get; set; }

    /// <summary>
    ///     商品规格。
    /// </summary>
    public string? Spec { get; set; }

    /// <summary>
    ///     商品品牌。
    /// </summary>
    public string? Brand { get; set; }

    /// <summary>
    ///     商品产地。
    /// </summary>
    public string? Origin { get; set; }

    /// <summary>
    ///     商品描述。
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///     商品税率。
    /// </summary>
    public decimal? TaxRate { get; set; }

    /// <summary>
    ///     是否上架销售。
    /// </summary>
    public bool IsOnSale { get; set; }

    /// <summary>
    ///     备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    ///     商品分类名称。
    /// </summary>
    public string? GoodsTypeName { get; set; }

    /// <summary>
    ///     基础单位名称。
    /// </summary>
    public string? BaseUnitName { get; set; }

    /// <summary>
    ///     默认供应商名称。
    /// </summary>
    public string? DefaultSupplierName { get; set; }

    /// <summary>
    ///     默认仓库名称。
    /// </summary>
    public string? DefaultWareName { get; set; }

    /// <summary>
    ///     商品单位列表。
    /// </summary>
    public List<GoodsUnitDto>? Units { get; set; }

    /// <summary>
    ///     商品图片列表。
    /// </summary>
    public List<GoodsImageDto>? Images { get; set; }

    /// <summary>
    ///     可供货供应商 ID。
    /// </summary>
    public List<Guid>? SupplierIds { get; set; }
}

