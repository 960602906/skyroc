namespace Application.DTOs.Goods;

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

