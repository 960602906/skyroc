using System.Reflection;
using Shared.Constants;
using SkyRoc.Authorization;
using SkyRoc.Controllers;
using Xunit;

namespace SkyRoc.Tests.Authorization;

public class StocktakingControllerPermissionTests
{
    [Fact]
    public void StocktakingController_declares_storage_resource_and_action_permissions()
    {
        var resource = Assert.Single(typeof(StocktakingController)
            .GetCustomAttributes<PermissionResourceAttribute>());
        Assert.Equal(PermissionCodes.Business.Storage.Resource, resource.Resource);

        var expectedActions = new Dictionary<string, string>
        {
            [nameof(StocktakingController.GetPaged)] = PermissionActions.Read,
            [nameof(StocktakingController.GetById)] = PermissionActions.Read,
            [nameof(StocktakingController.Create)] = PermissionActions.Create,
            [nameof(StocktakingController.Audit)] = PermissionActions.Update
        };
        foreach (var (actionName, expectedAction) in expectedActions)
        {
            var method = typeof(StocktakingController).GetMethod(actionName)
                         ?? throw new InvalidOperationException($"Action {actionName} was not found.");
            var permission = Assert.Single(method.GetCustomAttributes<ResourcePermissionAttribute>());
            Assert.Equal(expectedAction, permission.Action);
        }
    }
}
