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

