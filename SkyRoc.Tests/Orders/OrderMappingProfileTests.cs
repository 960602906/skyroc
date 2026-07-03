using Application.DTOs.Orders;
using Application.Mappers;
using AutoMapper;
using Domain.Entities.Orders;
using Domain.Entities.Storage;
using Xunit;

namespace SkyRoc.Tests.Orders;

public class OrderMappingProfileTests
{
    private readonly IMapper mapper;

    public OrderMappingProfileTests()
    {
        var configuration = new MapperConfiguration(config => config.AddProfile<OrderMappingProfile>());
        configuration.AssertConfigurationIsValid();
        mapper = configuration.CreateMapper();
    }

    [Fact]
    public void SaleOrder_mapping_uses_historical_snapshots_and_nested_records()
    {
        var detailId = Guid.NewGuid();
        var order = new SaleOrder
        {
            Id = Guid.NewGuid(),
            OrderNo = "SO202607020001",
            CustomerId = Guid.NewGuid(),
            CustomerNameSnapshot = "学校客户",
            CustomerCodeSnapshot = "SCHOOL_001",
            Ware = new Ware { Id = Guid.NewGuid(), Name = "中心仓", Code = "MAIN" },
            ContactNameSnapshot = "张老师",
            ContactPhoneSnapshot = "13800000000",
            DeliveryAddressSnapshot = "学校食堂",
            Details =
            [
                new SaleOrderDetail
                {
                    Id = detailId,
                    GoodsId = Guid.NewGuid(),
                    GoodsNameSnapshot = "番茄",
                    GoodsCodeSnapshot = "TOMATO",
                    GoodsUnitId = Guid.NewGuid(),
                    GoodsUnitNameSnapshot = "千克",
                    Quantity = 10m,
                    FixedPrice = 8.5m,
                    TotalPrice = 85m
                }
            ],
            AuditLogs =
            [
                new OrderAuditLog
                {
                    Id = Guid.NewGuid(),
                    Action = OrderAuditAction.Submit,
                    AuditUserNameSnapshot = "审核员",
                    AuditTime = DateTime.UtcNow
                }
            ]
        };

        var result = mapper.Map<SaleOrderDto>(order);

        Assert.Equal("学校客户", result.CustomerName);
        Assert.Equal("SCHOOL_001", result.CustomerCode);
        Assert.Equal("中心仓", result.WareName);
        Assert.Equal("张老师", result.ContactName);
        Assert.Equal(detailId, Assert.Single(result.Details).Id);
        Assert.Equal("番茄", result.Details[0].GoodsName);
        Assert.Equal("审核员", Assert.Single(result.AuditLogs).AuditUserName);
    }

    [Fact]
    public void CreateSaleOrder_mapping_preserves_editable_fields_and_details()
    {
        var goodsId = Guid.NewGuid();
        var input = new CreateSaleOrderDto
        {
            CustomerId = Guid.NewGuid(),
            OrderDate = new DateTime(2026, 7, 2, 8, 0, 0, DateTimeKind.Utc),
            ReceiveDate = new DateTime(2026, 7, 3, 8, 0, 0, DateTimeKind.Utc),
            ContactName = "李老师",
            DeliveryAddress = "第二食堂",
            Details =
            [
                new CreateSaleOrderDetailDto
                {
                    GoodsId = goodsId,
                    GoodsUnitId = Guid.NewGuid(),
                    Quantity = 5m,
                    FixedPrice = 12m,
                    FixedGoodsUnitId = Guid.NewGuid()
                }
            ]
        };

        var result = mapper.Map<SaleOrder>(input);

        Assert.Equal(input.CustomerId, result.CustomerId);
        Assert.Equal("李老师", result.ContactNameSnapshot);
        Assert.Equal("第二食堂", result.DeliveryAddressSnapshot);
        var detail = Assert.Single(result.Details);
        Assert.Equal(goodsId, detail.GoodsId);
        Assert.Equal(5m, detail.Quantity);
        Assert.Equal(12m, detail.FixedPrice);
    }

    [Fact]
    public void UpdateSaleOrder_mapping_preserves_order_and_detail_ids()
    {
        var orderId = Guid.NewGuid();
        var detailId = Guid.NewGuid();
        var input = new UpdateSaleOrderDto
        {
            Id = orderId,
            CustomerId = Guid.NewGuid(),
            OrderDate = DateTime.UtcNow,
            Details =
            [
                new UpdateSaleOrderDetailDto
                {
                    Id = detailId,
                    GoodsId = Guid.NewGuid(),
                    GoodsUnitId = Guid.NewGuid(),
                    FixedGoodsUnitId = Guid.NewGuid(),
                    Quantity = 1m
                }
            ]
        };

        var result = mapper.Map<SaleOrder>(input);

        Assert.Equal(orderId, result.Id);
        Assert.Equal(detailId, Assert.Single(result.Details).Id);
    }
}
