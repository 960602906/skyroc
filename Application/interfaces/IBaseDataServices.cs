using Application.DTOs;
using Application.DTOs.Customers;
using Application.DTOs.Goods;
using Application.DTOs.Pricing;
using Application.DTOs.Purchases;
using Application.DTOs.Storage;
using Application.QueryParameters;
using Shared.Constants;

namespace Application.interfaces;

/// <summary>
///     基础资料通用应用服务接口。
/// </summary>
public interface IBaseDataService<TDto, in TCreateDto, in TUpdateDto, in TQuery>
    where TUpdateDto : IHasId
{
    /// <summary>
    ///     分页查询。
    /// </summary>
    Task<PagedResult<TDto>> GetPagedAsync(TQuery parameters);

    /// <summary>
    ///     查询全部。
    /// </summary>
    Task<List<TDto>> GetAllAsync();

    /// <summary>
    ///     根据 ID 查询。
    /// </summary>
    Task<TDto> GetByIdAsync(Guid id);

    /// <summary>
    ///     创建。
    /// </summary>
    Task<TDto> CreateAsync(TCreateDto dto);

    /// <summary>
    ///     更新。
    /// </summary>
    Task<TDto> UpdateAsync(Guid id, TUpdateDto dto);

    /// <summary>
    ///     删除。
    /// </summary>
    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    ///     批量删除。
    /// </summary>
    Task<bool> BatchDeleteAsync(List<Guid> ids);

    /// <summary>
    ///     启用或禁用。
    /// </summary>
    Task<TDto> ToggleStatusAsync(Guid id, Status status);
}

/// <summary>
///     树形基础资料通用应用服务接口。
/// </summary>
public interface ITreeBaseDataService<TDto, in TCreateDto, in TUpdateDto, in TQuery>
    : IBaseDataService<TDto, TCreateDto, TUpdateDto, TQuery>
    where TUpdateDto : IHasId
{
    /// <summary>
    ///     获取树形结构。
    /// </summary>
    Task<List<TDto>> GetTreeAsync();
}

public interface IGoodsTypeService : ITreeBaseDataService<GoodsTypeDto, CreateGoodsTypeDto, UpdateGoodsTypeDto, GoodsTypeQueryParameters>;

public interface IGoodsService : IBaseDataService<GoodsDto, CreateGoodsDto, UpdateGoodsDto, GoodsQueryParameters>
{
    /// <summary>
    ///     修改商品上下架状态。
    /// </summary>
    Task<GoodsDto> ToggleSaleStatusAsync(Guid id, bool isOnSale);
}

public interface IGoodsUnitService : IBaseDataService<GoodsUnitDto, CreateGoodsUnitDto, UpdateGoodsUnitDto, GoodsUnitQueryParameters>
{
    /// <summary>
    ///     查询商品单位列表。
    /// </summary>
    Task<List<GoodsUnitDto>> GetByGoodsIdAsync(Guid goodsId);
}

public interface ICompanyService : IBaseDataService<CompanyDto, CreateCompanyDto, UpdateCompanyDto, CompanyQueryParameters>;

public interface ICustomerService : IBaseDataService<CustomerDto, CreateCustomerDto, UpdateCustomerDto, CustomerQueryParameters>;

public interface ICustomerTagService : ITreeBaseDataService<CustomerTagDto, CreateCustomerTagDto, UpdateCustomerTagDto, CustomerTagQueryParameters>;

public interface ICustomerSubAccountService : IBaseDataService<CustomerSubAccountDto, CreateCustomerSubAccountDto, UpdateCustomerSubAccountDto, CustomerSubAccountQueryParameters>;

public interface ISupplierService : IBaseDataService<SupplierDto, CreateSupplierDto, UpdateSupplierDto, SupplierQueryParameters>;

public interface IPurchaserService : IBaseDataService<PurchaserDto, CreatePurchaserDto, UpdatePurchaserDto, PurchaserQueryParameters>;

public interface IWareService : IBaseDataService<WareDto, CreateWareDto, UpdateWareDto, WareQueryParameters>;

public interface IQuotationService : IBaseDataService<QuotationDto, CreateQuotationDto, UpdateQuotationDto, QuotationQueryParameters>
{
    /// <summary>
    ///     审核或反审核报价单。
    /// </summary>
    Task<QuotationDto> ToggleAuditAsync(Guid id, bool isAudited);
}

public interface IQuotationGoodsService : IBaseDataService<QuotationGoodsDto, CreateQuotationGoodsDto, UpdateQuotationGoodsDto, QuotationGoodsQueryParameters>;

public interface ICustomerProtocolService : IBaseDataService<CustomerProtocolDto, CreateCustomerProtocolDto, UpdateCustomerProtocolDto, CustomerProtocolQueryParameters>;

public interface ICustomerProtocolGoodsService : IBaseDataService<CustomerProtocolGoodsDto, CreateCustomerProtocolGoodsDto, UpdateCustomerProtocolGoodsDto, CustomerProtocolGoodsQueryParameters>;

public interface IPurchaseRuleService : IBaseDataService<PurchaseRuleDto, CreatePurchaseRuleDto, UpdatePurchaseRuleDto, PurchaseRuleQueryParameters>;
