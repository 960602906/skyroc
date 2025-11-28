namespace Application.DTOs.Menu;

public class MenuTreeDto : MenuDto
{
    /// <summary>
    ///     父菜单ID (支持无限级菜单)
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    ///     子菜单集合
    /// </summary>
    public IEnumerable<MenuTreeDto>? Children { get; set; }
}