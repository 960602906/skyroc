using Shared.Constants;

namespace Application.DTOs.Goods;

/// <summary>
///     更新商品单位 DTO。
/// </summary>
public class UpdateGoodsUnitDto : CreateGoodsUnitDto, IHasId
{
    /// <summary>
    ///     主键 ID。
    /// </summary>
    public Guid Id { get; set; }
}

