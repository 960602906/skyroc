using System.Reflection;
using Shared.Constants;
using SkyRoc.Authorization;
using SkyRoc.Controllers;
using Xunit;

namespace SkyRoc.Tests.Authorization;

/// <summary>
/// 防止首页驾驶舱只读接口丢失报表读取权限。
/// </summary>
public class DashboardControllerPermissionTests
{
    [Fact]
    public void DashboardController_DeclaresReportResourceAndReadPermissions()
    {
        var resource = Assert.Single(typeof(DashboardController).GetCustomAttributes<PermissionResourceAttribute>());
        Assert.Equal(PermissionCodes.Business.Reports.Resource, resource.Resource);

        var expected = new[]
        {
            nameof(DashboardController.GetBrief),
            nameof(DashboardController.GetSalesTrend),
            nameof(DashboardController.GetCustomerSalesRank),
            nameof(DashboardController.GetGoodsTypeSalesRank),
            nameof(DashboardController.GetReconciliation),
            nameof(DashboardController.GetPickupStatuses)
        };

        foreach (var actionName in expected)
        {
            var action = typeof(DashboardController).GetMethod(actionName)
                         ?? throw new InvalidOperationException($"{actionName} not found.");
            var attribute = Assert.Single(action.GetCustomAttributes<ResourcePermissionAttribute>());
            Assert.Equal(PermissionActions.Read, attribute.Action);
        }
    }
}
