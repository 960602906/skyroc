namespace Application.DTOs.Menu;

public class MenuTreeDto : MenuDto
{
    /// <summary>
    ///     子菜单集合
    /// </summary>
    public IEnumerable<MenuTreeDto>? Children { get; set; }
}