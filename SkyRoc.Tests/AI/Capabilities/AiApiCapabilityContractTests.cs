using System.Reflection;
using System.Text.Json;
using Application.AI.Capabilities;
using Xunit;

namespace SkyRoc.Tests.AI.Capabilities;

/// <summary>验证能力调用契约可序列化且不能表达传输、身份或权限注入字段。</summary>
public class AiApiCapabilityContractTests
{
    private static readonly string[] ForbiddenRequestMemberNames =
    [
        "Url", "Uri", "Method", "HttpMethod", "Header", "Headers", "UserId", "Permission", "Permissions", "Token"
    ];

    [Fact]
    public void Contracts_RoundTripWithoutProviderSpecificTypes()
    {
        using var arguments = JsonDocument.Parse("""{"id":"42"}""");
        using var data = JsonDocument.Parse("""{"name":"测试客户"}""");
        var request = new AiApiOperationRequest
        {
            OperationId = "CustomersController.GetById",
            Arguments = arguments.RootElement.Clone()
        };
        var result = new AiApiOperationResult
        {
            Code = 200,
            Message = "操作成功",
            Data = data.RootElement.Clone(),
            PayloadBytes = 25
        };

        var requestJson = JsonSerializer.Serialize(request);
        var resultJson = JsonSerializer.Serialize(result);
        var requestCopy = JsonSerializer.Deserialize<AiApiOperationRequest>(requestJson);
        var resultCopy = JsonSerializer.Deserialize<AiApiOperationResult>(resultJson);

        Assert.Equal(request.OperationId, requestCopy!.OperationId);
        Assert.Equal("42", requestCopy.Arguments.GetProperty("id").GetString());
        Assert.Equal(200, resultCopy!.Code);
        Assert.Equal("测试客户", resultCopy.Data!.Value.GetProperty("name").GetString());
        Assert.DoesNotContain("DeepSeek", requestJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("OpenAI", resultJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OperationRequest_ContainsOnlyOperationIdAndBusinessArguments()
    {
        var properties = typeof(AiApiOperationRequest)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public);

        Assert.Equal(
            [nameof(AiApiOperationRequest.Arguments), nameof(AiApiOperationRequest.OperationId)],
            properties.Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray());
        Assert.DoesNotContain(properties, property => ForbiddenRequestMemberNames.Any(forbidden =>
            property.Name.Contains(forbidden, StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void Capability_UsesStableOperationIdAndExistingPermissionMetadata()
    {
        var capability = new AiApiCapability
        {
            OperationId = "SaleOrdersController.GetById",
            Title = "查询销售订单",
            Category = "销售订单",
            HttpMethod = "GET",
            RiskLevel = AiOperationRiskLevel.Read,
            ConfirmationMode = AiConfirmationMode.None,
            PermissionResource = "business:order",
            PermissionAction = "read",
            RequiresAuthorization = true
        };

        var json = JsonSerializer.Serialize(capability);
        var copy = JsonSerializer.Deserialize<AiApiCapability>(json);

        Assert.Equal(capability.OperationId, copy!.OperationId);
        Assert.Equal("business:order", copy.PermissionResource);
        Assert.True(copy.RequiresAuthorization);
    }

    [Fact]
    public void OperationId_UsesControllerAndActionAndRejectsDuplicates()
    {
        var operationId = AiOperationId.Create("CustomersController", "GetById");

        Assert.Equal("CustomersController.GetById", operationId);
        var exception = Assert.Throws<InvalidOperationException>(() =>
            AiOperationId.EnsureUnique([operationId, "customerscontroller.getbyid"]));
        Assert.Contains("重复", exception.Message, StringComparison.Ordinal);
    }
}
