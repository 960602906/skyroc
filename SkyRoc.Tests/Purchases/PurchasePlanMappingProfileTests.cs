using Application.DTOs.Purchases;
using Application.Mappers;
using AutoMapper;
using Domain.Entities.Orders;
using Domain.Entities.Purchases;
using Xunit;

namespace SkyRoc.Tests.Purchases;

public class PurchasePlanMappingProfileTests
{
    private readonly IMapper mapper;

    public PurchasePlanMappingProfileTests()
    {
        var configuration = new MapperConfiguration(config => config.AddProfile<PurchasePlanMappingProfile>());
        configuration.AssertConfigurationIsValid();
        mapper = configuration.CreateMapper();
    }

    [Fact]
    public void PurchasePlan_mapping_uses_snapshots_and_nested_details()
    {
        var detailId = Guid.NewGuid();
        var plan = new PurchasePlan
        {
            Id = Guid.NewGuid(),
            PlanNo = "PP202607030001",
            PlanDate = new DateTime(2026, 7, 4, 8, 0, 0, DateTimeKind.Utc),
            PurchasePattern = PurchasePattern.MarketSelfPurchase,
            PurchaseStatus = PurchasePlanStatus.Unpublished,
            SupplierId = Guid.NewGuid(),
            SupplierNameSnapshot = "蔬菜直供商",
            PurchaserId = Guid.NewGuid(),
            PurchaserNameSnapshot = "采购员甲",
            Details =
            [
                new PurchasePlanDetail
                {
                    Id = detailId,
                    GoodsId = Guid.NewGuid(),
                    GoodsNameSnapshot = "番茄",
                    GoodsCodeSnapshot = "TOMATO",
                    PurchaseUnitId = Guid.NewGuid(),
                    PurchaseUnitNameSnapshot = "千克",
                    RequiredQuantity = 10m,
                    PlannedQuantity = 10m,
                    OrderRelations =
                    [
                        new PurchasePlanOrderRelation
                        {
                            Id = Guid.NewGuid(),
                            SaleOrderId = Guid.NewGuid(),
                            SaleOrderDetailId = Guid.NewGuid(),
                            RequiredQuantity = 10m,
                            SaleOrder = new SaleOrder { OrderNo = "SO202607030001" }
                        }
                    ]
                }
            ]
        };

        var result = mapper.Map<PurchasePlanDto>(plan);

        Assert.Equal("蔬菜直供商", result.SupplierName);
        Assert.Equal("采购员甲", result.PurchaserName);
        Assert.Equal(PurchasePattern.MarketSelfPurchase, result.PurchasePattern);
        var detail = Assert.Single(result.Details);
        Assert.Equal(detailId, detail.Id);
        Assert.Equal("番茄", detail.GoodsName);
        Assert.Equal("千克", detail.PurchaseUnitName);
        Assert.Equal("SO202607030001", Assert.Single(detail.OrderRelations).SaleOrderNo);
    }
}
