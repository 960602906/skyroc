using Application.DTOs.Storage;
using AutoMapper;
using Domain.Entities.Storage;

namespace Application.Mappers;

/// <summary>
/// 库存盘点主单和批次差异明细到输出 DTO 的快照字段映射配置。
/// </summary>
public class StocktakingMappingProfile : Profile
{
    /// <summary>
    /// 配置盘点聚合的仓库、审核人、商品、批次和基础单位快照映射。
    /// </summary>
    public StocktakingMappingProfile()
    {
        CreateMap<StocktakingOrder, StocktakingOrderDto>()
            .ForMember(x => x.WareName, opt => opt.MapFrom(src => src.WareNameSnapshot))
            .ForMember(x => x.AuditUserName, opt => opt.MapFrom(src => src.AuditUserNameSnapshot));

        CreateMap<StocktakingDetail, StocktakingDetailDto>()
            .ForMember(x => x.GoodsName, opt => opt.MapFrom(src => src.GoodsNameSnapshot))
            .ForMember(x => x.GoodsCode, opt => opt.MapFrom(src => src.GoodsCodeSnapshot))
            .ForMember(x => x.BatchNo, opt => opt.MapFrom(src => src.BatchNoSnapshot))
            .ForMember(x => x.BaseUnitName, opt => opt.MapFrom(src => src.BaseUnitNameSnapshot));
    }
}
