namespace Application.DTOs.MenuButton;

public class UpdateMenuButtonDto
{
    public Guid Id { get; set; }

    /// <summary>
    ///     按钮编码
    /// </summary>
    public string? Code { get; init; }

    /// <summary>
    ///     按钮描述
    /// </summary>
    public string? Desc { get; init; }

    /// <summary>
    ///     关联的菜单id
    /// </summary>
    public Guid MenuId { get; init; }
}