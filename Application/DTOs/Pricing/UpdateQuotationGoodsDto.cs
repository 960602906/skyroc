using Application.DTOs.Goods;
namespace Application.DTOs.Pricing;

/// <summary>
///     更新报价商品 DTO。
/// </summary>
public class UpdateQuotationGoodsDto : CreateQuotationGoodsDto, IHasId
{
    /// <summary>
    ///     主键 ID。
    /// </summary>
    public Guid Id { get; set; }
}

