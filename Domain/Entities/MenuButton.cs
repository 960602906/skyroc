namespace Domain.Entities;

/// <summary>
///     菜单按钮实体
/// </summary>
public class MenuButton : BaseEntity
{
    /// <summary>
    ///     按钮编码 (唯一标识，用于权限控制)
    /// </summary>
    public required string Code { get; set; }

    /// <summary>
    ///     按钮描述
    /// </summary>
    public string? Desc { get; set; }

    /// <summary>
    ///     所属菜单Id
    /// </summary>
    public required Guid MenuId { get; set; }

    /// <summary>
    ///     所属菜单
    /// </summary>
    public required Menu Menu { get; set; }
}