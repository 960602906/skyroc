namespace Application.DTOs.Purchases;

/// <summary>
///     采购员 DTO。
/// </summary>
public class PurchaserDto : BaseDto
{
    /// <summary>
    ///     采购员名称。
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///     采购员编码。
    /// </summary>
    public string? Code { get; set; }

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

    /// <summary>
    ///     备注。
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    ///     关联系统用户名。
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    ///     所属部门名称。
    /// </summary>
    public string? DepartmentName { get; set; }
}

