using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Shared.Constants;
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

    [Fact]
    public async Task Swagger_DocumentsDeliveryTaskAndExceptionContracts()
    {
        using var factory = new SwaggerDocumentationWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        var root = document.RootElement;
        var paths = root.GetProperty("paths");
        var schemas = root.GetProperty("components").GetProperty("schemas");

        Assert.True(schemas.TryGetProperty("DeliveryTaskDto", out _));
        Assert.True(schemas.TryGetProperty("CreateDeliveryExceptionDto", out _));
        Assert.True(schemas.TryGetProperty("SignDeliveryTaskDto", out _));
        Assert.True(schemas.TryGetProperty("OrderReceiptDto", out _));
        Assert.True(paths.TryGetProperty("/api/delivery-tasks/generate/{stockOutOrderId}", out _));
        Assert.True(paths.TryGetProperty("/api/delivery-tasks/intelligent-plan", out _));
        Assert.True(paths.TryGetProperty("/api/delivery-tasks/{id}/start", out _));
        Assert.True(paths.TryGetProperty("/api/delivery-tasks/{id}/sign", out _));
        Assert.True(paths.TryGetProperty("/api/delivery-tasks/{id}/receipt", out _));
        Assert.True(paths.TryGetProperty("/api/delivery-exceptions", out _));
        Assert.True(paths.TryGetProperty("/api/delivery-exceptions/{id}/handle", out _));

        var operation = paths.GetProperty("/api/delivery-tasks").GetProperty("get");
        Assert.Contains(PermissionCodes.Business.Delivery.Read, operation.GetProperty("description").GetString());
        Assert.Contains("配送", operation.GetProperty("tags").EnumerateArray().Select(x => x.GetString()));
        Assert.True(operation.GetProperty("responses").GetProperty("200").TryGetProperty("content", out _));
    }

    [Fact]
    public async Task Swagger_DocumentsAfterSaleStateMachineContracts()
    {
        using var factory = new SwaggerDocumentationWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        var root = document.RootElement;
        var paths = root.GetProperty("paths");
        var schemas = root.GetProperty("components").GetProperty("schemas");

        Assert.True(schemas.TryGetProperty("AfterSaleDto", out _));
        Assert.True(schemas.TryGetProperty("PickupTaskDto", out _));
        Assert.True(schemas.TryGetProperty("AssignPickupTaskDto", out _));
        Assert.True(schemas.TryGetProperty("CreateAfterSaleDto", out var createSchema));
        Assert.Contains("待提交", createSchema.GetProperty("description").GetString());
        Assert.True(paths.TryGetProperty("/api/after-sales", out _));
        Assert.True(paths.TryGetProperty("/api/after-sales/{id}/submit", out _));
        Assert.True(paths.TryGetProperty("/api/after-sales/{id}/approve", out _));
        Assert.True(paths.TryGetProperty("/api/after-sales/{id}/reject", out _));
        Assert.True(paths.TryGetProperty("/api/after-sales/{id}/resubmit", out _));
        Assert.True(paths.TryGetProperty("/api/after-sales/{id}/reverse", out _));
        Assert.True(paths.TryGetProperty("/api/after-sales/{id}/complete", out _));
        Assert.True(paths.TryGetProperty("/api/after-sales/pickup-tasks", out _));
        Assert.True(paths.TryGetProperty("/api/after-sales/pickup-tasks/{id}/assign", out _));
        Assert.True(paths.TryGetProperty("/api/after-sales/pickup-tasks/{id}/start", out _));
        Assert.True(paths.TryGetProperty("/api/after-sales/pickup-tasks/{id}/complete", out _));

        var operation = paths.GetProperty("/api/after-sales").GetProperty("get");
        Assert.Contains(PermissionCodes.Business.AfterSales.Read, operation.GetProperty("description").GetString());
        Assert.Contains("售后", operation.GetProperty("tags").EnumerateArray().Select(x => x.GetString()));
        Assert.True(operation.GetProperty("responses").GetProperty("200").TryGetProperty("content", out _));

        var pickupOperation = paths.GetProperty("/api/after-sales/pickup-tasks").GetProperty("get");
        Assert.Contains(PermissionCodes.Business.AfterSales.Read, pickupOperation.GetProperty("description").GetString());
        Assert.Contains("售后", pickupOperation.GetProperty("tags").EnumerateArray().Select(x => x.GetString()));
    }

    [Fact]
    public async Task Swagger_DocumentsCustomerSettlementContracts()
    {
        using var factory = new SwaggerDocumentationWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        var root = document.RootElement;
        var paths = root.GetProperty("paths");
        var schemas = root.GetProperty("components").GetProperty("schemas");

        Assert.True(schemas.TryGetProperty("CustomerBillDto", out _));
        Assert.True(schemas.TryGetProperty("CustomerSettlementDto", out _));
        Assert.True(schemas.TryGetProperty("CreateCustomerSettlementDto", out var createSchema));
        Assert.Contains("客户结款凭证", createSchema.GetProperty("description").GetString());
        Assert.True(paths.TryGetProperty("/api/customer-settlements/bills", out _));
        Assert.True(paths.TryGetProperty("/api/customer-settlements", out _));
        Assert.True(paths.TryGetProperty("/api/customer-settlements/{id}", out _));
        Assert.True(paths.TryGetProperty("/api/customer-settlements/{id}/void", out _));

        var listOperation = paths.GetProperty("/api/customer-settlements").GetProperty("get");
        var createOperation = paths.GetProperty("/api/customer-settlements").GetProperty("post");
        var voidOperation = paths.GetProperty("/api/customer-settlements/{id}/void").GetProperty("delete");
        Assert.Contains(PermissionCodes.Business.Finance.Read, listOperation.GetProperty("description").GetString());
        Assert.Contains(PermissionCodes.Business.Finance.Create, createOperation.GetProperty("description").GetString());
        Assert.Contains(PermissionCodes.Business.Finance.Delete, voidOperation.GetProperty("description").GetString());
        Assert.Contains("财务", listOperation.GetProperty("tags").EnumerateArray().Select(x => x.GetString()));
        Assert.True(listOperation.GetProperty("responses").GetProperty("200").TryGetProperty("content", out _));
    }

    [Fact]
    public async Task Swagger_DocumentsSupplierSettlementContracts()
    {
        using var factory = new SwaggerDocumentationWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        var root = document.RootElement;
        var paths = root.GetProperty("paths");
        var schemas = root.GetProperty("components").GetProperty("schemas");

        Assert.True(schemas.TryGetProperty("SupplierBillDto", out _));
        Assert.True(schemas.TryGetProperty("SupplierSettlementDto", out _));
        Assert.True(schemas.TryGetProperty("CreateSupplierSettlementDto", out var createSchema));
        Assert.Contains("供应商结算单", createSchema.GetProperty("description").GetString());
        Assert.True(paths.TryGetProperty("/api/supplier-settlements/bills", out _));
        Assert.True(paths.TryGetProperty("/api/supplier-settlements", out _));
        Assert.True(paths.TryGetProperty("/api/supplier-settlements/{id}", out _));
        Assert.True(paths.TryGetProperty("/api/supplier-settlements/{id}/void", out _));

        var listOperation = paths.GetProperty("/api/supplier-settlements").GetProperty("get");
        var createOperation = paths.GetProperty("/api/supplier-settlements").GetProperty("post");
        var voidOperation = paths.GetProperty("/api/supplier-settlements/{id}/void").GetProperty("delete");
        Assert.Contains(PermissionCodes.Business.Finance.Read, listOperation.GetProperty("description").GetString());
        Assert.Contains(PermissionCodes.Business.Finance.Create, createOperation.GetProperty("description").GetString());
        Assert.Contains(PermissionCodes.Business.Finance.Delete, voidOperation.GetProperty("description").GetString());
        Assert.Contains("财务", listOperation.GetProperty("tags").EnumerateArray().Select(x => x.GetString()));
        Assert.True(listOperation.GetProperty("responses").GetProperty("200").TryGetProperty("content", out _));
    }

    [Fact]
    public async Task Swagger_DocumentsTraceabilityContractsAndPublicQrCode()
    {
        using var factory = new SwaggerDocumentationWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        var root = document.RootElement;
        var paths = root.GetProperty("paths");
        var schemas = root.GetProperty("components").GetProperty("schemas");

        Assert.True(schemas.TryGetProperty("InspectionReportDto", out _));
        Assert.True(schemas.TryGetProperty("TraceRecordDto", out _));
        Assert.True(schemas.TryGetProperty("ExternalPushLogDto", out _));
        Assert.True(paths.TryGetProperty("/api/traceability/inspection-reports", out _));
        Assert.True(paths.TryGetProperty("/api/traceability/traces", out _));
        Assert.True(paths.TryGetProperty("/api/traceability/traces/qr/{traceNo}", out _));
        Assert.True(paths.TryGetProperty("/api/traceability/push-logs", out _));

        var reportOperation = paths.GetProperty("/api/traceability/inspection-reports").GetProperty("get");
        Assert.Contains(PermissionCodes.Business.Traceability.Read, reportOperation.GetProperty("description").GetString());
        Assert.Contains("溯源", reportOperation.GetProperty("tags").EnumerateArray().Select(x => x.GetString()));
        Assert.True(reportOperation.GetProperty("responses").GetProperty("200").TryGetProperty("content", out _));

        var qrOperation = paths.GetProperty("/api/traceability/traces/qr/{traceNo}").GetProperty("get");
        Assert.False(qrOperation.TryGetProperty("security", out _));
        Assert.True(qrOperation.GetProperty("responses").GetProperty("200").TryGetProperty("content", out _));
    }
}
