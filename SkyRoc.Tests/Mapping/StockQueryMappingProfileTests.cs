using Application.DTOs.Storage;
using Application.Mappers;
using AutoMapper;
using Domain.Entities.Goods;
using Domain.Entities.Storage;
using Domain.ReadModels.Storage;
using Xunit;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace SkyRoc.Tests.Mapping;

public class StockQueryMappingProfileTests
{
    private readonly IMapper mapper;

    public StockQueryMappingProfileTests()
    {
        var configuration = new MapperConfiguration(config => config.AddProfile<StockQueryMappingProfile>());
        configuration.AssertConfigurationIsValid();
        mapper = configuration.CreateMapper();
    }

    [Fact]
    public void StockOverviewAndBatch_mapping_calculates_quantities_costs_and_current_names()
    {
        var overview = mapper.Map<StockOverviewDto>(new StockOverviewReadModel
        {
            CurrentQuantity = 15m,
            AvailableQuantity = 12m,
            StockValue = 80m
        });
        Assert.Equal(3m, overview.OccupiedQuantity);
        Assert.Equal(5.3333m, overview.WeightedUnitCost);
        Assert.Equal(80m, overview.StockValue);
        Assert.Null(typeof(StockOverviewDto).GetProperty(nameof(StockOverviewDto.OccupiedQuantity))!.SetMethod);
        Assert.Null(typeof(StockOverviewDto).GetProperty(nameof(StockOverviewDto.WeightedUnitCost))!.SetMethod);

        var batch = mapper.Map<StockBatchDto>(new StockBatchReadModel
        {
            CurrentQuantity = 10m,
            AvailableQuantity = 7m,
            UnitCost = 4m,
            WareName = "中心仓",
            GoodsTypeName = "蔬菜",
            GoodsName = "番茄",
            GoodsCode = "TOMATO",
            BaseUnitName = "千克"
        });
        Assert.Equal("中心仓", batch.WareName);
        Assert.Equal("蔬菜", batch.GoodsTypeName);
        Assert.Equal("番茄", batch.GoodsName);
        Assert.Equal("千克", batch.BaseUnitName);
        Assert.Equal(3m, batch.OccupiedQuantity);
        Assert.Equal(40m, batch.StockValue);
        Assert.Null(typeof(StockBatchDto).GetProperty(nameof(StockBatchDto.OccupiedQuantity))!.SetMethod);
        Assert.Null(typeof(StockBatchDto).GetProperty(nameof(StockBatchDto.StockValue))!.SetMethod);
    }

    [Fact]
    public void StockLedger_mapping_uses_snapshots_and_applies_direction_to_quantity()
    {
        var result = mapper.Map<StockLedgerDto>(new StockLedger
        {
            WareNameSnapshot = "中心仓",
            GoodsNameSnapshot = "番茄",
            GoodsCodeSnapshot = "TOMATO",
            BatchNoSnapshot = "BATCH-001",
            BaseUnitNameSnapshot = "千克",
            Direction = StockLedgerDirection.Decrease,
            ChangeQuantity = 2m,
            BalanceQuantity = 8m,
            UnitCost = 4m,
            TotalCost = 8m
        });

        Assert.Equal("中心仓", result.WareName);
        Assert.Equal("番茄", result.GoodsName);
        Assert.Equal("BATCH-001", result.BatchNo);
        Assert.Equal(2m, result.ChangeQuantity);
        Assert.Equal(-2m, result.SignedChangeQuantity);
        Assert.Equal(8m, result.BalanceQuantity);
        Assert.Null(typeof(StockLedgerDto).GetProperty(nameof(StockLedgerDto.SignedChangeQuantity))!.SetMethod);
    }
}
