using Application.DTOs.Orders;
using Application.Validator;
using Xunit;

namespace SkyRoc.Tests.Orders;

public class OrderValidatorTests
{
    [Fact]
    public async Task Create_validator_accepts_complete_order()
    {
        var orderDate = new DateTime(2026, 7, 2, 8, 0, 0, DateTimeKind.Utc);
        var input = new CreateSaleOrderDto
        {
            CustomerId = Guid.NewGuid(),
            OrderDate = orderDate,
            ReceiveDate = orderDate.AddDays(1),
            Details =
            [
                new CreateSaleOrderDetailDto
                {
                    GoodsId = Guid.NewGuid(),
                    GoodsUnitId = Guid.NewGuid(),
                    FixedGoodsUnitId = Guid.NewGuid(),
                    Quantity = 2m,
                    FixedPrice = 10m
                }
            ]
        };

        var result = await new CreateSaleOrderValidator().ValidateAsync(input);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Create_validator_rejects_missing_customer_and_invalid_detail()
    {
        var input = new CreateSaleOrderDto
        {
            OrderDate = DateTime.UtcNow,
            Details =
            [
                new CreateSaleOrderDetailDto
                {
                    Quantity = 0,
                    FixedPrice = -1m
                }
            ]
        };

        var result = await new CreateSaleOrderValidator().ValidateAsync(input);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateSaleOrderDto.CustomerId));
        Assert.Contains(result.Errors, error => error.PropertyName == "Details[0].GoodsId");
        Assert.Contains(result.Errors, error => error.PropertyName == "Details[0].GoodsUnitId");
        Assert.Contains(result.Errors, error => error.PropertyName == "Details[0].Quantity");
        Assert.Contains(result.Errors, error => error.PropertyName == "Details[0].FixedPrice");
        Assert.Contains(result.Errors, error => error.PropertyName == "Details[0].FixedGoodsUnitId");
    }

    [Fact]
    public async Task Update_validator_rejects_missing_id_and_receive_date_before_order_date()
    {
        var orderDate = DateTime.UtcNow;
        var input = new UpdateSaleOrderDto
        {
            CustomerId = Guid.NewGuid(),
            OrderDate = orderDate,
            ReceiveDate = orderDate.AddMinutes(-1),
            Details =
            [
                new UpdateSaleOrderDetailDto
                {
                    GoodsId = Guid.NewGuid(),
                    GoodsUnitId = Guid.NewGuid(),
                    FixedGoodsUnitId = Guid.NewGuid(),
                    Quantity = 1m
                }
            ]
        };

        var result = await new UpdateSaleOrderValidator().ValidateAsync(input);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateSaleOrderDto.Id));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateSaleOrderDto.ReceiveDate));
    }

    [Fact]
    public async Task Update_validator_allows_new_detail_without_id()
    {
        var input = new UpdateSaleOrderDto
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            OrderDate = DateTime.UtcNow,
            Details =
            [
                new UpdateSaleOrderDetailDto
                {
                    GoodsId = Guid.NewGuid(),
                    GoodsUnitId = Guid.NewGuid(),
                    FixedGoodsUnitId = Guid.NewGuid(),
                    Quantity = 1m
                }
            ]
        };

        var result = await new UpdateSaleOrderValidator().ValidateAsync(input);

        Assert.True(result.IsValid);
    }
}
