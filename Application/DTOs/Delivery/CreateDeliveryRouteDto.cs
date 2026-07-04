namespace Application.DTOs.Delivery;

/// <summary>
///     创建配送路线 DTO。
/// </summary>
public class CreateDeliveryRouteDto : CreateNamedCodeDto
{
    /// <summary>
    ///     路线描述。
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///     排序值，数值越小越靠前。
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    ///     初始分配到该路线的客户 ID 集合；为空表示不分配客户。
    /// </summary>
    public List<Guid>? CustomerIds { get; set; }
}
