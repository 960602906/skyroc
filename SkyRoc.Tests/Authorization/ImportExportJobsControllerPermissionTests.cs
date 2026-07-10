using System.Reflection;
using Shared.Constants;
using SkyRoc.Authorization;
using SkyRoc.Controllers;
using Xunit;

namespace SkyRoc.Tests.Authorization;

/// <summary>防止统一导入导出接口丢失商品写入或任务读取的细粒度权限。</summary>
public class ImportExportJobsControllerPermissionTests
{
    [Fact]
    public void ImportExportJobsController_DeclaresResourceAndActionPermissions()
    {
        var resource = Assert.Single(typeof(ImportExportJobsController).GetCustomAttributes<PermissionResourceAttribute>());
        Assert.Equal(PermissionCodes.Business.ImportExport.Resource, resource.Resource);

        var expected = new Dictionary<string, string>
        {
            [nameof(ImportExportJobsController.DownloadTemplate)] = PermissionActions.Create,
            [nameof(ImportExportJobsController.Import)] = PermissionActions.Create,
            [nameof(ImportExportJobsController.Export)] = PermissionActions.Read,
            [nameof(ImportExportJobsController.GetById)] = PermissionActions.Read
        };
        foreach (var (actionName, permissionAction) in expected)
        {
            var action = typeof(ImportExportJobsController).GetMethod(actionName)
                         ?? throw new InvalidOperationException($"{actionName} 不存在。");
            var attribute = Assert.Single(action.GetCustomAttributes<ResourcePermissionAttribute>());
            Assert.Equal(permissionAction, attribute.Action);
        }
    }
}
