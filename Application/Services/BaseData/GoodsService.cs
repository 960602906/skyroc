using System.Linq.Expressions;
using Application.DTOs.Customers;
using Application.DTOs.Goods;
using Application.DTOs.Pricing;
using Application.DTOs.Purchases;
using Application.DTOs.Storage;
using Application.Exceptions;
using Application.interfaces;
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
public class GoodsService(
    IGoodsRepository repository,
    IGoodsTypeRepository goodsTypeRepository,
    ISupplierRepository supplierRepository,
    IWareRepository wareRepository,
    IUnitOfWork unitOfWork,
    ILogger<GoodsService> logger,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IValidator<CreateGoodsDto> createValidator,
    IValidator<UpdateGoodsDto> updateValidator)
    : NamedCodeBaseDataService<GoodsEntity, GoodsDto, CreateGoodsDto, UpdateGoodsDto, GoodsQueryParameters>(
            repository, unitOfWork, logger, mapper, currentUserService, createValidator, updateValidator),
        IGoodsService
{
    /// <inheritdoc />
    protected override string DisplayName => "商品";

    /// <inheritdoc />
    protected override Expression<Func<GoodsEntity, bool>> BuildPredicate(GoodsQueryParameters parameters)
    {
        return parameters.QueryBuild();
    }

    /// <inheritdoc />
    protected override async Task ValidateCreateAsync(CreateGoodsDto dto)
    {
        await base.ValidateCreateAsync(dto);
        await ValidateReferencesAsync(dto.GoodsTypeId, dto.DefaultSupplierId, dto.DefaultWareId, dto.SupplierIds);
    }

    /// <inheritdoc />
    protected override async Task ValidateUpdateAsync(Guid id, UpdateGoodsDto dto)
    {
        await base.ValidateUpdateAsync(id, dto);
        await ValidateReferencesAsync(dto.GoodsTypeId, dto.DefaultSupplierId, dto.DefaultWareId, dto.SupplierIds);
    }

    /// <inheritdoc />
    protected override async Task AfterCreateAsync(GoodsEntity entity, CreateGoodsDto dto)
    {
        await repository.ReplaceSupplierRelationsAsync(entity.Id, dto.SupplierIds, dto.DefaultSupplierId);
    }

    /// <inheritdoc />
    protected override async Task AfterUpdateAsync(GoodsEntity entity, UpdateGoodsDto dto)
    {
        await repository.ReplaceSupplierRelationsAsync(entity.Id, dto.SupplierIds, dto.DefaultSupplierId);
    }

    /// <inheritdoc />
    public async Task<GoodsDto> ToggleSaleStatusAsync(Guid id, bool isOnSale)
    {
        var entity = await repository.GetByIdAsync(id);
        if (entity is null)
        {
            throw new NotFoundException("商品不存在");
        }

        entity.IsOnSale = isOnSale;
        ApplyUpdateAudit(entity);
        await repository.UpdateAsync(entity);
        await UnitOfWork.SaveChangesAsync();
        return Mapper.Map<GoodsDto>(entity);
    }

    private async Task ValidateReferencesAsync(Guid goodsTypeId, Guid? supplierId, Guid? wareId, IEnumerable<Guid>? supplierIds)
    {
        if (!await goodsTypeRepository.ExistsAsync(goodsTypeId))
        {
            throw new BusinessException("商品分类不存在");
        }

        if (supplierId.HasValue && !await supplierRepository.ExistsAsync(supplierId.Value))
        {
            throw new BusinessException("默认供应商不存在");
        }

        if (wareId.HasValue && !await wareRepository.ExistsAsync(wareId.Value))
        {
            throw new BusinessException("默认仓库不存在");
        }

        var relationSupplierIds = supplierIds?.Where(x => x != Guid.Empty).Distinct().ToList() ?? [];
        if (relationSupplierIds.Count > 0)
        {
            var suppliers = await supplierRepository.GetByIdsAsync(relationSupplierIds);
            if (suppliers.Count != relationSupplierIds.Count)
            {
                throw new BusinessException("部分可供货供应商不存在");
            }
        }
    }
}
