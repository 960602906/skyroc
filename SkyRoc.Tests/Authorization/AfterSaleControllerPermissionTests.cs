using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Shared.Constants;
using SkyRoc.Authorization;
using SkyRoc.Controllers;
using Xunit;

namespace SkyRoc.Tests.Authorization;

/// <summary>
/// 防止售后查询、草稿维护和审核操作丢失细粒度权限。
/// </summary>
public class AfterSaleControllerPermissionTests
{
    [Fact]
    public void AfterSalesController_DeclaresResourceAndCrudPermissions()
    {
        var resource = Assert.Single(typeof(AfterSalesController).GetCustomAttributes<PermissionResourceAttribute>());
        Assert.Equal(PermissionCodes.Business.AfterSales.Resource, resource.Resource);

        var expected = new Dictionary<string, string>
        {
            [nameof(AfterSalesController.GetPaged)] = PermissionActions.Read,
            [nameof(AfterSalesController.GetById)] = PermissionActions.Read,
            [nameof(AfterSalesController.Create)] = PermissionActions.Create,
            [nameof(AfterSalesController.Update)] = PermissionActions.Update,
            [nameof(AfterSalesController.Delete)] = PermissionActions.Delete,
            [nameof(AfterSalesController.Complete)] = PermissionActions.Update
        };
        foreach (var (actionName, permissionAction) in expected)
        {
            var attribute = Assert.Single(GetAction(actionName).GetCustomAttributes<ResourcePermissionAttribute>());
            Assert.Equal(permissionAction, attribute.Action);
        }
    }

    [Fact]
    public void AfterSalesController_StateReviewActionsRequireAuditPermission()
    {
        var actionNames = new[]
        {
            nameof(AfterSalesController.Submit),
            nameof(AfterSalesController.Approve),
            nameof(AfterSalesController.Reject),
            nameof(AfterSalesController.Resubmit),
            nameof(AfterSalesController.Reverse)
        };

        foreach (var actionName in actionNames)
        {
            var authorize = Assert.Single(
                GetAction(actionName).GetCustomAttributes<AuthorizeAttribute>(),
                attribute => !string.IsNullOrWhiteSpace(attribute.Policy));
            Assert.Equal(PermissionCodes.Business.AfterSales.Audit, authorize.Policy);
        }
    }

    private static MethodInfo GetAction(string name)
    {
        return typeof(AfterSalesController).GetMethod(name)
               ?? throw new InvalidOperationException($"Action {name} was not found.");
    }
}
