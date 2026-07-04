using Application.DTOs.Storage;
using AutoMapper;
using Domain.Entities.Storage;

namespace Application.Mappers;

/// <summary>
/// 出库单实体及商品批次明细到输出 DTO 的映射配置。
/// </summary>
public class StockOutMappingProfile : Profile
{
    /// <summary>
    /// 配置出库单聚合到输出 DTO 的历史快照字段映射。
    /// </summary>
    public StockOutMappingProfile()
    {
        CreateMap<StockOutOrder, StockOutOrderDto>()
            .ForMember(x => x.WareName, opt => opt.MapFrom(src => src.WareNameSnapshot))
            .ForMember(x => x.CustomerName, opt => opt.MapFrom(src => src.CustomerNameSnapshot))
            .ForMember(x => x.SupplierName, opt => opt.MapFrom(src => src.SupplierNameSnapshot))
            .ForMember(x => x.DepartmentName, opt => opt.MapFrom(src => src.DepartmentNameSnapshot))
            .ForMember(x => x.AuditUserName, opt => opt.MapFrom(src => src.AuditUserNameSnapshot))
            .ForMember(x => x.ReverseUserName, opt => opt.MapFrom(src => src.ReverseUserNameSnapshot));

        CreateMap<StockOutDetail, StockOutDetailDto>()
            .ForMember(x => x.GoodsName, opt => opt.MapFrom(src => src.GoodsNameSnapshot))
            .ForMember(x => x.GoodsCode, opt => opt.MapFrom(src => src.GoodsCodeSnapshot))
            .ForMember(x => x.GoodsUnitName, opt => opt.MapFrom(src => src.GoodsUnitNameSnapshot))
            .ForMember(x => x.BatchNo, opt => opt.MapFrom(src => src.BatchNoSnapshot));
    }
}
