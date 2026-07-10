using System.Reflection;
using Shared.Constants;
using SkyRoc.Authorization;
using SkyRoc.Controllers;
using Xunit;

namespace SkyRoc.Tests.Authorization;

/// <summary>防止检测报告、商品溯源和外部报送日志接口丢失独立溯源资源权限或操作权限。</summary>
public class TraceabilityControllerPermissionTests
{
    [Fact]
    public void TraceabilityControllers_DeclareResourceAndActionPermissions()
    {
        AssertControllerPermissions(typeof(InspectionReportsController), new Dictionary<string, string>
        {
            [nameof(InspectionReportsController.GetPaged)] = PermissionActions.Read,
            [nameof(InspectionReportsController.GetById)] = PermissionActions.Read,
            [nameof(InspectionReportsController.GetEligibleStockIns)] = PermissionActions.Read,
            [nameof(InspectionReportsController.GetEligibleStockInDetails)] = PermissionActions.Read,
            [nameof(InspectionReportsController.Create)] = PermissionActions.Create,
            [nameof(InspectionReportsController.Update)] = PermissionActions.Update,
            [nameof(InspectionReportsController.Delete)] = PermissionActions.Delete
        });
        AssertControllerPermissions(typeof(TraceRecordsController), new Dictionary<string, string>
        {
            [nameof(TraceRecordsController.GetPaged)] = PermissionActions.Read,
            [nameof(TraceRecordsController.Generate)] = PermissionActions.Create
        });
        AssertControllerPermissions(typeof(ExternalPushLogsController), new Dictionary<string, string>
        {
            [nameof(ExternalPushLogsController.GetPaged)] = PermissionActions.Read
        });
    }

    [Fact]
    public void TraceQrCode_IsExplicitlyAnonymousAndHasNoOperationPermission()
    {
        var action = typeof(TraceRecordsController).GetMethod(nameof(TraceRecordsController.GetQrCode))
                     ?? throw new InvalidOperationException("二维码详情操作不存在。");
        Assert.Single(action.GetCustomAttributes<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>());
        Assert.Empty(action.GetCustomAttributes<ResourcePermissionAttribute>());
    }

    private static void AssertControllerPermissions(Type controllerType, IReadOnlyDictionary<string, string> expected)
    {
        var resource = Assert.Single(controllerType.GetCustomAttributes<PermissionResourceAttribute>());
        Assert.Equal(PermissionCodes.Business.Traceability.Resource, resource.Resource);
        foreach (var (actionName, permissionAction) in expected)
        {
            var action = controllerType.GetMethod(actionName)
                         ?? throw new InvalidOperationException($"{actionName} not found.");
            var attribute = Assert.Single(action.GetCustomAttributes<ResourcePermissionAttribute>());
            Assert.Equal(permissionAction, attribute.Action);
        }
    }
}
