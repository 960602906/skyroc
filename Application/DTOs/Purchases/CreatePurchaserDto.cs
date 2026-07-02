namespace Application.DTOs.Purchases;

/// <summary>
///     创建采购员 DTO。
/// </summary>
public class CreatePurchaserDto : CreateNamedCodeDto
{
    /// <summary>
    ///     联系电话。
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    ///     关联系统用户 ID。
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    ///     所属部门 ID。
    /// </summary>
    public Guid? DepartmentId { get; set; }
}

