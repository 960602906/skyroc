namespace Application.DTOs.Delivery;

/// <summary>
///     创建司机 DTO。
/// </summary>
public class CreateDriverDto : CreateNamedCodeDto
{
    /// <summary>
    ///     联系电话。
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    ///     所属承运商 ID；自营司机可为空。
    /// </summary>
    public Guid? CarrierId { get; set; }

    /// <summary>
    ///     车牌号。
    /// </summary>
    public string? PlateNumber { get; set; }

    /// <summary>
    ///     驾驶证号。
    /// </summary>
    public string? LicenseNo { get; set; }
}
