namespace Domain.Entities;

/// <summary>
///     菜单实体
///     支持树形结构 (多级菜单)
/// </summary>
public sealed class Menu : BaseEntity
{
    /// <summary>
    ///     路由名称
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    ///     路由路径
    /// </summary>
    public required string Path { get; set; }

    /// <summary>
    ///     只有第一级或最后一级路由才有该属性，作为布局组件或者页面组件
    /// </summary>
    public string? Component { get; set; }

    /// <summary>
    ///     菜单名称
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    ///     国际化键值
    /// </summary>
    public string? I18NKey { get; set; }

    /// <summary>
    ///     排序值 (值越小越靠前)
    /// </summary>
    public int? Order { get; set; }

    /// <summary>
    ///     缓存路由
    /// </summary>
    public bool KeepAlive { get; set; } = false;

    /// <summary>
    ///     常量路由
    /// </summary>
    public bool Constant { get; set; } = false;

    /// <summary>
    ///     菜单图标
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    ///     本地图标
    /// </summary>
    public string? LocalIcon { get; set; }

    /// <summary>
    ///     路由的外部链接。如果设置，点击菜单时会跳转到外部链接而不是路由路径
    /// </summary>
    public string? Href { get; set; }

    /// <summary>
    ///     是否隐藏 (隐藏的菜单不在菜单栏显示，但权限仍然有效)
    /// </summary>
    public bool HideInMenu { get; set; }

    /// <summary>
    ///     激活的菜单键
    /// </summary>
    public string? ActiveMenu { get; set; }


    /// <summary>
    ///     默认情况下，相同路径的路由会共享一个标签页。若设置为 true，则使用多个标签页（即使路径相同）
    /// </summary>
    public bool? MultiTab { get; set; } = false;

    /// <summary>
    ///     若设置，路由将在标签页中固定显示，其值表示固定标签页的顺序（首页是特殊的，它将自动保持 fixed）
    /// </summary>
    public int? FixedIndexInTab { get; set; }

    /// <summary>
    ///     父菜单ID (支持无限级菜单)
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    ///     导航属性：父菜单
    /// </summary>
    public Menu? Parent { get; set; }

    /// <summary>
    ///     导航属性：子菜单
    /// </summary>
    public ICollection<Menu> Children { get; private set; } = new List<Menu>();

    /// <summary>
    ///     导航属性：角色菜单关联 (多对多)
    /// </summary>
    public ICollection<RoleMenu> RoleMenus { get; private set; } = new List<RoleMenu>();
}