using Application.DTOs.Delivery;
using AutoMapper;
using Domain.Entities.Delivery;
using Domain.Entities.Orders;

namespace Application.Mappers;

/// <summary>
/// 配送任务与配送异常响应映射配置。
/// </summary>
public class DeliveryTaskMappingProfile : Profile
{
    /// <summary>
    /// 配置配送任务来源单号、业务快照和配送异常关联信息映射。
    /// </summary>
    public DeliveryTaskMappingProfile()
    {
        CreateMap<DeliveryTask, DeliveryTaskDto>()
            .ForMember(x => x.StockOutOrderNo, opt => opt.MapFrom(src => src.StockOutOrder.OutNo))
            .ForMember(x => x.SaleOrderNo, opt => opt.MapFrom(src => src.SaleOrder.OrderNo))
            .ForMember(x => x.CustomerName, opt => opt.MapFrom(src => src.CustomerNameSnapshot))
            .ForMember(x => x.ContactName, opt => opt.MapFrom(src => src.ContactNameSnapshot))
            .ForMember(x => x.ContactPhone, opt => opt.MapFrom(src => src.ContactPhoneSnapshot))
            .ForMember(x => x.DeliveryAddress, opt => opt.MapFrom(src => src.DeliveryAddressSnapshot))
            .ForMember(x => x.WareName, opt => opt.MapFrom(src => src.WareNameSnapshot))
            .ForMember(x => x.DriverName, opt => opt.MapFrom(src => src.DriverNameSnapshot))
            .ForMember(x => x.DriverPhone, opt => opt.MapFrom(src => src.DriverPhoneSnapshot))
            .ForMember(x => x.CarrierName, opt => opt.MapFrom(src => src.CarrierNameSnapshot))
            .ForMember(x => x.RouteName, opt => opt.MapFrom(src => src.RouteNameSnapshot));

        CreateMap<OrderReceipt, OrderReceiptDto>()
            .ForMember(
                x => x.CheckDetails,
                opt => opt.MapFrom(src => src.CheckDetails
                    .OrderBy(detail => detail.GoodsCodeSnapshot)
                    .ThenBy(detail => detail.StockOutDetailId)));
        CreateMap<OrderCheckDetail, OrderCheckDetailDto>()
            .ForMember(x => x.GoodsName, opt => opt.MapFrom(src => src.GoodsNameSnapshot))
            .ForMember(x => x.GoodsCode, opt => opt.MapFrom(src => src.GoodsCodeSnapshot))
            .ForMember(x => x.GoodsUnitName, opt => opt.MapFrom(src => src.GoodsUnitNameSnapshot));

        CreateMap<DeliveryException, DeliveryExceptionDto>()
            .ForMember(x => x.DeliveryTaskNo, opt => opt.MapFrom(src => src.DeliveryTask == null ? null : src.DeliveryTask.TaskNo))
            .ForMember(x => x.DriverName, opt => opt.MapFrom(src => src.Driver == null ? null : src.Driver.Name))
            .ForMember(x => x.CustomerName, opt => opt.MapFrom(src => src.Customer == null ? null : src.Customer.Name));
    }
}
