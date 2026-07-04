namespace Application.DTOs.Delivery;

/// <summary>
///     司机 DTO。
/// </summary>
public class DriverDto : BaseDto
{
    /// <summary>
    ///     司机姓名。
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///     司机编码。
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    ///     联系电话。
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    ///     所属承运商 ID；自营司机为空。
    /// </summary>
    public Guid? CarrierId { get; set; }

    /// <summary>
    ///     所属承运商名称。
    /// </summary>
    public string? CarrierName { get; set; }

    /// <summary>
    ///     车牌号。
    /// </summary>
    public string? PlateNumber { get; set; }

    /// <summary>
    ///     驾驶证号。
    /// </summary>
    public string? LicenseNo { get; set; }

    /// <summary>
    ///     备注。
    /// </summary>
    public string? Remark { get; set; }
}
