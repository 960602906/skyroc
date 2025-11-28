using Common.Constants;
using Common.Utils;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // 确保数据库已创建
        await context.Database.MigrateAsync();
        await SeedUser(context);
        await SeedRoles(context);
        await SeedMenu(context);
    }

    private static async Task SeedUser(ApplicationDbContext context)
    {
        // 检查是否已有数据
        if (await context.Users.AnyAsync())
        {
            return;
        }
        var users = new List<User>
        {
            new()
            { 
                Username = "admin",
                NickName = "系统管理员",
                Email = "960602906@qq.com",
                Gender = GenderType.Male,
                PasswordHash = PasswordHasher.Hash("123456"),
            },
            new()
            { 
                Username = "user",
                NickName = "普通用户",
                Email = "960602606qq.com",
                Gender =  GenderType.Female,
                PasswordHash = PasswordHasher.Hash("123456"),
            }
        };
        context.Users.AddRange(users);
        await context.SaveChangesAsync();
    }
    private static async Task SeedRoles(ApplicationDbContext context)
    {
        if (await context.Roles.AnyAsync()) return;
        var roles = new List<Role>
        {
            new()
            {
                Name = "管理员",
                Code = "Admin",
                Desc = "系统管理员，拥有所有权限"
            },
            new()
            {
                Name = "用户",
                Code = "User",
                Desc = "普通用户，拥有基本权限"
            },
        };
        context.Roles.AddRange(roles);
        await context.SaveChangesAsync();
    }
    
    private static async Task SeedMenu(ApplicationDbContext context)
    {
        if (await context.Menus.AnyAsync()) return;
         var multi = new Menu
        {
            Name = "multi-menu",
            Path = "/multi-menu",
            Component = "page.(base)_multi-menu",
            Title = "multi-menu",
            I18NKey = "route.(base)_multi-menu",
            Order = 5, 
            Constant = true,
        };
        await context.Menus.AddAsync(multi);
        await context.SaveChangesAsync();
        var first = new Menu
        {
            Name = "multi-menu_first",
            Path = "/multi-menu/first",
            Component = "page.(base)_multi-menu_first",
            Title = "multi-menu_first",
            I18NKey = "route.(base)_multi-menu_first",
            Constant = true,
            ParentId = multi.Id,
        };
        await context.Menus.AddAsync(first);
        await context.SaveChangesAsync();
        var firstChildren = new Menu
        {
            Name = "multi-menu_first_child",
            Path = "/multi-menu/first/child",
            Component = "page.(base)_multi-menu_first_child",
            Title = "multi-menu_first_child",
            I18NKey = "route.(base)_multi-menu_first_child",
            Constant = true,
            ParentId = first.Id,
        };
        await context.Menus.AddRangeAsync(firstChildren);
        var second = new Menu
        {
            Name = "multi-menu_second",
            Path = "/multi-menu/second",
            Component = "page.(base)_multi-menu_second",
            Title = "(base)_multi-menu_second",
            I18NKey = "route.(base)_multi-menu_second",
            ParentId = multi.Id
        };
        await context.Menus.AddAsync(second);
        await context.SaveChangesAsync();
        var secondChildren = new Menu
        {
            Name = "multi-menu_second_child",
            Path = "/multi-menu/second/child",
            Component = "page.(base)_multi-menu_second_child",
            Title = "(base)_multi-menu_second_child",
            I18NKey = "route.(base)_multi-menu_second_child",
            ParentId = second.Id,
        };
        await context.Menus.AddAsync(secondChildren);
        await context.SaveChangesAsync();
        var secondChildrenHome = new Menu
        {
            Name = "multi-menu_second_child_home",
            Path = "/multi-menu/second/child/home",
            Component = "page.(base)_multi-menu_second_child_home",
            Title = "(base)_multi-menu_second_child_home",
            I18NKey = "route.(base)_multi-menu_second_child_home",
            ParentId = secondChildren.Id,
        };
        await context.Menus.AddAsync(secondChildrenHome);
        await context.Menus.AddAsync(new()
        {
            Name = "about",
            Path = "/about",
            Component = "page.(base)_about",
            Title = "about",
            I18NKey = "route.(base)_about",
            Icon = "fluent:book-information-24-regular",
            Order = 9,
        });
        await context.SaveChangesAsync();
        await FunctionMenu(context);
       var home = new Menu
        {
            Name = "home",
            Path = "/home",
            Component = "page.(base)_home",
            Title = "home",
            I18NKey = "route.(base)_home",
            Icon = "mdi:monitor-dashboard",
            Order = 1
        };
        context.Menus.Add(home);
       var manage =  new Menu
        {
            Name = "manage",
            Path = "/manage",
            Component = "page.(base)_manage",
            Title = "manage",
            I18NKey = "route.(base)_manage",
            Icon = "carbon:cloud-service-management",
            Order = 8,
        };
       context.Menus.Add(manage);
       await context.SaveChangesAsync();
       await ManageMenu(context);
       await ProjectsMenu(context);
       await UserCenterMenu(context);
       await ExceptionMenu(context);
       await DocumentMenu(context);
    }

    private static async Task FunctionMenu(ApplicationDbContext context)
    {
        var function = new Menu
        {
            Name = "function",
            Path = "/function",
            Component = "page.(base)_function",
            Title = "function",
            I18NKey = "route.(base)_function",
            Icon = "icon-park-outline:all-application",
            Order = 6,
        };
        context.Menus.Add(function);
        await context.SaveChangesAsync();
        var functionEvent = new Menu
        {
            Name = "function_event-bus",
            Path = "/function/event-bus",
            Component = "page.(base)_function_event-bus",
            Title = "function_event-bus",
            I18NKey = "route.(base)_function_event-bus",
            Icon = "ant-design:send-outlined",
            ParentId = function.Id
        };
        context.Menus.Add(functionEvent);
        var functionChild = new Menu
        {
            Name = "function_hide-child",
            Path = "/function/hide-child",
            Component = "page.(base)_function_hide-child",
            Title = "function_hide-child",
            I18NKey = "route.(base)_function_hide-child",
            Icon = "material-symbols:filter-list-off",
            Order = 2,
            ParentId = function.Id
        };
        context.Menus.Add(functionChild);
        await context.SaveChangesAsync();

        var functionChildChild = new List<Menu>
        {
            new()
            {
                Name = "function_hide-child_one",
                Path = "/function/hide-child/one",
                Component = "page.(base)_function_hide-child_one",
                Title = "function_hide-child_one",
                I18NKey = "route.(base)_function_hide-child_one",
                HideInMenu = true,
                ActiveMenu = "/function/hide-child",
                ParentId = functionChild.Id
            },
            new()
            {
                Name = "function_hide-child_three",
                Path = "/function/hide-child/three",
                Component = "page.(base)_function_hide-child_three",
                Title = "function_hide-child_three",
                I18NKey = "route.(base)_function_hide-child_three",
                HideInMenu = true,
                ActiveMenu =  "/function/hide-child",
                ParentId = functionChild.Id
            },
            new()
            {
                Name = "function_hide-child_two",
                Path = "/function/hide-child/two",
                Component = "page.(base)_function_hide-child_two",
                Title = "function_hide-child_two",
                I18NKey = "route.(base)_function_hide-child_two",
                HideInMenu = true,
                ActiveMenu = "/function/hide-child",
                ParentId = functionChild.Id
            },
            
        };
        context.Menus.AddRange(functionChildChild);
        var functionHy= new List<Menu>()
        {
            new()
            {
                Name = "function_multi-tab",
                Path = "/function/multi-tab",
                Component = "page.(base)_function_multi-tab",
                Title = "function_multi-tab",
                I18NKey = "route.(base)_function_multi-tab",
                Icon = "ic:round-tab",
                MultiTab = true,
                HideInMenu =  true,
                ActiveMenu = "/function/tab",
                ParentId = function.Id
            },
            new()
            {
                Name = "function_request",
                Path = "/function/request",
                Component = "page.(base)_function_request",
                Title = "function_request",
                I18NKey = "route.(base)_function_request",
                Icon = "carbon:network-overlay",
                Order = 3,
                ParentId = function.Id
            },
            new()
            {
                Name = "function_super-page",
                Path = "/function/super-page",
                Component = "page.(base)_function_super-page",
                Title = "function_super-page",
                I18NKey = "route.(base)_function_super-page",
                Icon = "ic:round-supervisor-account",
                Order = 5,
                ParentId = function.Id
            },
            new()
            {
                Name = "function_tab",
                Path = "/function/tab",
                Component = "page.(base)_function_tab",
                Title = "function_tab",
                I18NKey = "route.(base)_function_tab",
                Icon = "ic:round-tab",
                Order = 1,
                KeepAlive = true,
                ParentId = function.Id
            },
            new()
            {
                Name = "function_toggle-auth",
                Path = "/function/toggle-auth",
                Component = "page.(base)_function_toggle-auth",
                Title = "function_toggle-auth",
                I18NKey = "route.(base)_function_toggle-auth",
                Icon = "ic:round-construction",
                Order = 4,
                ParentId = function.Id
            },
        };
        context.Menus.AddRange(functionHy);
        await context.SaveChangesAsync();
    }

    private static async Task ManageMenu(ApplicationDbContext context)
    {
        var manage = await context.Menus.FirstOrDefaultAsync(m => m.Name == "manage");
        if (manage == null) return;

        var manageRole = new Menu
        {
            Name = "manage_role",
            Path = "/manage/role",
            Component = "page.(base)_manage_role",
            Title = "manage_role",
            I18NKey = "route.(base)_manage_role",
            Icon = "carbon:user-role",
            Order = 2,
            ParentId = manage.Id
        };
        context.Menus.Add(manageRole);
        await context.SaveChangesAsync();

        var manageRoleSlug = new Menu
        {
            Name = "manage_role_[...slug]",
            Path = "/manage/role/*",
            Component = "page.(base)_manage_role_[...slug]",
            Title = "(base)_manage_role_[...slug]",
            I18NKey = "route.(base)_manage_role_[...slug]",
            HideInMenu = true,
            ParentId = manageRole.Id
        };
        context.Menus.Add(manageRoleSlug);

        var manageUser = new Menu
        {
            Name = "manage_user",
            Path = "/manage/user",
            Component = "page.(base)_manage_user",
            Title = "manage_user",
            I18NKey = "route.(base)_manage_user",
            Icon = "ic:round-manage-accounts",
            Order = 1,
            KeepAlive = true,
            ParentId = manage.Id
        };
        context.Menus.Add(manageUser);
        await context.SaveChangesAsync();

        var manageUserId = new Menu
        {
            Name = "manage_user_[id]",
            Path = "/manage/user/:id",
            Component = "page.(base)_manage_user_[id]",
            Title = "(base)_manage_user_[id]",
            I18NKey = "route.(base)_manage_user_[id]",
            HideInMenu = true,
            ActiveMenu = "/manage/user",
            ParentId = manageUser.Id
        };
        context.Menus.Add(manageUserId);
        await context.SaveChangesAsync();
    }

    private static async Task ProjectsMenu(ApplicationDbContext context)
    {
        var projects = new Menu
        {
            Name = "projects",
            Path = "/projects",
            Component = "page.(base)_projects",
            Title = "(base)_projects",
            I18NKey = "route.(base)_projects",
            Icon = "hugeicons:align-box-top-center",
            Order = 7
        };
        context.Menus.Add(projects);
        await context.SaveChangesAsync();

        var projectsPid = new Menu
        {
            Name = "projects_[pid]",
            Path = "/projects/:pid",
            Component = "page.(base)_projects_[pid]",
            Title = "(base)_projects_[pid]",
            I18NKey = "route.(base)_projects_[pid]",
            Icon = "material-symbols-light:attachment",
            ParentId = projects.Id
        };
        context.Menus.Add(projectsPid);
        await context.SaveChangesAsync();

        var projectsPidEdit = new Menu
        {
            Name = "projects_[pid]_edit",
            Path = "/projects/:pid/edit",
            Component = "page.(base)_projects_[pid]_edit",
            Title = "(base)_projects_[pid]_edit",
            I18NKey = "route.(base)_projects_[pid]_edit",
            Icon = "material-symbols-light:assistant-on-hub-outline",
            ParentId = projectsPid.Id
        };
        context.Menus.Add(projectsPidEdit);
        await context.SaveChangesAsync();

        var projectsPidEditId = new Menu
        {
            Name = "projects_[pid]_edit_[id]",
            Path = "/projects/:pid/edit/:id",
            Component = "page.(base)_projects_[pid]_edit_[id]",
            Title = "(base)_projects_[pid]_edit_[id]",
            I18NKey = "route.(base)_projects_[pid]_edit_[id]",
            ParentId = projectsPidEdit.Id
        };
        context.Menus.Add(projectsPidEditId);
        await context.SaveChangesAsync();
    }

    private static async Task UserCenterMenu(ApplicationDbContext context)
    {
        var userCenter = new Menu
        {
            Name = "user-center",
            Path = "/user-center",
            Component = "page.(base)_user-center",
            Title = "user-center",
            I18NKey = "route.(base)_user-center",
            HideInMenu = true
        };
        context.Menus.Add(userCenter);
        await context.SaveChangesAsync();
    }

    private static async Task ExceptionMenu(ApplicationDbContext context)
    {
        var exception = new Menu
        {
            Name = "exception",
            Path = "/exception",
            Title = "exception",
            I18NKey = "route.exception",
            Icon = "ant-design:exception-outlined",
            Order = 4
        };
        context.Menus.Add(exception);
        await context.SaveChangesAsync();

        var exceptionMenus = new List<Menu>
        {
            new()
            {
                Name = "exception_403",
                Path = "/exception/403",
                Component = "page.403",
                Title = "exception_403",
                I18NKey = "route.exception_403",
                Icon = "ic:baseline-block",
                ParentId = exception.Id
            },
            new()
            {
                Name = "exception_404",
                Path = "/exception/404",
                Component = "page.404",
                Title = "exception_404",
                I18NKey = "route.exception_404",
                Icon = "ic:baseline-web-asset-off",
                ParentId = exception.Id
            },
            new()
            {
                Name = "exception_500",
                Path = "/exception/500",
                Component = "page.500",
                Title = "exception_500",
                I18NKey = "route.exception_500",
                Icon = "ic:baseline-wifi-off",
                ParentId = exception.Id
            }
        };
        context.Menus.AddRange(exceptionMenus);
        await context.SaveChangesAsync();
    }

    private static async Task DocumentMenu(ApplicationDbContext context)
    {
        var document = new Menu
        {
            Name = "document",
            Path = "/document",
            Title = "document",
            I18NKey = "route.document",
            Icon = "mdi:file-document-multiple-outline",
            Order = 2
        };
        context.Menus.Add(document);
        await context.SaveChangesAsync();

        var documentMenus = new List<Menu>
        {
            new()
            {
                Name = "document_ui",
                Path = "/document/ui",
                Component = "page.iframe-page",
                Title = "document_ui",
                I18NKey = "route.document_ui",
                LocalIcon = "logo",
                Order = 0,
                Href = "https://ui-play.skyroc.me/button",
                ParentId = document.Id
            },
            new()
            {
                Name = "document_project",
                Path = "/document/project",
                Component = "page.iframe-page",
                Title = "document_project",
                I18NKey = "route.document_project",
                LocalIcon = "logo",
                Order = 1,
                Href = "https://admin-docs.skyroc.me  ",
                ParentId = document.Id
            },
            new()
            {
                Name = "document_project-link",
                Path = "/document/project-link",
                Title = "document_project-link",
                I18NKey = "route.document_project-link",
                LocalIcon = "logo",
                Order = 2,
                Href = "https://admin-docs.skyroc.me  ",
                ParentId = document.Id
            },
            new()
            {
                Name = "document_react",
                Path = "/document/react",
                Component = "page.iframe-page",
                Title = "document_react",
                I18NKey = "route.document_react",
                Icon = "logos:react",
                Order = 3,
                Href = "https://react.dev/",
                ParentId = document.Id
            },
            new()
            {
                Name = "document_vite",
                Path = "/document/vite",
                Component = "page.iframe-page",
                Title = "document_vite",
                I18NKey = "route.document_vite",
                Icon = "logos:vitejs",
                Order = 4,
                Href = "https://cn.vitejs.dev/",
                ParentId = document.Id
            },
            new()
            {
                Name = "document_unocss",
                Path = "/document/unocss",
                Component = "page.iframe-page",
                Title = "document_unocss",
                I18NKey = "route.document_unocss",
                Icon = "logos:unocss",
                Order = 5,
                Href = "https://unocss.dev/",
                ParentId = document.Id
            },
            new()
            {
                Name = "document_antd",
                Path = "/document/antd",
                Component = "page.iframe-page",
                Title = "document_antd",
                I18NKey = "route.document_antd",
                Icon = "logos:ant-design",
                Order = 7,
                Href = "https://ant.design/index-cn",
                ParentId = document.Id
            },
            new()
            {
                Name = "document_procomponents",
                Path = "/document/procomponents",
                Component = "page.iframe-page",
                Title = "document_procomponents",
                I18NKey = "route.document_procomponents",
                Icon = "logos:ant-design",
                Order = 8,
                Href = "https://pro-components.antdigital.dev/",
                ParentId = document.Id
            }
        };
        context.Menus.AddRange(documentMenus);
        await context.SaveChangesAsync();
    }
}
