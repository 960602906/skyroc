using Application.DTOs.Purchases;
using AutoMapper;
using Domain.Entities.Purchases;

namespace Application.Mappers;

/// <summary>
/// 采购计划应用模型映射配置。
/// </summary>
public class PurchasePlanMappingProfile : Profile
{
    /// <summary>
    /// 配置采购计划实体与 DTO 之间的映射规则。
    /// </summary>
    public PurchasePlanMappingProfile()
    {
        CreateMap<PurchasePlan, PurchasePlanDto>()
            .ForMember(x => x.SupplierName, opt => opt.MapFrom(src => src.SupplierNameSnapshot))
            .ForMember(x => x.PurchaserName, opt => opt.MapFrom(src => src.PurchaserNameSnapshot));

        CreateMap<PurchasePlanDetail, PurchasePlanDetailDto>()
            .ForMember(x => x.GoodsName, opt => opt.MapFrom(src => src.GoodsNameSnapshot))
            .ForMember(x => x.GoodsCode, opt => opt.MapFrom(src => src.GoodsCodeSnapshot))
            .ForMember(x => x.PurchaseUnitName, opt => opt.MapFrom(src => src.PurchaseUnitNameSnapshot));

        CreateMap<PurchasePlanOrderRelation, PurchasePlanOrderRelationDto>()
            .ForMember(x => x.SaleOrderNo, opt => opt.MapFrom(src => src.SaleOrder == null ? null : src.SaleOrder.OrderNo));
    }
}
