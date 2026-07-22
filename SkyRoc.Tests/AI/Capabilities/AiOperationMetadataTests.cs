using Application.AI.Capabilities;
using SkyRoc.AI.Attributes;
using Xunit;

namespace SkyRoc.Tests.AI.Capabilities;

/// <summary>验证 HTTP 默认风险、显式收紧规则和端点排除原因。</summary>
public class AiOperationMetadataTests
{
    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    public void Resolve_ClassifiesReadMethodsWithoutConfirmation(string method)
    {
        var metadata = AiOperationMetadataResolver.Resolve(method);

        Assert.Equal(AiOperationRiskLevel.Read, metadata.RiskLevel);
        Assert.Equal(AiConfirmationMode.None, metadata.ConfirmationMode);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public void Resolve_ClassifiesWriteMethodsAsConfirmationRequired(string method)
    {
        var metadata = AiOperationMetadataResolver.Resolve(method);

        Assert.Equal(AiOperationRiskLevel.Write, metadata.RiskLevel);
        Assert.Equal(AiConfirmationMode.Required, metadata.ConfirmationMode);
    }

    [Fact]
    public void Resolve_AppliesHighRiskAndProjectionOverrides()
    {
        var metadata = AiOperationMetadataResolver.Resolve("POST", new AiOperationAttribute
        {
            Title = "作废结算",
            Category = "财务",
            RiskLevel = AiOperationRiskLevel.HighRisk,
            MaxResultItems = 5,
            SensitiveFieldPolicy = AiSensitiveFieldPolicy.Remove
        });

        Assert.Equal("作废结算", metadata.Title);
        Assert.Equal("财务", metadata.Category);
        Assert.Equal(AiOperationRiskLevel.HighRisk, metadata.RiskLevel);
        Assert.Equal(AiConfirmationMode.HighRisk, metadata.ConfirmationMode);
        Assert.Equal(5, metadata.MaxResultItems);
        Assert.Equal(AiSensitiveFieldPolicy.Remove, metadata.SensitiveFieldPolicy);
    }

    [Fact]
    public void Resolve_RejectsMetadataThatWeakensWriteConfirmation()
    {
        var attribute = new AiOperationAttribute
        {
            RiskLevel = AiOperationRiskLevel.Read,
            ConfirmationMode = AiConfirmationMode.None
        };

        Assert.Throws<InvalidOperationException>(() =>
            AiOperationMetadataResolver.Resolve("DELETE", attribute));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AiIgnore_RejectsEmptyReason(string reason)
    {
        Assert.Throws<ArgumentException>(() => new AiIgnoreAttribute(reason));
    }

    [Fact]
    public void AiIgnore_TrimsAndPreservesReason()
    {
        var attribute = new AiIgnoreAttribute("  文件流需要专用适配器  ");

        Assert.Equal("文件流需要专用适配器", attribute.Reason);
    }
}
