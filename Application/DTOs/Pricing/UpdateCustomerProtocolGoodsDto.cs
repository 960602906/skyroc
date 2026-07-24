namespace Application.DTOs.Pricing;

/// <summary>
///     更新客户协议价商品 DTO。
/// </summary>
public class UpdateCustomerProtocolGoodsDto : CreateCustomerProtocolGoodsDto, IHasId
{
    /// <summary>
    ///     主键 ID。
    /// </summary>
    public Guid Id { get; set; }
}

