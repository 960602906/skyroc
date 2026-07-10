using System.Reflection;
using Shared.Constants;
using SkyRoc.Authorization;
using SkyRoc.Controllers;
using Xunit;

namespace SkyRoc.Tests.Authorization;

/// <summary>
/// 防止报表查询接口丢失只读细粒度权限。
/// </summary>
public class ReportControllerPermissionTests
{
    [Fact]
    public void ReportsController_DeclaresReportResourceAndReadPermissions()
    {
        var resource = Assert.Single(typeof(ReportsController).GetCustomAttributes<PermissionResourceAttribute>());
        Assert.Equal(PermissionCodes.Business.Reports.Resource, resource.Resource);

        var expected = new[]
        {
            nameof(ReportsController.GetSalesGoodsSummary),
            nameof(ReportsController.GetSalesCategorySummary),
            nameof(ReportsController.GetSalesCustomerSummary),
            nameof(ReportsController.GetSalesAreaSummary),
            nameof(ReportsController.GetAfterSaleSummary),
            nameof(ReportsController.GetDailyStockInOutSummary),
            nameof(ReportsController.GetDailyGoodsStockInOutSummary),
            nameof(ReportsController.GetPurchaseInOutGoodsSummary),
            nameof(ReportsController.GetPurchaseInOutSupplierSummary),
            nameof(ReportsController.GetPurchaseInOutPurchaserSummary)
        };

        foreach (var actionName in expected)
        {
            var action = typeof(ReportsController).GetMethod(actionName)
                         ?? throw new InvalidOperationException($"{actionName} not found.");
            var attribute = Assert.Single(action.GetCustomAttributes<ResourcePermissionAttribute>());
            Assert.Equal(PermissionActions.Read, attribute.Action);
        }
    }
}
