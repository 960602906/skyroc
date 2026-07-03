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

/// <inheritdoc />
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
    /// <inheritdoc />
    protected override string DisplayName => "报价商品";

    /// <inheritdoc />
    protected override Expression<Func<QuotationGoods, bool>> BuildPredicate(QuotationGoodsQueryParameters parameters)
    {
        return parameters.QueryBuild();
    }

    /// <inheritdoc />
    protected override async Task ValidateCreateAsync(CreateQuotationGoodsDto dto)
    {
        await ValidateReferencesAsync(dto.QuotationId, dto.GoodsId, dto.GoodsUnitId);
        if (await repository.ExistsDetailAsync(dto.QuotationId, dto.GoodsId, dto.GoodsUnitId))
        {
            throw new BusinessException("报价商品明细已经存在");
        }
    }

    /// <inheritdoc />
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
