using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Shared.Constants;
using SkyRoc.Authorization;
using SkyRoc.Controllers;
using Xunit;

namespace SkyRoc.Tests.Authorization;

public class OrderControllerPermissionTests
{
    private static readonly IReadOnlyDictionary<string, string> ExpectedResourceActions =
        new Dictionary<string, string>
        {
            [nameof(OrdersController.GetPaged)] = PermissionActions.Read,
            [nameof(OrdersController.GetById)] = PermissionActions.Read,
            [nameof(OrdersController.Create)] = PermissionActions.Create,
            [nameof(OrdersController.Update)] = PermissionActions.Update,
            [nameof(OrdersController.Delete)] = PermissionActions.Delete
        };

    private static readonly string[] AuditActions =
    [
        nameof(OrdersController.Approve),
        nameof(OrdersController.Reject),
        nameof(OrdersController.Resubmit)
    ];

    [Fact]
    public void OrdersController_DeclaresExpectedResourceAndCrudPermissions()
    {
        var resource = Assert.Single(typeof(OrdersController).GetCustomAttributes<PermissionResourceAttribute>());
        Assert.Equal(PermissionCodes.Business.Orders.Resource, resource.Resource);

        foreach (var (actionName, expectedAction) in ExpectedResourceActions)
        {
            var action = GetAction(actionName);
            var requirement = Assert.Single(action.GetCustomAttributes<ResourcePermissionAttribute>());
            Assert.Equal(expectedAction, requirement.Action);
        }
    }

    [Fact]
    public void OrdersController_AuditActionsRequireAuditPermission()
    {
        foreach (var actionName in AuditActions)
        {
            var action = GetAction(actionName);
            var authorize = Assert.Single(
                action.GetCustomAttributes<AuthorizeAttribute>(),
                attribute => !string.IsNullOrWhiteSpace(attribute.Policy));
            Assert.Equal(PermissionCodes.Business.Orders.Audit, authorize.Policy);
        }
    }

    private static MethodInfo GetAction(string actionName)
    {
        return typeof(OrdersController).GetMethod(actionName)
               ?? throw new InvalidOperationException($"Action {actionName} was not found.");
    }
}
