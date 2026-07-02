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

