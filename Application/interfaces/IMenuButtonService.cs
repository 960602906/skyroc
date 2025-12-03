using Application.DTOs.MenuButton;

namespace Application.interfaces;

public interface IMenuButtonService
{
    /// <summary>
    ///     根据Id获取菜单按钮
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<MenuButtonDto> GetMenuButtonAsync(Guid id);

    /// <summary>
    ///     创建菜单按钮
    /// </summary>
    /// <param name="menuButton"></param>
    /// <returns></returns>
    Task<MenuButtonDto> CreateMenuButtonAsync(CreateMenuButtonDto menuButton);

    /// <summary>
    ///     批量创建菜单按钮
    /// </summary>
    Task<IEnumerable<MenuButtonDto>> CreateMenuButtonsAsync(Guid menuId, IEnumerable<CreateMenuButtonDto> menuButtons);

    /// <summary>
    ///     更新菜单按钮
    /// </summary>
    /// <param name="menuId"></param>
    /// <param name="menuButton"></param>
    /// <returns></returns>
    Task<MenuButtonDto> UpdateMenuButtonAsync(Guid menuId, UpdateMenuButtonDto menuButton);

    /// <summary>
    ///     批量更新菜单按钮
    /// </summary>
    /// <param name="menuId"></param>
    /// <param name="menuButtons"></param>
    /// <returns></returns>
    Task<IEnumerable<MenuButtonDto>> UpdateMenuButtonsAsync(Guid menuId, IEnumerable<UpdateMenuButtonDto> menuButtons);

    /// <summary>
    ///     删除菜单按钮
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task DeleteMenuButtonAsync(Guid id);
}