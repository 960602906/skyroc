using System.Reflection;
using Shared.Constants;
using SkyRoc.Authorization;
using SkyRoc.Controllers;
using Xunit;

namespace SkyRoc.Tests.Authorization;

/// <summary>
/// 防止打印模板维护或业务打印数据接口遗漏细粒度权限。
/// </summary>
public class PrintControllerPermissionTests
{
    [Fact]
    public void PrintTemplatesController_DeclaresResourceAndActionPermissions()
    {
        var resource = Assert.Single(typeof(PrintTemplatesController).GetCustomAttributes<PermissionResourceAttribute>());
        Assert.Equal(PermissionCodes.System.PrintTemplates.Resource, resource.Resource);

        AssertActionPermissions(typeof(PrintTemplatesController), new Dictionary<string, string>
        {
            [nameof(PrintTemplatesController.GetPaged)] = PermissionActions.Read,
            [nameof(PrintTemplatesController.GetByCode)] = PermissionActions.Read,
            [nameof(PrintTemplatesController.Create)] = PermissionActions.Create,
            [nameof(PrintTemplatesController.Update)] = PermissionActions.Update,
            [nameof(PrintTemplatesController.Delete)] = PermissionActions.Delete
        });
    }

    [Fact]
    public void PrintDataController_DeclaresReadAndConfirmPermissions()
    {
        var resource = Assert.Single(typeof(PrintDataController).GetCustomAttributes<PermissionResourceAttribute>());
        Assert.Equal(PermissionCodes.Business.Printing.Resource, resource.Resource);

        AssertActionPermissions(typeof(PrintDataController), new Dictionary<string, string>
        {
            [nameof(PrintDataController.GetData)] = PermissionActions.Read,
            [nameof(PrintDataController.ConfirmPrinted)] = PermissionActions.Update
        });
    }

    private static void AssertActionPermissions(Type controllerType, IReadOnlyDictionary<string, string> expected)
    {
        foreach (var (actionName, actionPermission) in expected)
        {
            var action = controllerType.GetMethod(actionName)
                         ?? throw new InvalidOperationException($"{actionName} 不存在。");
            var attribute = Assert.Single(action.GetCustomAttributes<ResourcePermissionAttribute>());
            Assert.Equal(actionPermission, attribute.Action);
        }
    }
}
