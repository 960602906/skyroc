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
public class PurchaserService(
    IPurchaserRepository repository,
    IUserRepository userRepository,
    IDepartmentRepository departmentRepository,
    IUnitOfWork unitOfWork,
    ILogger<PurchaserService> logger,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IValidator<CreatePurchaserDto> createValidator,
    IValidator<UpdatePurchaserDto> updateValidator)
    : NamedCodeBaseDataService<Purchaser, PurchaserDto, CreatePurchaserDto, UpdatePurchaserDto, PurchaserQueryParameters>(
            repository, unitOfWork, logger, mapper, currentUserService, createValidator, updateValidator),
        IPurchaserService
{
    /// <inheritdoc />
    protected override string DisplayName => "采购员";

    /// <inheritdoc />
    protected override Expression<Func<Purchaser, bool>> BuildPredicate(PurchaserQueryParameters parameters)
    {
        return parameters.QueryBuild();
    }

    /// <inheritdoc />
    protected override async Task ValidateCreateAsync(CreatePurchaserDto dto)
    {
        await base.ValidateCreateAsync(dto);
        await ValidateReferencesAsync(dto.UserId, dto.DepartmentId);
    }

    /// <inheritdoc />
    protected override async Task ValidateUpdateAsync(Guid id, UpdatePurchaserDto dto)
    {
        await base.ValidateUpdateAsync(id, dto);
        await ValidateReferencesAsync(dto.UserId, dto.DepartmentId);
    }

    private async Task ValidateReferencesAsync(Guid? userId, Guid? departmentId)
    {
        if (userId.HasValue && !await userRepository.ExistsAsync(userId.Value))
        {
            throw new BusinessException("关联系统用户不存在");
        }

        if (departmentId.HasValue && !await departmentRepository.ExistsAsync(departmentId.Value))
        {
            throw new BusinessException("所属部门不存在");
        }
    }
}
