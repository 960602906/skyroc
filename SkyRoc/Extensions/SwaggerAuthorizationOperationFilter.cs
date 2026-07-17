using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;
using SkyRoc.Authorization;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SkyRoc.Extensions;

/// <summary>
///     在 Swagger 操作上标记认证要求和细粒度权限编码。
/// </summary>
public sealed class SwaggerAuthorizationOperationFilter : IOperationFilter
{
    /// <inheritdoc />
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.ApiDescription.ActionDescriptor is not ControllerActionDescriptor actionDescriptor)
            return;

        var methodAttributes = actionDescriptor.MethodInfo.GetCustomAttributes(true);
        if (methodAttributes.OfType<IAllowAnonymous>().Any())
            return;

        var controllerAttributes = actionDescriptor.ControllerTypeInfo.GetCustomAttributes(true);
        var authorizeData = controllerAttributes.Concat(methodAttributes).OfType<IAuthorizeData>().ToList();
        if (authorizeData.Count == 0)
            return;

        operation.Security =
        [
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                }] = []
            }
        ];
        // 业务约定：HTTP 始终 200；未认证/无权限分别体现在 body.code = 401/403
        operation.Description = string.IsNullOrWhiteSpace(operation.Description)
            ? "HTTP 200；未认证时 body.code=401，无权限时 body.code=403"
            : $"{operation.Description}\n\nHTTP 200；未认证时 body.code=401，无权限时 body.code=403";

        var permissionCode = ResolvePermissionCode(actionDescriptor, authorizeData);
        if (permissionCode is null)
            return;

        var permissionDescription = $"所需权限：`{permissionCode}`";
        operation.Description = string.IsNullOrWhiteSpace(operation.Description)
            ? permissionDescription
            : $"{operation.Description}\n\n{permissionDescription}";
    }

    private static string? ResolvePermissionCode(
        ControllerActionDescriptor actionDescriptor,
        IEnumerable<IAuthorizeData> authorizeData)
    {
        var policy = authorizeData
            .Select(data => data.Policy)
            .LastOrDefault(value => !string.IsNullOrWhiteSpace(value));
        if (policy is not null)
            return policy;

        var resourcePermission = actionDescriptor.MethodInfo
            .GetCustomAttributes<ResourcePermissionAttribute>(true)
            .SingleOrDefault();
        var resource = actionDescriptor.ControllerTypeInfo
            .GetCustomAttribute<PermissionResourceAttribute>(true);

        return resourcePermission is not null && resource is not null
            ? $"{resource.Resource}:{resourcePermission.Action}"
            : null;
    }
}
