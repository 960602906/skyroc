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
public class WareService(
    IWareRepository repository,
    IUnitOfWork unitOfWork,
    ILogger<WareService> logger,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IValidator<CreateWareDto> createValidator,
    IValidator<UpdateWareDto> updateValidator)
    : NamedCodeBaseDataService<Ware, WareDto, CreateWareDto, UpdateWareDto, WareQueryParameters>(
            repository, unitOfWork, logger, mapper, currentUserService, createValidator, updateValidator),
        IWareService
{
    /// <inheritdoc />
    protected override string DisplayName => "仓库";

    /// <inheritdoc />
    protected override Expression<Func<Ware, bool>> BuildPredicate(WareQueryParameters parameters)
    {
        return parameters.QueryBuild();
    }
}
