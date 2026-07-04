using System.Reflection;
using Shared.Constants;
using SkyRoc.Authorization;
using SkyRoc.Controllers;
using Xunit;

namespace SkyRoc.Tests.Authorization;

/// <summary>
/// 校验配送任务与配送异常操作使用稳定的配送资源权限。
/// </summary>
public class DeliveryTaskControllerPermissionTests
{
    [Fact]
    public void DeliveryTaskController_DeclaresExpectedPermissions()
    {
        AssertPermissions<DeliveryTasksController>(new Dictionary<string, string>
        {
            ["GetOrderTasks"] = PermissionActions.Read,
            ["GetDriverTasks"] = PermissionActions.Read,
            ["GetById"] = PermissionActions.Read,
            ["Generate"] = PermissionActions.Create,
            ["AssignDriver"] = PermissionActions.Update,
            ["IntelligentPlan"] = PermissionActions.Update,
            ["Start"] = PermissionActions.Update,
            ["Sign"] = PermissionActions.Update,
            ["ReturnReceipt"] = PermissionActions.Update
        });
    }

    [Fact]
    public void DeliveryExceptionController_DeclaresExpectedPermissions()
    {
        AssertPermissions<DeliveryExceptionsController>(new Dictionary<string, string>
        {
            ["GetPaged"] = PermissionActions.Read,
            ["GetById"] = PermissionActions.Read,
            ["Create"] = PermissionActions.Create,
            ["Handle"] = PermissionActions.Update
        });
    }

    private static void AssertPermissions<TController>(IReadOnlyDictionary<string, string> expected)
    {
        var controllerType = typeof(TController);
        var resource = Assert.Single(controllerType.GetCustomAttributes<PermissionResourceAttribute>());
        Assert.Equal(PermissionCodes.Business.Delivery.Resource, resource.Resource);

        foreach (var (methodName, action) in expected)
        {
            var method = controllerType.GetMethod(methodName)
                         ?? throw new InvalidOperationException($"Action {methodName} was not found.");
            Assert.Equal(action, Assert.Single(method.GetCustomAttributes<ResourcePermissionAttribute>()).Action);
        }
    }
}
