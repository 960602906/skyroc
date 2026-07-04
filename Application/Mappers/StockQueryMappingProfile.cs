using Application.DTOs.Storage;
using AutoMapper;
using Domain.Entities.Storage;
using Domain.ReadModels.Storage;
using Shared.Constants;

namespace Application.Mappers;

/// <summary>
/// 库存总览、批次和台账查询结果到输出 DTO 的映射配置。
/// </summary>
public class StockQueryMappingProfile : Profile
{
    /// <summary>
    /// 配置库存查询的当前资料、历史快照、原始数量和展示精度映射。
    /// </summary>
    public StockQueryMappingProfile()
    {
        CreateMap<StockOverviewReadModel, StockOverviewDto>()
            .ForMember(x => x.CurrentQuantity,
                opt => opt.MapFrom(src => NumericPrecision.RoundQuantity(src.CurrentQuantity)))
            .ForMember(x => x.AvailableQuantity,
                opt => opt.MapFrom(src => NumericPrecision.RoundQuantity(src.AvailableQuantity)))
            .ForMember(x => x.StockValue, opt => opt.MapFrom(src => NumericPrecision.RoundMoney(src.StockValue)));

        CreateMap<StockBatch, StockBatchDto>()
            .ForMember(x => x.WareName, opt => opt.MapFrom(src => src.Ware.Name))
            .ForMember(x => x.GoodsTypeId, opt => opt.MapFrom(src => src.Goods.GoodsTypeId))
            .ForMember(x => x.GoodsTypeName, opt => opt.MapFrom(src => src.Goods.GoodsType.Name))
            .ForMember(x => x.GoodsName, opt => opt.MapFrom(src => src.Goods.Name))
            .ForMember(x => x.GoodsCode, opt => opt.MapFrom(src => src.Goods.Code))
            .ForMember(x => x.BaseUnitName, opt => opt.MapFrom(src => src.BaseUnit.Name))
            .ForMember(x => x.CurrentQuantity,
                opt => opt.MapFrom(src => NumericPrecision.RoundQuantity(src.CurrentQuantity)))
            .ForMember(x => x.AvailableQuantity,
                opt => opt.MapFrom(src => NumericPrecision.RoundQuantity(src.AvailableQuantity)))
            .ForMember(x => x.UnitCost, opt => opt.MapFrom(src => NumericPrecision.RoundMoney(src.UnitCost)));

        CreateMap<StockLedger, StockLedgerDto>()
            .ForMember(x => x.WareName, opt => opt.MapFrom(src => src.WareNameSnapshot))
            .ForMember(x => x.GoodsName, opt => opt.MapFrom(src => src.GoodsNameSnapshot))
            .ForMember(x => x.GoodsCode, opt => opt.MapFrom(src => src.GoodsCodeSnapshot))
            .ForMember(x => x.BatchNo, opt => opt.MapFrom(src => src.BatchNoSnapshot))
            .ForMember(x => x.BaseUnitName, opt => opt.MapFrom(src => src.BaseUnitNameSnapshot))
            .ForMember(x => x.ChangeQuantity,
                opt => opt.MapFrom(src => NumericPrecision.RoundQuantity(src.ChangeQuantity)))
            .ForMember(x => x.BalanceQuantity,
                opt => opt.MapFrom(src => NumericPrecision.RoundQuantity(src.BalanceQuantity)))
            .ForMember(x => x.UnitCost, opt => opt.MapFrom(src => NumericPrecision.RoundMoney(src.UnitCost)))
            .ForMember(x => x.TotalCost, opt => opt.MapFrom(src => NumericPrecision.RoundMoney(src.TotalCost)));
    }
}
