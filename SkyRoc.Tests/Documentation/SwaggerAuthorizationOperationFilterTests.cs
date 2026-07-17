using System.Reflection;
using Application.DTOs.Goods;
using Application.QueryParameters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;
using Shared.Constants;
using SkyRoc.Controllers;
using SkyRoc.Extensions;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xunit;

namespace SkyRoc.Tests.Documentation;

public class SwaggerAuthorizationOperationFilterTests
{
    [Fact]
    public void Apply_DoesNotRequireBearer_ForAnonymousAction()
    {
        var operation = ApplyFilter(
            typeof(AuthController),
            nameof(AuthController.Login));

        Assert.Empty(operation.Security);
        Assert.DoesNotContain("401", operation.Responses.Keys);
        Assert.DoesNotContain("403", operation.Responses.Keys);
    }

    [Fact]
    public void Apply_DocumentsBearerAndPermission_ForBaseDataAction()
    {
        var operation = ApplyFilter(
            typeof(GoodsController),
            nameof(BaseDataController<GoodsDto, CreateGoodsDto, UpdateGoodsDto, GoodsQueryParameters>.Create));

        var securityRequirement = Assert.Single(operation.Security);
        var securityScheme = Assert.Single(securityRequirement.Keys);
        Assert.Equal("Bearer", securityScheme.Reference.Id);
        Assert.Contains("body.code=401", operation.Description);
        Assert.Contains("body.code=403", operation.Description);
        Assert.DoesNotContain("401", operation.Responses.Keys);
        Assert.DoesNotContain("403", operation.Responses.Keys);
        Assert.Contains(PermissionCodes.Business.Goods.Create, operation.Description);
    }

    private static OpenApiOperation ApplyFilter(Type controllerType, string methodName)
    {
        var method = controllerType.GetMethod(methodName)
                     ?? throw new InvalidOperationException($"Action {controllerType.Name}.{methodName} was not found.");
        var actionDescriptor = new ControllerActionDescriptor
        {
            ControllerTypeInfo = controllerType.GetTypeInfo(),
            MethodInfo = method
        };
        var apiDescription = new ApiDescription { ActionDescriptor = actionDescriptor };
        var context = new OperationFilterContext(apiDescription, null!, new SchemaRepository(), method);
        var operation = new OpenApiOperation { Responses = new OpenApiResponses() };

        new SwaggerAuthorizationOperationFilter().Apply(operation, context);

        return operation;
    }
}
