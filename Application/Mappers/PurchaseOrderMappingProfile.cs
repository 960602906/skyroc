using Application.DTOs.Purchases;
using AutoMapper;
using Domain.Entities.Purchases;

namespace Application.Mappers;

/// <summary>
/// 采购单实体、明细及计划来源的应用模型映射配置。
/// </summary>
public class PurchaseOrderMappingProfile : Profile
{
    /// <summary>
    /// 配置采购单聚合到输出 DTO 的快照字段映射。
    /// </summary>
    public PurchaseOrderMappingProfile()
    {
        CreateMap<PurchaseOrder, PurchaseOrderDto>()
            .ForMember(x => x.SupplierName, opt => opt.MapFrom(src => src.SupplierNameSnapshot))
            .ForMember(x => x.PurchaserName, opt => opt.MapFrom(src => src.PurchaserNameSnapshot))
            .ForMember(x => x.SupplierContactName, opt => opt.MapFrom(src => src.SupplierContactNameSnapshot))
            .ForMember(x => x.SupplierContactPhone, opt => opt.MapFrom(src => src.SupplierContactPhoneSnapshot));

        CreateMap<PurchaseOrderDetail, PurchaseOrderDetailDto>()
            .ForMember(x => x.GoodsName, opt => opt.MapFrom(src => src.GoodsNameSnapshot))
            .ForMember(x => x.GoodsCode, opt => opt.MapFrom(src => src.GoodsCodeSnapshot))
            .ForMember(x => x.GoodsInfo, opt => opt.MapFrom(src => src.GoodsInfoSnapshot))
            .ForMember(x => x.PurchaseUnitName, opt => opt.MapFrom(src => src.PurchaseUnitNameSnapshot));

        CreateMap<PurchaseOrderPlanRelation, PurchaseOrderPlanRelationDto>()
            .ForMember(x => x.PurchasePlanId,
                opt => opt.MapFrom(src => src.PurchasePlanDetail.PurchasePlanId))
            .ForMember(x => x.PurchasePlanNo,
                opt => opt.MapFrom(src => src.PurchasePlanDetail.PurchasePlan.PlanNo));
    }
}
