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
public class CustomerTagService(
    ICustomerTagRepository repository,
    IUnitOfWork unitOfWork,
    ILogger<CustomerTagService> logger,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IValidator<CreateCustomerTagDto> createValidator,
    IValidator<UpdateCustomerTagDto> updateValidator)
    : TreeBaseDataService<CustomerTag, CustomerTagDto, CreateCustomerTagDto, UpdateCustomerTagDto, CustomerTagQueryParameters>(
            repository, unitOfWork, logger, mapper, currentUserService, createValidator, updateValidator),
        ICustomerTagService
{
    /// <inheritdoc />
    protected override string DisplayName => "客户标签";

    /// <inheritdoc />
    protected override Expression<Func<CustomerTag, bool>> BuildPredicate(CustomerTagQueryParameters parameters)
    {
        return parameters.QueryBuild();
    }

    /// <inheritdoc />
    protected override async Task ValidateDeleteAsync(Guid id)
    {
        await base.ValidateDeleteAsync(id);
        if (await repository.HasCustomersAsync(id))
        {
            throw new BusinessException("客户标签已被客户使用，不能删除");
        }
    }
}
