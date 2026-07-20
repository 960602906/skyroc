using Application.DTOs.AfterSales;
using AutoMapper;
using Domain.Entities.AfterSales;
using Domain.ReadModels.AfterSales;

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
        CreateMap<AfterSaleListItemReadModel, AfterSaleListItemDto>();
        CreateMap<AfterSaleListGoodsReadModel, AfterSaleListGoodsDto>();

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
                    .ThenBy(log => log.Id)))
            .ForMember(
                x => x.PickupTasks,
                opt => opt.MapFrom(src => src.PickupTasks
                    .OrderBy(task => task.TaskNo)
                    .ThenBy(task => task.Id)));

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

        CreateMap<PickupTask, PickupTaskDto>()
            .ForMember(x => x.AfterSaleNo, opt => opt.MapFrom(src => src.AfterSale.AfterSaleNo))
            .ForMember(x => x.CustomerId, opt => opt.MapFrom(src => src.AfterSale.CustomerId))
            .ForMember(x => x.CustomerName, opt => opt.MapFrom(src => src.AfterSale.CustomerNameSnapshot))
            .ForMember(x => x.GoodsId, opt => opt.MapFrom(src => src.AfterSaleGoods.GoodsId))
            .ForMember(x => x.GoodsName, opt => opt.MapFrom(src => src.AfterSaleGoods.GoodsNameSnapshot))
            .ForMember(x => x.Quantity, opt => opt.MapFrom(src => src.AfterSaleGoods.ActualRefundQuantity))
            .ForMember(x => x.GoodsUnitName, opt => opt.MapFrom(src => src.AfterSaleGoods.GoodsUnitNameSnapshot))
            .ForMember(x => x.DriverName, opt => opt.MapFrom(src => src.DriverNameSnapshot))
            .ForMember(x => x.DriverPhone, opt => opt.MapFrom(src => src.DriverPhoneSnapshot))
            .ForMember(x => x.ContactName, opt => opt.MapFrom(src => src.ContactNameSnapshot))
            .ForMember(x => x.ContactPhone, opt => opt.MapFrom(src => src.ContactPhoneSnapshot))
            .ForMember(x => x.PickupAddress, opt => opt.MapFrom(src => src.PickupAddressSnapshot))
            .ForMember(x => x.StockInOrderId, opt => opt.MapFrom(src => src.StockInDetail == null
                ? (Guid?)null
                : src.StockInDetail.StockInOrderId));
    }
}
