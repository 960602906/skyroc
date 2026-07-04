namespace Application.DTOs.Delivery;

/// <summary>
///     承运商 DTO。
/// </summary>
public class CarrierDto : BaseDto
{
    /// <summary>
    ///     承运商名称。
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///     承运商编码。
    /// </summary>
    public string? Code { get; set; }

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

    /// <summary>
    ///     备注。
    /// </summary>
    public string? Remark { get; set; }
}
