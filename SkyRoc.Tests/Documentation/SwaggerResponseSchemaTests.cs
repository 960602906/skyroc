using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace SkyRoc.Tests.Documentation;

public class SwaggerResponseSchemaTests
{
    [Fact]
    public async Task Swagger_DocumentsApiResponseWrapper_ForOrderDetail()
    {
        using var factory = new SwaggerDocumentationWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        var root = document.RootElement;
        var schemas = root.GetProperty("components").GetProperty("schemas");

        Assert.True(schemas.TryGetProperty("SaleOrderDto", out _));
        Assert.Contains(
            schemas.EnumerateObject(),
            p => p.Name.Contains("ApiResponse", StringComparison.Ordinal)
                 && p.Name.Contains("SaleOrderDto", StringComparison.Ordinal));

        var getOperation = root.GetProperty("paths").GetProperty("/api/orders/{id}").GetProperty("get");
        var okResponse = getOperation.GetProperty("responses").GetProperty("200");
        Assert.True(okResponse.TryGetProperty("content", out var content));
        Assert.True(content.TryGetProperty("application/json", out var mediaType));
        Assert.True(mediaType.TryGetProperty("schema", out _));
    }

    [Fact]
    public async Task Swagger_IncludesApplicationDtoDescriptions_ForCreateOrderRequest()
    {
        using var factory = new SwaggerDocumentationWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        var createSchema = document.RootElement
            .GetProperty("components")
            .GetProperty("schemas")
            .GetProperty("CreateSaleOrderDto");

        Assert.Equal("创建销售订单 DTO。", createSchema.GetProperty("description").GetString());
    }

    [Fact]
    public async Task Swagger_DocumentsGetRoutesResponse_WithStrongType()
    {
        using var factory = new SwaggerDocumentationWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");

        Assert.True(schemas.TryGetProperty("GetRoutesResDto", out var routesDto));
        Assert.Contains("路由", routesDto.GetProperty("description").GetString());
    }

    [Fact]
    public async Task Swagger_GroupsOperationsByBusinessTags()
    {
        using var factory = new SwaggerDocumentationWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        var listOperation = document.RootElement
            .GetProperty("paths")
            .GetProperty("/api/orders/list")
            .GetProperty("get");

        var tags = listOperation.GetProperty("tags").EnumerateArray().Select(t => t.GetString()).ToList();
        Assert.Contains("销售订单", tags);
    }
}
