namespace Application.DTOs.Delivery;

/// <summary>
///     创建承运商 DTO。
/// </summary>
public class CreateCarrierDto : CreateNamedCodeDto
{
    /// <summary>
    ///     联系人姓名。
    /// </summary>
    public string? ContactName { get; set; }

    /// <summary>
    ///     联系电话。
    /// </summary>
    public string? ContactPhone { get; set; }

    /// <summary>
    ///     承运商地址。
    /// </summary>
    public string? Address { get; set; }
}
