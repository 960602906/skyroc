using Application.DTOs.Orders;
using AutoMapper;
using Domain.Entities.Orders;

namespace Application.Mappers;

/// <summary>
/// 订单应用模型映射配置。
/// </summary>
public class OrderMappingProfile : Profile
{
    public OrderMappingProfile()
    {
        CreateMap<SaleOrder, SaleOrderDto>()
            .ForMember(x => x.CustomerName, opt => opt.MapFrom(src => src.CustomerNameSnapshot))
            .ForMember(x => x.CustomerCode, opt => opt.MapFrom(src => src.CustomerCodeSnapshot))
            .ForMember(x => x.WareName, opt => opt.MapFrom(src => src.Ware == null ? null : src.Ware.Name))
            .ForMember(x => x.ContactName, opt => opt.MapFrom(src => src.ContactNameSnapshot))
            .ForMember(x => x.ContactPhone, opt => opt.MapFrom(src => src.ContactPhoneSnapshot))
            .ForMember(x => x.DeliveryAddress, opt => opt.MapFrom(src => src.DeliveryAddressSnapshot));

        CreateMap<SaleOrderDetail, SaleOrderDetailDto>()
            .ForMember(x => x.GoodsName, opt => opt.MapFrom(src => src.GoodsNameSnapshot))
            .ForMember(x => x.GoodsCode, opt => opt.MapFrom(src => src.GoodsCodeSnapshot))
            .ForMember(x => x.GoodsImage, opt => opt.MapFrom(src => src.GoodsImageSnapshot))
            .ForMember(x => x.GoodsTypeName, opt => opt.MapFrom(src => src.GoodsTypeNameSnapshot))
            .ForMember(x => x.GoodsDescription, opt => opt.MapFrom(src => src.GoodsDescriptionSnapshot))
            .ForMember(x => x.GoodsUnitName, opt => opt.MapFrom(src => src.GoodsUnitNameSnapshot))
            .ForMember(x => x.BaseUnitName, opt => opt.MapFrom(src => src.BaseUnitNameSnapshot))
            .ForMember(x => x.FixedGoodsUnitName, opt => opt.MapFrom(src => src.FixedGoodsUnitNameSnapshot));

        CreateMap<OrderAuditLog, OrderAuditLogDto>()
            .ForMember(x => x.AuditUserName, opt => opt.MapFrom(src => src.AuditUserNameSnapshot));

        CreateMap<CreateSaleOrderDto, SaleOrder>(MemberList.None)
            .ForMember(x => x.ContactNameSnapshot, opt => opt.MapFrom(src => src.ContactName))
            .ForMember(x => x.ContactPhoneSnapshot, opt => opt.MapFrom(src => src.ContactPhone))
            .ForMember(x => x.DeliveryAddressSnapshot, opt => opt.MapFrom(src => src.DeliveryAddress));
        CreateMap<CreateSaleOrderDetailDto, SaleOrderDetail>(MemberList.None);

        CreateMap<UpdateSaleOrderDto, SaleOrder>(MemberList.None)
            .ForMember(x => x.ContactNameSnapshot, opt => opt.MapFrom(src => src.ContactName))
            .ForMember(x => x.ContactPhoneSnapshot, opt => opt.MapFrom(src => src.ContactPhone))
            .ForMember(x => x.DeliveryAddressSnapshot, opt => opt.MapFrom(src => src.DeliveryAddress));
        CreateMap<UpdateSaleOrderDetailDto, SaleOrderDetail>(MemberList.None);
    }
}
