using System.Reflection;
using Shared.Constants;
using SkyRoc.Authorization;
using SkyRoc.Controllers;
using Xunit;

namespace SkyRoc.Tests.Authorization;

public class PurchaseOrderControllerPermissionTests
{
    private static readonly IReadOnlyDictionary<string, string> ExpectedActions =
        new Dictionary<string, string>
        {
            [nameof(PurchaseOrdersController.GetPaged)] = PermissionActions.Read,
            [nameof(PurchaseOrdersController.GetById)] = PermissionActions.Read,
            [nameof(PurchaseOrdersController.Create)] = PermissionActions.Create,
            [nameof(PurchaseOrdersController.Update)] = PermissionActions.Update,
            [nameof(PurchaseOrdersController.Delete)] = PermissionActions.Delete,
            [nameof(PurchaseOrdersController.GenerateFromPlans)] = PermissionActions.Create,
            [nameof(PurchaseOrdersController.Complete)] = PermissionActions.Update,
            [nameof(PurchaseOrdersController.Cancel)] = PermissionActions.Update
        };

    [Fact]
    public void PurchaseOrdersController_declares_purchase_resource_and_action_permissions()
    {
        var resource = Assert.Single(typeof(PurchaseOrdersController)
            .GetCustomAttributes<PermissionResourceAttribute>());
        Assert.Equal(PermissionCodes.Business.Purchases.Resource, resource.Resource);

        foreach (var (actionName, expectedAction) in ExpectedActions)
        {
            var method = typeof(PurchaseOrdersController).GetMethod(actionName)
                         ?? throw new InvalidOperationException($"Action {actionName} was not found.");
            var permission = Assert.Single(method.GetCustomAttributes<ResourcePermissionAttribute>());
            Assert.Equal(expectedAction, permission.Action);
        }
    }
}
