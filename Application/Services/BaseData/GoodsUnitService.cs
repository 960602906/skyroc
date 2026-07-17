using System.Linq.Expressions;
using Application.DTOs.Customers;
using Application.DTOs.Goods;
using Application.DTOs.Pricing;
using Application.DTOs.Purchases;
using Application.DTOs.Storage;
using Application.Exceptions;
using Application.Interfaces;
using Application.QueryParameters;
using AutoMapper;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using Domain.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace Application.Services;

/// <inheritdoc />
public class GoodsUnitService(
    IGoodsUnitRepository repository,
    IGoodsRepository goodsRepository,
    IUnitOfWork unitOfWork,
    ILogger<GoodsUnitService> logger,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IValidator<CreateGoodsUnitDto> createValidator,
    IValidator<UpdateGoodsUnitDto> updateValidator)
    : BaseDataService<GoodsUnit, GoodsUnitDto, CreateGoodsUnitDto, UpdateGoodsUnitDto, GoodsUnitQueryParameters>(
            repository, unitOfWork, logger, mapper, currentUserService, createValidator, updateValidator),
        IGoodsUnitService
{
    /// <inheritdoc />
    protected override string DisplayName => "商品单位";

    /// <inheritdoc />
    protected override Expression<Func<GoodsUnit, bool>> BuildPredicate(GoodsUnitQueryParameters parameters)
    {
        return parameters.QueryBuild();
    }

    /// <inheritdoc />
    public async Task<List<GoodsUnitDto>> GetByGoodsIdAsync(Guid goodsId)
    {
        var units = await repository.GetByGoodsIdAsync(goodsId);
        return Mapper.Map<List<GoodsUnitDto>>(units);
    }

    /// <inheritdoc />
    protected override async Task ValidateCreateAsync(CreateGoodsUnitDto dto)
    {
        if (!await goodsRepository.ExistsAsync(dto.GoodsId))
        {
            throw new BusinessException("商品不存在");
        }

        if (await repository.ExistsByGoodsAndNameAsync(dto.GoodsId, dto.Name!))
        {
            throw new BusinessException("商品单位名称已经存在");
        }
    }

    /// <inheritdoc />
    protected override async Task ValidateUpdateAsync(Guid id, UpdateGoodsUnitDto dto)
    {
        if (!await goodsRepository.ExistsAsync(dto.GoodsId))
        {
            throw new BusinessException("商品不存在");
        }

        if (await repository.ExistsByGoodsAndNameAsync(dto.GoodsId, dto.Name!, id))
        {
            throw new BusinessException("商品单位名称已经存在");
        }
    }

    /// <inheritdoc />
    protected override async Task AfterCreateAsync(GoodsUnit entity, CreateGoodsUnitDto dto)
    {
        if (dto.IsBaseUnit)
        {
            await repository.SetBaseUnitAsync(entity.GoodsId, entity.Id);
        }
    }

    /// <inheritdoc />
    protected override async Task AfterUpdateAsync(GoodsUnit entity, UpdateGoodsUnitDto dto)
    {
        if (dto.IsBaseUnit)
        {
            await repository.SetBaseUnitAsync(entity.GoodsId, entity.Id);
        }
    }
}
