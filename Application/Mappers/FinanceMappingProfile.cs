using Application.DTOs.Finance;
using AutoMapper;
using Domain.Entities.Finance;

namespace Application.Mappers;

/// <summary>
/// 财务账单与结款凭证响应模型映射配置。
/// </summary>
public class FinanceMappingProfile : Profile
{
    /// <summary>
    /// 配置客户账单、结款凭证和凭证明细到响应 DTO 的结构化映射与稳定排序。
    /// </summary>
    public FinanceMappingProfile()
    {
        CreateMap<CustomerBill, CustomerBillDto>()
            .ForMember(x => x.CustomerName, opt => opt.MapFrom(src => src.CustomerNameSnapshot))
            .ForMember(x => x.SaleOrderNo, opt => opt.MapFrom(src => src.SaleOrderNoSnapshot));

        CreateMap<CustomerSettlement, CustomerSettlementDto>()
            .ForMember(x => x.CustomerName, opt => opt.MapFrom(src => src.CustomerNameSnapshot))
            .ForMember(x => x.VoidedByName, opt => opt.MapFrom(src => src.VoidedByNameSnapshot))
            .ForMember(
                x => x.Details,
                opt => opt.MapFrom(src => src.Details
                    .OrderBy(detail => detail.CustomerBillNoSnapshot)
                    .ThenBy(detail => detail.Id)));

        CreateMap<CustomerSettlementDetail, CustomerSettlementDetailDto>()
            .ForMember(x => x.CustomerBillNo, opt => opt.MapFrom(src => src.CustomerBillNoSnapshot))
            .ForMember(x => x.SaleOrderNo, opt => opt.MapFrom(src => src.SaleOrderNoSnapshot))
            .ForMember(x => x.ReceivableAmount, opt => opt.MapFrom(src => src.ReceivableAmountSnapshot));

        CreateMap<SupplierBill, SupplierBillDto>()
            .ForMember(x => x.SupplierName, opt => opt.MapFrom(src => src.SupplierNameSnapshot))
            .ForMember(x => x.SourceDocumentNo, opt => opt.MapFrom(src => src.SourceDocumentNoSnapshot));

        CreateMap<SupplierSettlement, SupplierSettlementDto>()
            .ForMember(x => x.SupplierName, opt => opt.MapFrom(src => src.SupplierNameSnapshot))
            .ForMember(x => x.VoidedByName, opt => opt.MapFrom(src => src.VoidedByNameSnapshot))
            .ForMember(
                x => x.Details,
                opt => opt.MapFrom(src => src.Details
                    .OrderBy(detail => detail.SupplierBillNoSnapshot)
                    .ThenBy(detail => detail.Id)));

        CreateMap<SupplierSettlementDetail, SupplierSettlementDetailDto>()
            .ForMember(x => x.SupplierBillNo, opt => opt.MapFrom(src => src.SupplierBillNoSnapshot))
            .ForMember(x => x.SourceDocumentNo, opt => opt.MapFrom(src => src.SourceDocumentNoSnapshot))
            .ForMember(x => x.PayableAmount, opt => opt.MapFrom(src => src.PayableAmountSnapshot));
    }
}
