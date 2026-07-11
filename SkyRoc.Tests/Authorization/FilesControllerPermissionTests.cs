using System.Reflection;
using Shared.Constants;
using SkyRoc.Authorization;
using SkyRoc.Controllers;
using Xunit;

namespace SkyRoc.Tests.Authorization;

/// <summary>
/// 防止受保护文件接口遗漏上传或下载的细粒度权限。
/// </summary>
public class FilesControllerPermissionTests
{
    [Fact]
    public void FilesController_DeclaresResourceAndActionPermissions()
    {
        var resource = Assert.Single(typeof(FilesController).GetCustomAttributes<PermissionResourceAttribute>());
        Assert.Equal(PermissionCodes.Business.Files.Resource, resource.Resource);

        var expected = new Dictionary<string, string>
        {
            [nameof(FilesController.Upload)] = PermissionActions.Create,
            [nameof(FilesController.Download)] = PermissionActions.Read
        };
        foreach (var (actionName, actionPermission) in expected)
        {
            var action = typeof(FilesController).GetMethod(actionName)
                         ?? throw new InvalidOperationException($"{actionName} 不存在。");
            var attribute = Assert.Single(action.GetCustomAttributes<ResourcePermissionAttribute>());
            Assert.Equal(actionPermission, attribute.Action);
        }
    }
}
