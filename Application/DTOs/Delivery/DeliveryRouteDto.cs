namespace Application.DTOs.Delivery;

/// <summary>
///     配送路线 DTO。
/// </summary>
public class DeliveryRouteDto : BaseDto
{
    /// <summary>
    ///     路线名称。
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///     路线编码。
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    ///     路线描述。
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///     排序值，数值越小越靠前。
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    ///     备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    ///     路线覆盖的客户 ID 集合。
    /// </summary>
    public List<Guid>? CustomerIds { get; set; }
}
