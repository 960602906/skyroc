using System.Reflection;
using Shared.Constants;
using SkyRoc.Authorization;
using SkyRoc.Controllers;
using Xunit;

namespace SkyRoc.Tests.Authorization;

/// <summary>
/// 防止客户结款查询、创建和作废操作丢失财务细粒度权限。
/// </summary>
public class FinanceControllerPermissionTests
{
    [Fact]
    public void CustomerSettlementsController_DeclaresFinanceResourceAndActionPermissions()
    {
        var resource = Assert.Single(typeof(CustomerSettlementsController)
            .GetCustomAttributes<PermissionResourceAttribute>());
        Assert.Equal(PermissionCodes.Business.Finance.Resource, resource.Resource);

        var expected = new Dictionary<string, string>
        {
            [nameof(CustomerSettlementsController.GetBills)] = PermissionActions.Read,
            [nameof(CustomerSettlementsController.GetPaged)] = PermissionActions.Read,
            [nameof(CustomerSettlementsController.GetById)] = PermissionActions.Read,
            [nameof(CustomerSettlementsController.Create)] = PermissionActions.Create,
            [nameof(CustomerSettlementsController.Void)] = PermissionActions.Delete
        };

        foreach (var (actionName, permissionAction) in expected)
        {
            var action = typeof(CustomerSettlementsController).GetMethod(actionName)
                         ?? throw new InvalidOperationException($"{actionName} not found.");
            var attribute = Assert.Single(action.GetCustomAttributes<ResourcePermissionAttribute>());
            Assert.Equal(permissionAction, attribute.Action);
        }
    }
}
