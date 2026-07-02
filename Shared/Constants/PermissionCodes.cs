namespace Shared.Constants;

/// <summary>
///     API 权限编码。
///     编码格式固定为 module:resource:action，已发布编码不得复用或改变含义。
/// </summary>
public static class PermissionCodes
{
    public const string All = "*:*:*";

    public static class System
    {
        public static class Users
        {
            public const string Read = "system:user:read";
            public const string Create = "system:user:create";
            public const string Update = "system:user:update";
            public const string Delete = "system:user:delete";
            public const string AssignRoles = "system:user:assign-roles";
        }

        public static class Roles
        {
            public const string Read = "system:role:read";
            public const string Create = "system:role:create";
            public const string Update = "system:role:update";
            public const string Delete = "system:role:delete";
            public const string AssignMenus = "system:role:assign-menus";
        }

        public static class Menus
        {
            public const string Read = "system:menu:read";
            public const string Create = "system:menu:create";
            public const string Update = "system:menu:update";
            public const string Delete = "system:menu:delete";
        }

        public static class MenuButtons
        {
            public const string Read = "system:menu-button:read";
            public const string Create = "system:menu-button:create";
            public const string Update = "system:menu-button:update";
            public const string Delete = "system:menu-button:delete";
        }

        public static class Departments
        {
            public const string Read = "system:department:read";
            public const string Create = "system:department:create";
            public const string Update = "system:department:update";
            public const string Delete = "system:department:delete";
        }
    }

    public static IReadOnlyCollection<string> Defined { get; } =
    [
        System.Users.Read,
        System.Users.Create,
        System.Users.Update,
        System.Users.Delete,
        System.Users.AssignRoles,
        System.Roles.Read,
        System.Roles.Create,
        System.Roles.Update,
        System.Roles.Delete,
        System.Roles.AssignMenus,
        System.Menus.Read,
        System.Menus.Create,
        System.Menus.Update,
        System.Menus.Delete,
        System.MenuButtons.Read,
        System.MenuButtons.Create,
        System.MenuButtons.Update,
        System.MenuButtons.Delete,
        System.Departments.Read,
        System.Departments.Create,
        System.Departments.Update,
        System.Departments.Delete
    ];
}
