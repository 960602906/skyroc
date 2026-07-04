namespace Application.DTOs.Delivery;

/// <summary>
///     更新司机 DTO。
/// </summary>
public class UpdateDriverDto : CreateDriverDto, IUpdateInput
{
    /// <summary>
    ///     主键 ID。
    /// </summary>
    public Guid Id { get; set; }
}
