using System.Linq.Expressions;
using Application.DTOs.Pricing;
using Application.DTOs.Purchases;
using Application.Exceptions;
using Application.interfaces;
using Application.QueryParameters;
using AutoMapper;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public class QuotationService(
    IQuotationRepository repository,
    ICustomerRepository customerRepository,
    IUnitOfWork unitOfWork,
    ILogger<QuotationService> logger,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IValidator<CreateQuotationDto> createValidator,
    IValidator<UpdateQuotationDto> updateValidator)
    : NamedCodeBaseDataService<Quotation, QuotationDto, CreateQuotationDto, UpdateQuotationDto, QuotationQueryParameters>(
            repository, unitOfWork, logger, mapper, currentUserService, createValidator, updateValidator),
        IQuotationService
{
    protected override string DisplayName => "报价单";

    protected override Expression<Func<Quotation, bool>> BuildPredicate(QuotationQueryParameters parameters)
    {
        return parameters.QueryBuild();
    }

    protected override async Task ValidateCreateAsync(CreateQuotationDto dto)
    {
        await base.ValidateCreateAsync(dto);
        await ValidateCustomersAsync(dto.CustomerIds);
    }

    protected override async Task ValidateUpdateAsync(Guid id, UpdateQuotationDto dto)
    {
        await base.ValidateUpdateAsync(id, dto);
        await ValidateCustomersAsync(dto.CustomerIds);
    }

    protected override async Task AfterCreateAsync(Quotation entity, CreateQuotationDto dto)
    {
        await repository.ReplaceCustomerRelationsAsync(entity.Id, dto.CustomerIds);
    }

    protected override async Task AfterUpdateAsync(Quotation entity, UpdateQuotationDto dto)
    {
        await repository.ReplaceCustomerRelationsAsync(entity.Id, dto.CustomerIds);
    }

    public async Task<QuotationDto> ToggleAuditAsync(Guid id, bool isAudited)
    {
        var entity = await repository.GetByIdAsync(id);
        if (entity is null)
        {
            throw new NotFoundException("报价单不存在");
        }

        entity.IsAudited = isAudited;
        ApplyUpdateAudit(entity);
        await repository.UpdateAsync(entity);
        await UnitOfWork.SaveChangesAsync();
        return Mapper.Map<QuotationDto>(entity);
    }

    private async Task ValidateCustomersAsync(IEnumerable<Guid>? customerIds)
    {
        var idList = customerIds?.Where(x => x != Guid.Empty).Distinct().ToList() ?? [];
        if (idList.Count == 0)
        {
            return;
        }

        var customers = await customerRepository.GetByIdsAsync(idList);
        if (customers.Count != idList.Count)
        {
            throw new BusinessException("部分绑定客户不存在");
        }
    }
}

public class QuotationGoodsService(
    IQuotationGoodsRepository repository,
    IQuotationRepository quotationRepository,
    IGoodsRepository goodsRepository,
    IGoodsUnitRepository goodsUnitRepository,
    IUnitOfWork unitOfWork,
    ILogger<QuotationGoodsService> logger,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IValidator<CreateQuotationGoodsDto> createValidator,
    IValidator<UpdateQuotationGoodsDto> updateValidator)
    : BaseDataService<QuotationGoods, QuotationGoodsDto, CreateQuotationGoodsDto, UpdateQuotationGoodsDto, QuotationGoodsQueryParameters>(
            repository, unitOfWork, logger, mapper, currentUserService, createValidator, updateValidator),
        IQuotationGoodsService
{
    protected override string DisplayName => "报价商品";

    protected override Expression<Func<QuotationGoods, bool>> BuildPredicate(QuotationGoodsQueryParameters parameters)
    {
        return parameters.QueryBuild();
    }

    protected override async Task ValidateCreateAsync(CreateQuotationGoodsDto dto)
    {
        await ValidateReferencesAsync(dto.QuotationId, dto.GoodsId, dto.GoodsUnitId);
        if (await repository.ExistsDetailAsync(dto.QuotationId, dto.GoodsId, dto.GoodsUnitId))
        {
            throw new BusinessException("报价商品明细已经存在");
        }
    }

    protected override async Task ValidateUpdateAsync(Guid id, UpdateQuotationGoodsDto dto)
    {
        await ValidateReferencesAsync(dto.QuotationId, dto.GoodsId, dto.GoodsUnitId);
        if (await repository.ExistsDetailAsync(dto.QuotationId, dto.GoodsId, dto.GoodsUnitId, id))
        {
            throw new BusinessException("报价商品明细已经存在");
        }
    }

    private async Task ValidateReferencesAsync(Guid quotationId, Guid goodsId, Guid goodsUnitId)
    {
        if (!await quotationRepository.ExistsAsync(quotationId))
        {
            throw new BusinessException("报价单不存在");
        }

        if (!await goodsRepository.ExistsAsync(goodsId))
        {
            throw new BusinessException("商品不存在");
        }

        var unit = await goodsUnitRepository.GetByIdAsync(goodsUnitId);
        if (unit is null || unit.GoodsId != goodsId)
        {
            throw new BusinessException("报价单位不存在或不属于该商品");
        }
    }
}

public class CustomerProtocolService(
    ICustomerProtocolRepository repository,
    IQuotationRepository quotationRepository,
    ICustomerRepository customerRepository,
    IUnitOfWork unitOfWork,
    ILogger<CustomerProtocolService> logger,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IValidator<CreateCustomerProtocolDto> createValidator,
    IValidator<UpdateCustomerProtocolDto> updateValidator)
    : NamedCodeBaseDataService<CustomerProtocol, CustomerProtocolDto, CreateCustomerProtocolDto, UpdateCustomerProtocolDto, CustomerProtocolQueryParameters>(
            repository, unitOfWork, logger, mapper, currentUserService, createValidator, updateValidator),
        ICustomerProtocolService
{
    protected override string DisplayName => "客户协议价";

    protected override Expression<Func<CustomerProtocol, bool>> BuildPredicate(CustomerProtocolQueryParameters parameters)
    {
        return parameters.QueryBuild();
    }

    protected override async Task ValidateCreateAsync(CreateCustomerProtocolDto dto)
    {
        await base.ValidateCreateAsync(dto);
        await ValidateReferencesAsync(dto.QuotationId, dto.CustomerIds);
    }

    protected override async Task ValidateUpdateAsync(Guid id, UpdateCustomerProtocolDto dto)
    {
        await base.ValidateUpdateAsync(id, dto);
        await ValidateReferencesAsync(dto.QuotationId, dto.CustomerIds);
    }

    protected override async Task AfterCreateAsync(CustomerProtocol entity, CreateCustomerProtocolDto dto)
    {
        await repository.ReplaceCustomerRelationsAsync(entity.Id, dto.CustomerIds);
    }

    protected override async Task AfterUpdateAsync(CustomerProtocol entity, UpdateCustomerProtocolDto dto)
    {
        await repository.ReplaceCustomerRelationsAsync(entity.Id, dto.CustomerIds);
    }

    private async Task ValidateReferencesAsync(Guid? quotationId, IEnumerable<Guid>? customerIds)
    {
        if (quotationId.HasValue && !await quotationRepository.ExistsAsync(quotationId.Value))
        {
            throw new BusinessException("报价单不存在");
        }

        var idList = customerIds?.Where(x => x != Guid.Empty).Distinct().ToList() ?? [];
        if (idList.Count == 0)
        {
            return;
        }

        var customers = await customerRepository.GetByIdsAsync(idList);
        if (customers.Count != idList.Count)
        {
            throw new BusinessException("部分绑定客户不存在");
        }
    }
}

public class CustomerProtocolGoodsService(
    ICustomerProtocolGoodsRepository repository,
    ICustomerProtocolRepository customerProtocolRepository,
    IGoodsRepository goodsRepository,
    IGoodsUnitRepository goodsUnitRepository,
    IUnitOfWork unitOfWork,
    ILogger<CustomerProtocolGoodsService> logger,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IValidator<CreateCustomerProtocolGoodsDto> createValidator,
    IValidator<UpdateCustomerProtocolGoodsDto> updateValidator)
    : BaseDataService<CustomerProtocolGoods, CustomerProtocolGoodsDto, CreateCustomerProtocolGoodsDto, UpdateCustomerProtocolGoodsDto, CustomerProtocolGoodsQueryParameters>(
            repository, unitOfWork, logger, mapper, currentUserService, createValidator, updateValidator),
        ICustomerProtocolGoodsService
{
    protected override string DisplayName => "客户协议价商品";

    protected override Expression<Func<CustomerProtocolGoods, bool>> BuildPredicate(CustomerProtocolGoodsQueryParameters parameters)
    {
        return parameters.QueryBuild();
    }

    protected override async Task ValidateCreateAsync(CreateCustomerProtocolGoodsDto dto)
    {
        await ValidateReferencesAsync(dto.CustomerProtocolId, dto.GoodsId, dto.GoodsUnitId);
        if (await repository.ExistsDetailAsync(dto.CustomerProtocolId, dto.GoodsId, dto.GoodsUnitId))
        {
            throw new BusinessException("客户协议价商品明细已经存在");
        }
    }

    protected override async Task ValidateUpdateAsync(Guid id, UpdateCustomerProtocolGoodsDto dto)
    {
        await ValidateReferencesAsync(dto.CustomerProtocolId, dto.GoodsId, dto.GoodsUnitId);
        if (await repository.ExistsDetailAsync(dto.CustomerProtocolId, dto.GoodsId, dto.GoodsUnitId, id))
        {
            throw new BusinessException("客户协议价商品明细已经存在");
        }
    }

    private async Task ValidateReferencesAsync(Guid customerProtocolId, Guid goodsId, Guid goodsUnitId)
    {
        if (!await customerProtocolRepository.ExistsAsync(customerProtocolId))
        {
            throw new BusinessException("客户协议价不存在");
        }

        if (!await goodsRepository.ExistsAsync(goodsId))
        {
            throw new BusinessException("商品不存在");
        }

        var unit = await goodsUnitRepository.GetByIdAsync(goodsUnitId);
        if (unit is null || unit.GoodsId != goodsId)
        {
            throw new BusinessException("协议价单位不存在或不属于该商品");
        }
    }
}

public class PurchaseRuleService(
    IPurchaseRuleRepository repository,
    ISupplierRepository supplierRepository,
    IPurchaserRepository purchaserRepository,
    IWareRepository wareRepository,
    IGoodsTypeRepository goodsTypeRepository,
    IGoodsRepository goodsRepository,
    ICustomerRepository customerRepository,
    IUnitOfWork unitOfWork,
    ILogger<PurchaseRuleService> logger,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IValidator<CreatePurchaseRuleDto> createValidator,
    IValidator<UpdatePurchaseRuleDto> updateValidator)
    : NamedCodeBaseDataService<PurchaseRule, PurchaseRuleDto, CreatePurchaseRuleDto, UpdatePurchaseRuleDto, PurchaseRuleQueryParameters>(
            repository, unitOfWork, logger, mapper, currentUserService, createValidator, updateValidator),
        IPurchaseRuleService
{
    protected override string DisplayName => "采购规则";

    protected override Expression<Func<PurchaseRule, bool>> BuildPredicate(PurchaseRuleQueryParameters parameters)
    {
        return parameters.QueryBuild();
    }

    protected override async Task ValidateCreateAsync(CreatePurchaseRuleDto dto)
    {
        await base.ValidateCreateAsync(dto);
        await ValidateReferencesAsync(dto);
    }

    protected override async Task ValidateUpdateAsync(Guid id, UpdatePurchaseRuleDto dto)
    {
        await base.ValidateUpdateAsync(id, dto);
        await ValidateReferencesAsync(dto);
    }

    protected override async Task AfterCreateAsync(PurchaseRule entity, CreatePurchaseRuleDto dto)
    {
        await repository.ReplaceGoodsRelationsAsync(entity.Id, dto.GoodsIds);
        await repository.ReplaceCustomerRelationsAsync(entity.Id, dto.CustomerIds);
    }

    protected override async Task AfterUpdateAsync(PurchaseRule entity, UpdatePurchaseRuleDto dto)
    {
        await repository.ReplaceGoodsRelationsAsync(entity.Id, dto.GoodsIds);
        await repository.ReplaceCustomerRelationsAsync(entity.Id, dto.CustomerIds);
    }

    private async Task ValidateReferencesAsync(CreatePurchaseRuleDto dto)
    {
        if (dto.SupplierId.HasValue && !await supplierRepository.ExistsAsync(dto.SupplierId.Value))
        {
            throw new BusinessException("供应商不存在");
        }

        if (dto.PurchaserId.HasValue && !await purchaserRepository.ExistsAsync(dto.PurchaserId.Value))
        {
            throw new BusinessException("采购员不存在");
        }

        if (dto.WareId.HasValue && !await wareRepository.ExistsAsync(dto.WareId.Value))
        {
            throw new BusinessException("仓库不存在");
        }

        if (dto.GoodsTypeId.HasValue && !await goodsTypeRepository.ExistsAsync(dto.GoodsTypeId.Value))
        {
            throw new BusinessException("商品分类不存在");
        }

        var goodsIds = dto.GoodsIds?.Where(x => x != Guid.Empty).Distinct().ToList() ?? [];
        if (goodsIds.Count > 0)
        {
            var goods = await goodsRepository.GetByIdsAsync(goodsIds);
            if (goods.Count != goodsIds.Count)
            {
                throw new BusinessException("部分适用商品不存在");
            }
        }

        var customerIds = dto.CustomerIds?.Where(x => x != Guid.Empty).Distinct().ToList() ?? [];
        if (customerIds.Count > 0)
        {
            var customers = await customerRepository.GetByIdsAsync(customerIds);
            if (customers.Count != customerIds.Count)
            {
                throw new BusinessException("部分适用客户不存在");
            }
        }
    }
}
