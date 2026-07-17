using Application.Exceptions;
using Application.Services;
using Shared.Constants;
using Xunit;

namespace SkyRoc.Tests.Common;

/// <summary>
/// 单据编号统一生成器行为测试。
/// </summary>
public class DocumentNoGeneratorServiceTests
{
    private readonly DocumentNoGeneratorService _generator = new();

    [Theory]
    [InlineData(DocumentNoKind.SaleOrder, "SO", 31)]
    [InlineData(DocumentNoKind.PurchaseOrder, "PO", 31)]
    [InlineData(DocumentNoKind.StockOut, "OUT", 32)]
    [InlineData(DocumentNoKind.Stocktaking, "STK", 32)]
    [InlineData(DocumentNoKind.DeliveryTask, "DT", 29)]
    [InlineData(DocumentNoKind.CustomerBill, "CB", 29)]
    public async Task NextAsync_ReturnsPrefixedNumber_WithExpectedLength(
        DocumentNoKind kind,
        string prefix,
        int expectedLength)
    {
        var number = await _generator.NextAsync(kind, _ => Task.FromResult(false));

        Assert.StartsWith(prefix, number);
        Assert.Equal(expectedLength, number.Length);
        Assert.Equal(number.ToUpperInvariant(), number);
    }

    [Theory]
    [InlineData(DocumentNoKind.InspectionReport, "IR", 40)]
    [InlineData(DocumentNoKind.TraceRecord, "TR", 40)]
    [InlineData(DocumentNoKind.PickupTask, "PU", 42)]
    [InlineData(DocumentNoKind.ImportExportJob, "IE", 48)]
    public async Task NextAsync_TruncatesToMaxLength_ForConfiguredKinds(
        DocumentNoKind kind,
        string prefix,
        int maxLength)
    {
        var number = await _generator.NextAsync(kind, _ => Task.FromResult(false));

        Assert.StartsWith(prefix, number);
        Assert.Equal(maxLength, number.Length);
        Assert.Equal(number.ToUpperInvariant(), number);
    }

    [Fact]
    public async Task NextAsync_RetriesUntilUnusedNumberFound()
    {
        var attempts = 0;
        var number = await _generator.NextAsync(DocumentNoKind.SaleOrder, _ =>
        {
            attempts++;
            return Task.FromResult(attempts < 3);
        });

        Assert.Equal(3, attempts);
        Assert.StartsWith("SO", number);
    }

    [Fact]
    public async Task NextAsync_ThrowsBusinessException_WhenAllAttemptsConflict()
    {
        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            _generator.NextAsync(DocumentNoKind.SaleOrder, _ => Task.FromResult(true)));

        Assert.Equal("订单号生成失败，请重试", exception.Message);
    }

    [Fact]
    public async Task NextAsync_ThrowsArgumentNullException_WhenExistsCheckIsNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _generator.NextAsync(DocumentNoKind.SaleOrder, null!));
    }
}
