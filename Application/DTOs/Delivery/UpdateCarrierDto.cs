namespace Application.DTOs.Delivery;

/// <summary>
///     更新承运商 DTO。
/// </summary>
public class UpdateCarrierDto : CreateCarrierDto, IUpdateInput
{
    /// <summary>
    ///     主键 ID。
    /// </summary>
    public Guid Id { get; set; }
}
