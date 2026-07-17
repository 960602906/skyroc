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
public class GoodsTypeService(
    IGoodsTypeRepository repository,
    IGoodsRepository goodsRepository,
    IUnitOfWork unitOfWork,
    ILogger<GoodsTypeService> logger,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IValidator<CreateGoodsTypeDto> createValidator,
    IValidator<UpdateGoodsTypeDto> updateValidator)
    : TreeBaseDataService<GoodsType, GoodsTypeDto, CreateGoodsTypeDto, UpdateGoodsTypeDto, GoodsTypeQueryParameters>(
            repository, unitOfWork, logger, mapper, currentUserService, createValidator, updateValidator),
        IGoodsTypeService
{
    /// <inheritdoc />
    protected override string DisplayName => "商品分类";

    /// <inheritdoc />
    protected override Expression<Func<GoodsType, bool>> BuildPredicate(GoodsTypeQueryParameters parameters)
    {
        return parameters.QueryBuild();
    }

    /// <inheritdoc />
    protected override async Task ValidateDeleteAsync(Guid id)
    {
        await base.ValidateDeleteAsync(id);
        if (await goodsRepository.ExistsAsync(x => x.GoodsTypeId == id))
        {
            throw new BusinessException("商品分类已被商品引用，不能删除");
        }
    }
}
