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

public class SupplierService(
    ISupplierRepository repository,
    IUnitOfWork unitOfWork,
    ILogger<SupplierService> logger,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IValidator<CreateSupplierDto> createValidator,
    IValidator<UpdateSupplierDto> updateValidator)
    : NamedCodeBaseDataService<Supplier, SupplierDto, CreateSupplierDto, UpdateSupplierDto, SupplierQueryParameters>(
            repository, unitOfWork, logger, mapper, currentUserService, createValidator, updateValidator),
        ISupplierService
{
    protected override string DisplayName => "供应商";

    protected override Expression<Func<Supplier, bool>> BuildPredicate(SupplierQueryParameters parameters)
    {
        return parameters.QueryBuild();
    }
}

