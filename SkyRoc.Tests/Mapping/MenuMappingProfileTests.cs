using Application.DTOs.Auth;
using Application.DTOs.Menu;
using AutoMapper;
using Domain.Entities;
using Shared.Constants;
using Xunit;

namespace SkyRoc.Tests.Mapping;

[Collection("Mapping")]
public class MenuMappingProfileTests
{
    private readonly IMapper _mapper;

    public MenuMappingProfileTests(MappingTestFixture fixture)
    {
        _mapper = fixture.MenuMapper;
    }

    [Fact]
    public void Should_build_menu_tree_from_flat_menus()
    {
        var rootId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        var menus = new List<Menu>
        {
            new()
            {
                Id = rootId,
                Name = "manage",
                Title = "管理",
                Path = "/manage",
                Order = 1,
                Status = Status.Enable,
                CreateTime = DateTime.UtcNow
            },
            new()
            {
                Id = childId,
                Name = "manage_user",
                Title = "用户管理",
                Path = "/manage/user",
                ParentId = rootId,
                Order = 1,
                Status = Status.Enable,
                CreateTime = DateTime.UtcNow
            }
        };

        menus[0].Children.Add(menus[1]);

        var result = _mapper.Map<List<MenuTreeDto>>(menus);

        var root = Assert.Single(result);
        Assert.Equal(rootId, root.Id);
        Assert.NotNull(root.Children);
        var child = Assert.Single(root.Children!);
        Assert.Equal(childId, child.Id);
        Assert.Equal(rootId, child.ParentId);
    }

    [Fact]
    public void Should_build_routes_tree_and_preserve_handle_metadata()
    {
        var rootId = Guid.NewGuid();
        var childId = Guid.NewGuid();

        var menus = new List<Menu>
        {
            new()
            {
                Id = rootId,
                Name = "document",
                Title = "文档",
                Path = "/document",
                Icon = "mdi:file-document",
                Order = 2,
                Status = Status.Enable,
                CreateTime = DateTime.UtcNow
            },
            new()
            {
                Id = childId,
                Name = "document_ui",
                Title = "UI 文档",
                Path = "/document/ui",
                ParentId = rootId,
                Href = "https://example.com/ui",
                Order = 1,
                Status = Status.Enable,
                CreateTime = DateTime.UtcNow
            }
        };

        menus[0].Children.Add(menus[1]);

        var result = _mapper.Map<List<RoutesDto>>(menus);

        var root = Assert.Single(result);
        Assert.Equal("document", root.Name);
        Assert.Equal("文档", root.Handle.Title);
        Assert.NotNull(root.Children);
        var child = Assert.Single(root.Children!);
        Assert.Equal(childId, child.Id);
        Assert.Equal("https://example.com/ui", child.Handle.Href);
    }

    /// <summary>
    ///     更新菜单 DTO 不得覆盖已加载的按钮导航，否则 EF 会把既有按钮级联删除。
    /// </summary>
    [Fact]
    public void UpdateMenuDto_MapToEntity_DoesNotClearExistingButtons()
    {
        var menuId = Guid.NewGuid();
        var buttonId = Guid.NewGuid();
        var menu = new Menu
        {
            Id = menuId,
            Name = "manage_menu",
            Title = "原标题",
            Path = "/manage/menu",
            Status = Status.Enable,
            CreateTime = DateTime.UtcNow
        };
        menu.Buttons.Add(new MenuButton
        {
            Id = buttonId,
            Code = "system:menu:read",
            Desc = "读取",
            MenuId = menuId,
            Menu = null!,
            Status = Status.Enable
        });

        var update = new UpdateMenuDto
        {
            Id = menuId,
            Name = "manage_menu",
            Title = "新标题",
            Path = "/manage/menu",
            Status = Status.Enable,
            Buttons = null
        };

        _mapper.Map(update, menu);

        Assert.Equal("新标题", menu.Title);
        var button = Assert.Single(menu.Buttons);
        Assert.Equal(buttonId, button.Id);
        Assert.Equal("system:menu:read", button.Code);
    }
}
