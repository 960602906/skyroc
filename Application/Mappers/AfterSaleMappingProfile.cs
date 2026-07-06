using Application.DTOs.AfterSales;
using AutoMapper;
using Domain.Entities.AfterSales;

namespace Application.Mappers;

/// <summary>
/// 售后聚合与响应模型映射配置。
/// </summary>
public class AfterSaleMappingProfile : Profile
{
    /// <summary>
    /// 配置售后主单、商品快照和审核轨迹映射，并固定集合返回顺序。
    /// </summary>
    public AfterSaleMappingProfile()
    {
        CreateMap<AfterSale, AfterSaleDto>()
            .ForMember(x => x.SaleOrderNo, opt => opt.MapFrom(src => src.SaleOrderNoSnapshot))
            .ForMember(x => x.CustomerName, opt => opt.MapFrom(src => src.CustomerNameSnapshot))
            .ForMember(x => x.ContactName, opt => opt.MapFrom(src => src.ContactNameSnapshot))
            .ForMember(x => x.ContactPhone, opt => opt.MapFrom(src => src.ContactPhoneSnapshot))
            .ForMember(x => x.PickupAddress, opt => opt.MapFrom(src => src.PickupAddressSnapshot))
            .ForMember(
                x => x.Goods,
                opt => opt.MapFrom(src => src.Goods
                    .OrderBy(item => item.GoodsCodeSnapshot)
                    .ThenBy(item => item.Id)))
            .ForMember(
                x => x.AuditLogs,
                opt => opt.MapFrom(src => src.AuditLogs
                    .OrderBy(log => log.AuditTime)
                    .ThenBy(log => log.Id)));

        CreateMap<AfterSaleGoods, AfterSaleGoodsDto>()
            .ForMember(x => x.GoodsName, opt => opt.MapFrom(src => src.GoodsNameSnapshot))
            .ForMember(x => x.GoodsCode, opt => opt.MapFrom(src => src.GoodsCodeSnapshot))
            .ForMember(x => x.GoodsTypeName, opt => opt.MapFrom(src => src.GoodsTypeNameSnapshot))
            .ForMember(x => x.GoodsUnitName, opt => opt.MapFrom(src => src.GoodsUnitNameSnapshot))
            .ForMember(x => x.BaseUnitName, opt => opt.MapFrom(src => src.BaseUnitNameSnapshot))
            .ForMember(x => x.SupplierName, opt => opt.MapFrom(src => src.SupplierNameSnapshot))
            .ForMember(x => x.DepartmentName, opt => opt.MapFrom(src => src.DepartmentNameSnapshot));

        CreateMap<AfterSaleAuditLog, AfterSaleAuditLogDto>()
            .ForMember(x => x.AuditUserName, opt => opt.MapFrom(src => src.AuditUserNameSnapshot));
    }
}
