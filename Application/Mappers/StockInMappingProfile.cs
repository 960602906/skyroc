using Application.DTOs.Storage;
using AutoMapper;
using Domain.Entities.Storage;

namespace Application.Mappers;

/// <summary>
/// 入库单实体及商品明细到输出 DTO 的映射配置。
/// </summary>
public class StockInMappingProfile : Profile
{
    /// <summary>
    /// 配置入库单聚合到输出 DTO 的快照字段映射。
    /// </summary>
    public StockInMappingProfile()
    {
        CreateMap<StockInOrder, StockInOrderDto>()
            .ForMember(x => x.WareName, opt => opt.MapFrom(src => src.WareNameSnapshot))
            .ForMember(x => x.SupplierName, opt => opt.MapFrom(src => src.SupplierNameSnapshot))
            .ForMember(x => x.CustomerName, opt => opt.MapFrom(src => src.CustomerNameSnapshot))
            .ForMember(x => x.DepartmentName, opt => opt.MapFrom(src => src.DepartmentNameSnapshot))
            .ForMember(x => x.PurchaserName, opt => opt.MapFrom(src => src.PurchaserNameSnapshot))
            .ForMember(x => x.AuditUserName, opt => opt.MapFrom(src => src.AuditUserNameSnapshot))
            .ForMember(x => x.ReverseUserName, opt => opt.MapFrom(src => src.ReverseUserNameSnapshot));

        CreateMap<StockInDetail, StockInDetailDto>()
            .ForMember(x => x.GoodsName, opt => opt.MapFrom(src => src.GoodsNameSnapshot))
            .ForMember(x => x.GoodsCode, opt => opt.MapFrom(src => src.GoodsCodeSnapshot))
            .ForMember(x => x.GoodsUnitName, opt => opt.MapFrom(src => src.GoodsUnitNameSnapshot));
    }
}
