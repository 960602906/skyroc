using System.Linq.Expressions;
using Application.DTOs.Delivery;
using Application.Exceptions;
using Application.interfaces;
using Application.QueryParameters;
using AutoMapper;
using Domain.Entities.Delivery;
using Domain.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Application.Services;

/// <inheritdoc cref="IDriverService" />
public class DriverService(
    IDriverRepository repository,
    ICarrierRepository carrierRepository,
    IUnitOfWork unitOfWork,
    ILogger<DriverService> logger,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IValidator<CreateDriverDto> createValidator,
    IValidator<UpdateDriverDto> updateValidator)
    : NamedCodeBaseDataService<Driver, DriverDto, CreateDriverDto, UpdateDriverDto, DriverQueryParameters>(
            repository, unitOfWork, logger, mapper, currentUserService, createValidator, updateValidator),
        IDriverService
{
    /// <inheritdoc />
    protected override string DisplayName => "司机";

    /// <inheritdoc />
    protected override Expression<Func<Driver, bool>> BuildPredicate(DriverQueryParameters parameters)
    {
        return parameters.QueryBuild();
    }

    /// <inheritdoc />
    protected override async Task ValidateCreateAsync(CreateDriverDto dto)
    {
        await base.ValidateCreateAsync(dto);
        await ValidateCarrierAsync(dto.CarrierId);
    }

    /// <inheritdoc />
    protected override async Task ValidateUpdateAsync(Guid id, UpdateDriverDto dto)
    {
        await base.ValidateUpdateAsync(id, dto);
        await ValidateCarrierAsync(dto.CarrierId);
    }

    private async Task ValidateCarrierAsync(Guid? carrierId)
    {
        if (carrierId.HasValue && !await carrierRepository.ExistsAsync(carrierId.Value))
        {
            throw new BusinessException("所属承运商不存在");
        }
    }
}
