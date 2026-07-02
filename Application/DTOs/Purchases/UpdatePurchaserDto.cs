namespace Application.DTOs.Purchases;

/// <summary>
///     更新采购员 DTO。
/// </summary>
public class UpdatePurchaserDto : CreatePurchaserDto, IUpdateInput
{
    /// <summary>
    ///     主键 ID。
    /// </summary>
    public Guid Id { get; set; }
}

