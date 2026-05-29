using Application.DTOs.Customers;
using Application.DTOs.Goods;
using Application.DTOs.Pricing;
using Application.DTOs.Purchases;
using Application.DTOs.Storage;
using AutoMapper;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace Application.Mappers;

/// <summary>
///     基础资料映射配置。
/// </summary>
public class BaseDataMappingProfile : Profile
{
    public BaseDataMappingProfile()
    {
        CreateMap<GoodsType, GoodsTypeDto>()
            .ForMember(x => x.Children, opt => opt.Ignore());
        CreateMap<CreateGoodsTypeDto, GoodsType>();
        CreateMap<UpdateGoodsTypeDto, GoodsType>();

        CreateMap<GoodsEntity, GoodsDto>()
            .ForMember(x => x.GoodsTypeName, opt => opt.MapFrom(src => src.GoodsType == null ? null : src.GoodsType.Name))
            .ForMember(x => x.BaseUnitName, opt => opt.MapFrom(src => src.BaseUnit == null ? null : src.BaseUnit.Name))
            .ForMember(x => x.DefaultSupplierName, opt => opt.MapFrom(src => src.DefaultSupplier == null ? null : src.DefaultSupplier.Name))
            .ForMember(x => x.DefaultWareName, opt => opt.MapFrom(src => src.DefaultWare == null ? null : src.DefaultWare.Name))
            .ForMember(x => x.SupplierIds, opt => opt.MapFrom(src => src.SupplierRelations.Select(x => x.SupplierId).ToList()));
        CreateMap<CreateGoodsDto, GoodsEntity>();
        CreateMap<UpdateGoodsDto, GoodsEntity>();

        CreateMap<GoodsUnit, GoodsUnitDto>();
        CreateMap<CreateGoodsUnitDto, GoodsUnit>();
        CreateMap<UpdateGoodsUnitDto, GoodsUnit>();
        CreateMap<GoodsImage, GoodsImageDto>();

        CreateMap<Company, CompanyDto>();
        CreateMap<CreateCompanyDto, Company>();
        CreateMap<UpdateCompanyDto, Company>();

        CreateMap<Customer, CustomerDto>()
            .ForMember(x => x.CompanyName, opt => opt.MapFrom(src => src.Company == null ? null : src.Company.Name))
            .ForMember(x => x.QuotationName, opt => opt.MapFrom(src => src.Quotation == null ? null : src.Quotation.Name))
            .ForMember(x => x.DefaultWareName, opt => opt.MapFrom(src => src.DefaultWare == null ? null : src.DefaultWare.Name))
            .ForMember(x => x.TagIds, opt => opt.MapFrom(src => src.TagRelations.Select(x => x.CustomerTagId).ToList()));
        CreateMap<CreateCustomerDto, Customer>();
        CreateMap<UpdateCustomerDto, Customer>();

        CreateMap<CustomerTag, CustomerTagDto>()
            .ForMember(x => x.Children, opt => opt.Ignore());
        CreateMap<CreateCustomerTagDto, CustomerTag>();
        CreateMap<UpdateCustomerTagDto, CustomerTag>();

        CreateMap<CustomerSubAccount, CustomerSubAccountDto>()
            .ForMember(x => x.CompanyName, opt => opt.MapFrom(src => src.Company == null ? null : src.Company.Name))
            .ForMember(x => x.CustomerName, opt => opt.MapFrom(src => src.Customer == null ? null : src.Customer.Name));
        CreateMap<CreateCustomerSubAccountDto, CustomerSubAccount>();
        CreateMap<UpdateCustomerSubAccountDto, CustomerSubAccount>();

        CreateMap<Supplier, SupplierDto>();
        CreateMap<CreateSupplierDto, Supplier>();
        CreateMap<UpdateSupplierDto, Supplier>();

        CreateMap<Purchaser, PurchaserDto>()
            .ForMember(x => x.UserName, opt => opt.MapFrom(src => src.User == null ? null : src.User.Username))
            .ForMember(x => x.DepartmentName, opt => opt.MapFrom(src => src.Department == null ? null : src.Department.Name));
        CreateMap<CreatePurchaserDto, Purchaser>();
        CreateMap<UpdatePurchaserDto, Purchaser>();

        CreateMap<Ware, WareDto>();
        CreateMap<CreateWareDto, Ware>();
        CreateMap<UpdateWareDto, Ware>();

        CreateMap<Quotation, QuotationDto>()
            .ForMember(x => x.CustomerIds, opt => opt.MapFrom(src => src.CustomerQuotations.Select(x => x.CustomerId).ToList()));
        CreateMap<CreateQuotationDto, Quotation>();
        CreateMap<UpdateQuotationDto, Quotation>();

        CreateMap<QuotationGoods, QuotationGoodsDto>()
            .ForMember(x => x.GoodsName, opt => opt.MapFrom(src => src.Goods == null ? null : src.Goods.Name))
            .ForMember(x => x.GoodsCode, opt => opt.MapFrom(src => src.Goods == null ? null : src.Goods.Code))
            .ForMember(x => x.GoodsUnitName, opt => opt.MapFrom(src => src.GoodsUnit == null ? null : src.GoodsUnit.Name));
        CreateMap<CreateQuotationGoodsDto, QuotationGoods>();
        CreateMap<UpdateQuotationGoodsDto, QuotationGoods>();

        CreateMap<CustomerProtocol, CustomerProtocolDto>()
            .ForMember(x => x.QuotationName, opt => opt.MapFrom(src => src.Quotation == null ? null : src.Quotation.Name))
            .ForMember(x => x.CustomerIds, opt => opt.MapFrom(src => src.Customers.Select(x => x.CustomerId).ToList()));
        CreateMap<CreateCustomerProtocolDto, CustomerProtocol>();
        CreateMap<UpdateCustomerProtocolDto, CustomerProtocol>();

        CreateMap<CustomerProtocolGoods, CustomerProtocolGoodsDto>()
            .ForMember(x => x.GoodsName, opt => opt.MapFrom(src => src.Goods == null ? null : src.Goods.Name))
            .ForMember(x => x.GoodsCode, opt => opt.MapFrom(src => src.Goods == null ? null : src.Goods.Code))
            .ForMember(x => x.GoodsUnitName, opt => opt.MapFrom(src => src.GoodsUnit == null ? null : src.GoodsUnit.Name));
        CreateMap<CreateCustomerProtocolGoodsDto, CustomerProtocolGoods>();
        CreateMap<UpdateCustomerProtocolGoodsDto, CustomerProtocolGoods>();

        CreateMap<PurchaseRule, PurchaseRuleDto>()
            .ForMember(x => x.SupplierName, opt => opt.MapFrom(src => src.Supplier == null ? null : src.Supplier.Name))
            .ForMember(x => x.PurchaserName, opt => opt.MapFrom(src => src.Purchaser == null ? null : src.Purchaser.Name))
            .ForMember(x => x.WareName, opt => opt.MapFrom(src => src.Ware == null ? null : src.Ware.Name))
            .ForMember(x => x.GoodsTypeName, opt => opt.MapFrom(src => src.GoodsType == null ? null : src.GoodsType.Name))
            .ForMember(x => x.GoodsIds, opt => opt.MapFrom(src => src.Goods.Select(x => x.GoodsId).ToList()))
            .ForMember(x => x.CustomerIds, opt => opt.MapFrom(src => src.Customers.Select(x => x.CustomerId).ToList()));
        CreateMap<CreatePurchaseRuleDto, PurchaseRule>();
        CreateMap<UpdatePurchaseRuleDto, PurchaseRule>();
    }
}
