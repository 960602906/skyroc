namespace Application.DTOs.Goods;

/// <summary>
///     更新商品档案 DTO。
/// </summary>
public class UpdateGoodsDto : CreateGoodsDto, IUpdateInput
{
    /// <summary>
    ///     主键 ID。
    /// </summary>
    public Guid Id { get; set; }
}

