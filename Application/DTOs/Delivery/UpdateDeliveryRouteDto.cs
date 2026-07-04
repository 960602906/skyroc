namespace Application.DTOs.Delivery;

/// <summary>
///     更新配送路线 DTO。
/// </summary>
public class UpdateDeliveryRouteDto : CreateDeliveryRouteDto, IUpdateInput
{
    /// <summary>
    ///     主键 ID。
    /// </summary>
    public Guid Id { get; set; }
}
