namespace Application.DTOs.Delivery;

/// <summary>
///     配送路线客户分配请求，用给定集合整体替换路线的客户关系。
/// </summary>
public class DispatchRouteCustomersDto
{
    /// <summary>
    ///     目标客户 ID 集合；为空表示清空该路线的所有客户关系。
    /// </summary>
    public List<Guid>? CustomerIds { get; set; }
}
