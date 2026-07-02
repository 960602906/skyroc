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

public class CompanyService(
    ICompanyRepository repository,
    IUnitOfWork unitOfWork,
    ILogger<CompanyService> logger,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IValidator<CreateCompanyDto> createValidator,
    IValidator<UpdateCompanyDto> updateValidator)
    : NamedCodeBaseDataService<Company, CompanyDto, CreateCompanyDto, UpdateCompanyDto, CompanyQueryParameters>(
            repository, unitOfWork, logger, mapper, currentUserService, createValidator, updateValidator),
        ICompanyService
{
    protected override string DisplayName => "公司";

    protected override Expression<Func<Company, bool>> BuildPredicate(CompanyQueryParameters parameters)
    {
        return parameters.QueryBuild();
    }
}

