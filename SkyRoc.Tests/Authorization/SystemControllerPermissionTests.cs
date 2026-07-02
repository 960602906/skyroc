using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Shared.Constants;
using SkyRoc.Controllers;
using Xunit;

namespace SkyRoc.Tests.Authorization;

public class SystemControllerPermissionTests
{
    private static readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<string, string>> ExpectedPolicies =
        new Dictionary<Type, IReadOnlyDictionary<string, string>>
        {
            [typeof(UsersController)] = new Dictionary<string, string>
            {
                [nameof(UsersController.GetPagedMenus)] = PermissionCodes.System.Users.Read,
                [nameof(UsersController.GetAll)] = PermissionCodes.System.Users.Read,
                [nameof(UsersController.GetById)] = PermissionCodes.System.Users.Read,
                [nameof(UsersController.Create)] = PermissionCodes.System.Users.Create,
                [nameof(UsersController.Update)] = PermissionCodes.System.Users.Update,
                [nameof(UsersController.Delete)] = PermissionCodes.System.Users.Delete,
                [nameof(UsersController.BatchDelete)] = PermissionCodes.System.Users.Delete,
                [nameof(UsersController.AssignRoles)] = PermissionCodes.System.Users.AssignRoles,
                [nameof(UsersController.UnassignRoles)] = PermissionCodes.System.Users.AssignRoles
            },
            [typeof(RolesController)] = new Dictionary<string, string>
            {
                [nameof(RolesController.GetPagedMenus)] = PermissionCodes.System.Roles.Read,
                [nameof(RolesController.GetAllRoles)] = PermissionCodes.System.Roles.Read,
                [nameof(RolesController.GetById)] = PermissionCodes.System.Roles.Read,
                [nameof(RolesController.Create)] = PermissionCodes.System.Roles.Create,
                [nameof(RolesController.Update)] = PermissionCodes.System.Roles.Update,
                [nameof(RolesController.Delete)] = PermissionCodes.System.Roles.Delete,
                [nameof(RolesController.BatchDeleteRoles)] = PermissionCodes.System.Roles.Delete,
                [nameof(RolesController.AssignMenus)] = PermissionCodes.System.Roles.AssignMenus,
                [nameof(RolesController.UnAssignMenus)] = PermissionCodes.System.Roles.AssignMenus
            },
            [typeof(MenusController)] = new Dictionary<string, string>
            {
                [nameof(MenusController.GetPagedMenus)] = PermissionCodes.System.Menus.Read,
                [nameof(MenusController.GetAllMenus)] = PermissionCodes.System.Menus.Read,
                [nameof(MenusController.GetAllMenusTreeAsync)] = PermissionCodes.System.Menus.Read,
                [nameof(MenusController.GetById)] = PermissionCodes.System.Menus.Read,
                [nameof(MenusController.Create)] = PermissionCodes.System.Menus.Create,
                [nameof(MenusController.Update)] = PermissionCodes.System.Menus.Update,
                [nameof(MenusController.Delete)] = PermissionCodes.System.Menus.Delete,
                [nameof(MenusController.BatchDelete)] = PermissionCodes.System.Menus.Delete
            },
            [typeof(MenuButtonsController)] = new Dictionary<string, string>
            {
                [nameof(MenuButtonsController.GetById)] = PermissionCodes.System.MenuButtons.Read,
                [nameof(MenuButtonsController.Create)] = PermissionCodes.System.MenuButtons.Create,
                [nameof(MenuButtonsController.BatchCreate)] = PermissionCodes.System.MenuButtons.Create,
                [nameof(MenuButtonsController.Update)] = PermissionCodes.System.MenuButtons.Update,
                [nameof(MenuButtonsController.Replace)] = PermissionCodes.System.MenuButtons.Update,
                [nameof(MenuButtonsController.Delete)] = PermissionCodes.System.MenuButtons.Delete
            },
            [typeof(DepartmentsController)] = new Dictionary<string, string>
            {
                [nameof(DepartmentsController.GetTree)] = PermissionCodes.System.Departments.Read,
                [nameof(DepartmentsController.GetById)] = PermissionCodes.System.Departments.Read,
                [nameof(DepartmentsController.Create)] = PermissionCodes.System.Departments.Create,
                [nameof(DepartmentsController.Update)] = PermissionCodes.System.Departments.Update,
                [nameof(DepartmentsController.Delete)] = PermissionCodes.System.Departments.Delete,
                [nameof(DepartmentsController.BatchDelete)] = PermissionCodes.System.Departments.Delete,
                [nameof(DepartmentsController.ToggleStatus)] = PermissionCodes.System.Departments.Update,
                [nameof(DepartmentsController.GetUsers)] = PermissionCodes.System.Departments.Read
            }
        };

    [Fact]
    public void SystemManagementActions_RequireExpectedPermissionPolicies()
    {
        foreach (var (controllerType, expectedControllerPolicies) in ExpectedPolicies)
        {
            var actions = controllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

            Assert.Equal(expectedControllerPolicies.Count, actions.Length);
            foreach (var action in actions)
            {
                Assert.True(
                    expectedControllerPolicies.TryGetValue(action.Name, out var expectedPolicy),
                    $"{controllerType.Name}.{action.Name} is missing from the permission regression test.");

                var policy = Assert.Single(
                    action.GetCustomAttributes<AuthorizeAttribute>(),
                    attribute => !string.IsNullOrWhiteSpace(attribute.Policy)).Policy;

                Assert.Equal(expectedPolicy, policy);
                Assert.Empty(action.GetCustomAttributes<AllowAnonymousAttribute>());
            }
        }
    }
}
