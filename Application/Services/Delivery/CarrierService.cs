using System.Linq.Expressions;
using Application.DTOs.Delivery;
using Application.interfaces;
using Application.QueryParameters;
using AutoMapper;
using Domain.Entities.Delivery;
using Domain.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Application.Services;

/// <inheritdoc cref="ICarrierService" />
public class CarrierService(
    ICarrierRepository repository,
    IUnitOfWork unitOfWork,
    ILogger<CarrierService> logger,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IValidator<CreateCarrierDto> createValidator,
    IValidator<UpdateCarrierDto> updateValidator)
    : NamedCodeBaseDataService<Carrier, CarrierDto, CreateCarrierDto, UpdateCarrierDto, CarrierQueryParameters>(
            repository, unitOfWork, logger, mapper, currentUserService, createValidator, updateValidator),
        ICarrierService
{
    /// <inheritdoc />
    protected override string DisplayName => "承运商";

    /// <inheritdoc />
    protected override Expression<Func<Carrier, bool>> BuildPredicate(CarrierQueryParameters parameters)
    {
        return parameters.QueryBuild();
    }
}
