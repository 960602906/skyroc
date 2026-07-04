using System.Reflection;
using Shared.Constants;
using SkyRoc.Authorization;
using SkyRoc.Controllers;
using Xunit;

namespace SkyRoc.Tests.Authorization;

public class StockQueryControllerPermissionTests
{
    [Fact]
    public void StockQueryController_declares_storage_read_permission_for_all_actions()
    {
        var resource = Assert.Single(typeof(StockQueryController)
            .GetCustomAttributes<PermissionResourceAttribute>());
        Assert.Equal(PermissionCodes.Business.Storage.Resource, resource.Resource);

        foreach (var actionName in new[]
                 {
                     nameof(StockQueryController.GetOverview),
                     nameof(StockQueryController.GetBatches),
                     nameof(StockQueryController.GetLedgers)
                 })
        {
            var method = typeof(StockQueryController).GetMethod(actionName)
                         ?? throw new InvalidOperationException($"Action {actionName} was not found.");
            var permission = Assert.Single(method.GetCustomAttributes<ResourcePermissionAttribute>());
            Assert.Equal(PermissionActions.Read, permission.Action);
        }
    }
}
