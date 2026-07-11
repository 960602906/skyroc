using System.Reflection;
using Shared.Constants;
using SkyRoc.Authorization;
using SkyRoc.Controllers;
using Xunit;

namespace SkyRoc.Tests.Authorization;

/// <summary>防止系统支撑接口遗漏运营、公告或日志的细粒度权限。</summary>
public class SystemSupportControllerPermissionTests
{
    [Fact]
    public void SystemSettingsController_DeclaresCrudActionPermissions()
    {
        AssertResource(typeof(SystemSettingsController), PermissionCodes.System.Operations.Resource);
        AssertActions(typeof(SystemSettingsController), new Dictionary<string, string>
        {
            [nameof(SystemSettingsController.GetServicePeriods)] = PermissionActions.Read,
            [nameof(SystemSettingsController.GetServicePeriod)] = PermissionActions.Read,
            [nameof(SystemSettingsController.CreateServicePeriod)] = PermissionActions.Create,
            [nameof(SystemSettingsController.UpdateServicePeriod)] = PermissionActions.Update,
            [nameof(SystemSettingsController.DeleteServicePeriod)] = PermissionActions.Delete,
            [nameof(SystemSettingsController.GetMiniProgramOrderSettings)] = PermissionActions.Read,
            [nameof(SystemSettingsController.SaveMiniProgramOrderSettings)] = PermissionActions.Update,
            [nameof(SystemSettingsController.GetSortingWeightSettings)] = PermissionActions.Read,
            [nameof(SystemSettingsController.SaveSortingWeightSettings)] = PermissionActions.Update
        });
    }

    [Fact]
    public void NoticesAndLogsControllers_DeclareExpectedPermissions()
    {
        AssertResource(typeof(NoticesController), PermissionCodes.System.Notices.Resource);
        AssertActions(typeof(NoticesController), new Dictionary<string, string>
        {
            [nameof(NoticesController.GetPaged)] = PermissionActions.Read,
            [nameof(NoticesController.Create)] = PermissionActions.Create,
            [nameof(NoticesController.Update)] = PermissionActions.Update,
            [nameof(NoticesController.UpdateStatus)] = PermissionActions.Update,
            [nameof(NoticesController.Delete)] = PermissionActions.Delete
        });
        AssertResource(typeof(LogsController), PermissionCodes.System.Logs.Resource);
        AssertActions(typeof(LogsController), new Dictionary<string, string>
        {
            [nameof(LogsController.GetOperations)] = PermissionActions.Read,
            [nameof(LogsController.GetLogins)] = PermissionActions.Read
        });
    }

    private static void AssertResource(Type controllerType, string expected)
    {
        var resource = Assert.Single(controllerType.GetCustomAttributes<PermissionResourceAttribute>());
        Assert.Equal(expected, resource.Resource);
    }

    private static void AssertActions(Type controllerType, IReadOnlyDictionary<string, string> expected)
    {
        foreach (var (actionName, actionPermission) in expected)
        {
            var action = controllerType.GetMethod(actionName) ?? throw new InvalidOperationException($"{actionName} 不存在。");
            Assert.Equal(actionPermission, Assert.Single(action.GetCustomAttributes<ResourcePermissionAttribute>()).Action);
        }
    }
}
