using System.Reflection;
using Shared.Constants;
using SkyRoc.Authorization;
using SkyRoc.Controllers;
using Xunit;

namespace SkyRoc.Tests.Authorization;

public class StockOutControllerPermissionTests
{
    private static readonly IReadOnlyDictionary<string, string> ExpectedActions =
        new Dictionary<string, string>
        {
            ["GetPaged"] = PermissionActions.Read,
            ["GetById"] = PermissionActions.Read,
            ["Create"] = PermissionActions.Create,
            ["Update"] = PermissionActions.Update,
            ["Delete"] = PermissionActions.Delete,
            ["Audit"] = PermissionActions.Update,
            ["ReverseAudit"] = PermissionActions.Update
        };

    public static IEnumerable<object[]> ControllerTypes =>
    [
        [typeof(SaleStockOutController)],
        [typeof(PurchaseReturnStockOutController)],
        [typeof(OtherStockOutController)]
    ];

    [Theory]
    [MemberData(nameof(ControllerTypes))]
    public void StockOutController_declares_storage_resource_and_action_permissions(Type controllerType)
    {
        var resource = Assert.Single(controllerType.GetCustomAttributes<PermissionResourceAttribute>());
        Assert.Equal(PermissionCodes.Business.Storage.Resource, resource.Resource);

        foreach (var (actionName, expectedAction) in ExpectedActions)
        {
            var method = controllerType.GetMethod(actionName)
                         ?? throw new InvalidOperationException($"Action {actionName} was not found on {controllerType.Name}.");
            var permission = Assert.Single(method.GetCustomAttributes<ResourcePermissionAttribute>());
            Assert.Equal(expectedAction, permission.Action);
        }
    }
}
