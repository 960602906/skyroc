using System.Text.Json;
using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shared.Constants;
using Xunit;

namespace SkyRoc.Tests.Documentation;

public class SwaggerResponseSchemaTests
{
    [Fact]
    public void SwaggerDocumentationFactory_UsesInMemoryDatabaseForAuditScope()
    {
        using var factory = new SwaggerDocumentationWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Assert.Equal("Microsoft.EntityFrameworkCore.InMemory", context.Database.ProviderName);
    }

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
        Assert.True(schemas.TryGetProperty("AfterSaleListItemDto", out _));
        Assert.True(schemas.TryGetProperty("AfterSaleListGoodsDto", out _));
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
        Assert.True(paths.TryGetProperty("/api/after-sales/pickup-tasks/{id}", out _));
        Assert.True(paths.TryGetProperty("/api/after-sales/pickup-tasks/{id}/assign", out _));
        Assert.True(paths.TryGetProperty("/api/after-sales/pickup-tasks/{id}/start", out _));
        Assert.True(paths.TryGetProperty("/api/after-sales/pickup-tasks/{id}/complete", out _));

        var operation = paths.GetProperty("/api/after-sales").GetProperty("get");
        Assert.Contains(PermissionCodes.Business.AfterSales.Read, operation.GetProperty("description").GetString());
        Assert.Contains("售后", operation.GetProperty("tags").EnumerateArray().Select(x => x.GetString()));
        Assert.True(operation.GetProperty("responses").GetProperty("200").TryGetProperty("content", out _));
        Assert.Contains("AfterSaleListItemDto", operation.GetRawText());

        var detailOperation = paths.GetProperty("/api/after-sales/{id}").GetProperty("get");
        Assert.Contains("AfterSaleDto", detailOperation.GetRawText());

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

    [Fact]
    public async Task Swagger_DocumentsReportContracts()
    {
        using var factory = new SwaggerDocumentationWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        var root = document.RootElement;
        var paths = root.GetProperty("paths");
        var schemas = root.GetProperty("components").GetProperty("schemas");

        Assert.True(schemas.TryGetProperty("SalesGoodsSummaryDto", out _));
        Assert.True(schemas.TryGetProperty("SalesCategorySummaryDto", out _));
        Assert.True(schemas.TryGetProperty("SalesCustomerSummaryDto", out _));
        Assert.True(schemas.TryGetProperty("SalesAreaSummaryDto", out _));
        Assert.True(schemas.TryGetProperty("AfterSaleSummaryDto", out _));
        Assert.True(schemas.TryGetProperty("DailyStockInOutSummaryDto", out _));
        Assert.True(schemas.TryGetProperty("DailyGoodsStockInOutSummaryDto", out _));
        Assert.True(schemas.TryGetProperty("PurchaseInOutGoodsSummaryDto", out _));
        Assert.True(schemas.TryGetProperty("PurchaseInOutSupplierSummaryDto", out _));
        Assert.True(schemas.TryGetProperty("PurchaseInOutPurchaserSummaryDto", out _));
        Assert.True(schemas.TryGetProperty("DashboardBriefDto", out _));
        Assert.True(schemas.TryGetProperty("DashboardSalesTrendDto", out _));
        Assert.True(schemas.TryGetProperty("DashboardCustomerSalesRankDto", out _));
        Assert.True(schemas.TryGetProperty("DashboardGoodsTypeSalesRankDto", out _));
        Assert.True(schemas.TryGetProperty("DashboardReconciliationDto", out _));
        Assert.True(schemas.TryGetProperty("DashboardPickupStatusDto", out _));
        Assert.True(paths.TryGetProperty("/api/reports/sales/goods", out _));
        Assert.True(paths.TryGetProperty("/api/reports/sales/categories", out _));
        Assert.True(paths.TryGetProperty("/api/reports/sales/customers", out _));
        Assert.True(paths.TryGetProperty("/api/reports/sales/areas", out _));
        Assert.True(paths.TryGetProperty("/api/reports/after-sales", out _));
        Assert.True(paths.TryGetProperty("/api/reports/stock/daily", out _));
        Assert.True(paths.TryGetProperty("/api/reports/stock/daily-goods", out _));
        Assert.True(paths.TryGetProperty("/api/reports/purchase-in-out/goods", out _));
        Assert.True(paths.TryGetProperty("/api/reports/purchase-in-out/suppliers", out _));
        Assert.True(paths.TryGetProperty("/api/reports/purchase-in-out/purchasers", out _));
        Assert.True(paths.TryGetProperty("/api/dashboard/brief", out _));
        Assert.True(paths.TryGetProperty("/api/dashboard/sales-trend", out _));
        Assert.True(paths.TryGetProperty("/api/dashboard/customer-sales-rank", out _));
        Assert.True(paths.TryGetProperty("/api/dashboard/goods-type-sales-rank", out _));
        Assert.True(paths.TryGetProperty("/api/dashboard/reconciliation", out _));
        Assert.True(paths.TryGetProperty("/api/dashboard/pickup-statuses", out _));

        var reportPaths = new[]
        {
            "/api/reports/sales/goods",
            "/api/reports/sales/categories",
            "/api/reports/sales/customers",
            "/api/reports/sales/areas",
            "/api/reports/after-sales",
            "/api/reports/stock/daily",
            "/api/reports/stock/daily-goods",
            "/api/reports/purchase-in-out/goods",
            "/api/reports/purchase-in-out/suppliers",
            "/api/reports/purchase-in-out/purchasers"
        };
        foreach (var path in reportPaths)
        {
            var operation = paths.GetProperty(path).GetProperty("get");
            Assert.Contains(PermissionCodes.Business.Reports.Read, operation.GetProperty("description").GetString());
            Assert.Contains("报表", operation.GetProperty("tags").EnumerateArray().Select(x => x.GetString()));
            var successResponse = operation.GetProperty("responses").GetProperty("200");
            var content = successResponse.GetProperty("content").GetProperty("application/json");
            var schemaReference = content.GetProperty("schema").GetProperty("$ref").GetString();
            Assert.StartsWith("#/components/schemas/", schemaReference);
        }

        var dashboardPaths = new[]
        {
            "/api/dashboard/brief",
            "/api/dashboard/sales-trend",
            "/api/dashboard/customer-sales-rank",
            "/api/dashboard/goods-type-sales-rank",
            "/api/dashboard/reconciliation",
            "/api/dashboard/pickup-statuses"
        };
        foreach (var path in dashboardPaths)
        {
            var operation = paths.GetProperty(path).GetProperty("get");
            Assert.Contains(PermissionCodes.Business.Reports.Read, operation.GetProperty("description").GetString());
            Assert.Contains("首页驾驶舱", operation.GetProperty("tags").EnumerateArray().Select(x => x.GetString()));
            var successResponse = operation.GetProperty("responses").GetProperty("200");
            var content = successResponse.GetProperty("content").GetProperty("application/json");
            var schemaReference = content.GetProperty("schema").GetProperty("$ref").GetString();
            Assert.StartsWith("#/components/schemas/", schemaReference);
        }
    }

    [Fact]
    public async Task Swagger_DocumentsImportExportJobContracts()
    {
        using var factory = new SwaggerDocumentationWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        var root = document.RootElement;
        var paths = root.GetProperty("paths");
        var schemas = root.GetProperty("components").GetProperty("schemas");

        Assert.True(schemas.TryGetProperty("ImportExportJobDto", out var jobSchema));
        Assert.Contains("任务", jobSchema.GetProperty("description").GetString());
        Assert.True(paths.TryGetProperty("/api/import-export/jobs/templates/{jobType}", out _));
        Assert.True(paths.TryGetProperty("/api/import-export/jobs/import/{jobType}", out _));
        Assert.True(paths.TryGetProperty("/api/import-export/jobs/export/{jobType}", out _));
        Assert.True(paths.TryGetProperty("/api/import-export/jobs/{id}", out _));

        var templateOperation = paths.GetProperty("/api/import-export/jobs/templates/{jobType}").GetProperty("get");
        var importOperation = paths.GetProperty("/api/import-export/jobs/import/{jobType}").GetProperty("post");
        var exportOperation = paths.GetProperty("/api/import-export/jobs/export/{jobType}").GetProperty("get");
        var statusOperation = paths.GetProperty("/api/import-export/jobs/{id}").GetProperty("get");

        foreach (var operation in new[] { templateOperation, importOperation, exportOperation, statusOperation })
        {
            Assert.Contains("导入导出", operation.GetProperty("tags").EnumerateArray().Select(x => x.GetString()));
            Assert.Contains("body.code=401", operation.GetProperty("description").GetString());
            Assert.Contains("body.code=403", operation.GetProperty("description").GetString());
        }
        Assert.Contains(PermissionCodes.Business.ImportExport.Create, templateOperation.GetProperty("description").GetString());
        Assert.Contains(PermissionCodes.Business.ImportExport.Create, importOperation.GetProperty("description").GetString());
        Assert.Contains(PermissionCodes.Business.ImportExport.Read, exportOperation.GetProperty("description").GetString());
        Assert.Contains(PermissionCodes.Business.ImportExport.Read, statusOperation.GetProperty("description").GetString());
        Assert.True(templateOperation.GetProperty("responses").GetProperty("200").GetProperty("content").TryGetProperty("text/csv", out _));
        Assert.True(exportOperation.GetProperty("responses").GetProperty("200").GetProperty("content").TryGetProperty("text/csv", out _));
        Assert.True(importOperation.GetProperty("requestBody").GetProperty("content").TryGetProperty("multipart/form-data", out _));
        Assert.True(statusOperation.GetProperty("responses").GetProperty("200").TryGetProperty("content", out _));
    }

    [Fact]
    public async Task Swagger_DocumentsProtectedFileUploadContracts()
    {
        using var factory = new SwaggerDocumentationWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        var root = document.RootElement;
        var paths = root.GetProperty("paths");
        var schemas = root.GetProperty("components").GetProperty("schemas");

        Assert.True(schemas.TryGetProperty("StoredFileDto", out var fileSchema));
        Assert.Contains("文件", fileSchema.GetProperty("description").GetString());
        Assert.True(paths.TryGetProperty("/api/files", out _));
        Assert.True(paths.TryGetProperty("/api/files/{id}/download", out _));

        var upload = paths.GetProperty("/api/files").GetProperty("post");
        var download = paths.GetProperty("/api/files/{id}/download").GetProperty("get");
        foreach (var operation in new[] { upload, download })
        {
            Assert.Contains("文件上传", operation.GetProperty("tags").EnumerateArray().Select(x => x.GetString()));
            Assert.Contains("body.code=401", operation.GetProperty("description").GetString());
            Assert.Contains("body.code=403", operation.GetProperty("description").GetString());
        }
        Assert.Contains(PermissionCodes.Business.Files.Create, upload.GetProperty("description").GetString());
        Assert.Contains(PermissionCodes.Business.Files.Read, download.GetProperty("description").GetString());
        Assert.True(upload.GetProperty("requestBody").GetProperty("content").TryGetProperty("multipart/form-data", out _));
        Assert.True(download.GetProperty("responses").GetProperty("200").TryGetProperty("content", out _));
    }

    [Fact]
    public async Task Swagger_DocumentsPrintingTemplateAndDataContracts()
    {
        using var factory = new SwaggerDocumentationWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        var root = document.RootElement;
        var paths = root.GetProperty("paths");
        var schemas = root.GetProperty("components").GetProperty("schemas");

        Assert.True(schemas.TryGetProperty("PrintTemplateDto", out var templateSchema));
        Assert.Contains("打印", templateSchema.GetProperty("description").GetString());
        Assert.True(schemas.TryGetProperty("PrintDocumentDto", out _));
        Assert.True(paths.TryGetProperty("/api/print-templates", out _));
        Assert.True(paths.TryGetProperty("/api/print-templates/by-code/{templateCode}", out _));
        Assert.True(paths.TryGetProperty("/api/print-data/{businessType}", out _));
        Assert.True(paths.TryGetProperty("/api/print-data/{businessType}/confirm", out _));

        var list = paths.GetProperty("/api/print-templates").GetProperty("get");
        var create = paths.GetProperty("/api/print-templates").GetProperty("post");
        var data = paths.GetProperty("/api/print-data/{businessType}").GetProperty("get");
        var confirm = paths.GetProperty("/api/print-data/{businessType}/confirm").GetProperty("post");
        foreach (var operation in new[] { list, create, data, confirm })
        {
            Assert.Contains("打印", operation.GetProperty("tags").EnumerateArray().Select(x => x.GetString()));
            Assert.Contains("body.code=401", operation.GetProperty("description").GetString());
            Assert.Contains("body.code=403", operation.GetProperty("description").GetString());
            Assert.True(operation.GetProperty("responses").GetProperty("200").TryGetProperty("content", out _));
        }

        Assert.Contains(PermissionCodes.System.PrintTemplates.Read, list.GetProperty("description").GetString());
        Assert.Contains(PermissionCodes.System.PrintTemplates.Create, create.GetProperty("description").GetString());
        Assert.Contains(PermissionCodes.Business.Printing.Read, data.GetProperty("description").GetString());
        Assert.Contains(PermissionCodes.Business.Printing.Update, confirm.GetProperty("description").GetString());
    }

    [Fact]
    public async Task Swagger_DocumentsSystemSupportContracts()
    {
        using var factory = new SwaggerDocumentationWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        var root = document.RootElement;
        var paths = root.GetProperty("paths");
        var schemas = root.GetProperty("components").GetProperty("schemas");

        Assert.True(schemas.TryGetProperty("ServicePeriodDto", out _));
        Assert.True(schemas.TryGetProperty("NoticeDto", out var noticeSchema));
        Assert.True(schemas.TryGetProperty("OperationLogDto", out _));
        Assert.True(schemas.TryGetProperty("LoginLogDto", out _));
        Assert.Contains("通知", noticeSchema.GetProperty("description").GetString());
        Assert.True(paths.TryGetProperty("/api/system-settings/service-periods", out _));
        Assert.True(paths.TryGetProperty("/api/system-settings/mini-program-order", out _));
        Assert.True(paths.TryGetProperty("/api/system-settings/sorting-weights", out _));
        Assert.True(paths.TryGetProperty("/api/notices", out _));
        Assert.True(paths.TryGetProperty("/api/notices/{id}", out _));
        Assert.True(paths.TryGetProperty("/api/logs/operations", out _));
        Assert.True(paths.TryGetProperty("/api/logs/logins", out _));

        var settings = paths.GetProperty("/api/system-settings/service-periods").GetProperty("post");
        var notices = paths.GetProperty("/api/notices").GetProperty("post");
        var operations = paths.GetProperty("/api/logs/operations").GetProperty("get");
        foreach (var operation in new[] { settings, notices, operations })
        {
            Assert.Contains("系统支撑", operation.GetProperty("tags").EnumerateArray().Select(x => x.GetString()));
            Assert.Contains("body.code=401", operation.GetProperty("description").GetString());
            Assert.Contains("body.code=403", operation.GetProperty("description").GetString());
            Assert.True(operation.GetProperty("responses").GetProperty("200").TryGetProperty("content", out _));
        }
        Assert.Contains(PermissionCodes.System.Operations.Create, settings.GetProperty("description").GetString());
        Assert.Contains(PermissionCodes.System.Notices.Create, notices.GetProperty("description").GetString());
        Assert.Contains(PermissionCodes.System.Logs.Read, operations.GetProperty("description").GetString());
    }
}
