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
public class CustomerSubAccountService(
    ICustomerSubAccountRepository repository,
    ICompanyRepository companyRepository,
    ICustomerRepository customerRepository,
    IUnitOfWork unitOfWork,
    ILogger<CustomerSubAccountService> logger,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IValidator<CreateCustomerSubAccountDto> createValidator,
    IValidator<UpdateCustomerSubAccountDto> updateValidator)
    : BaseDataService<CustomerSubAccount, CustomerSubAccountDto, CreateCustomerSubAccountDto, UpdateCustomerSubAccountDto, CustomerSubAccountQueryParameters>(
            repository, unitOfWork, logger, mapper, currentUserService, createValidator, updateValidator),
        ICustomerSubAccountService
{
    /// <inheritdoc />
    protected override string DisplayName => "客户子账号";

    /// <inheritdoc />
    protected override Expression<Func<CustomerSubAccount, bool>> BuildPredicate(CustomerSubAccountQueryParameters parameters)
    {
        return parameters.QueryBuild();
    }

    /// <inheritdoc />
    protected override async Task ValidateCreateAsync(CreateCustomerSubAccountDto dto)
    {
        await ValidateReferencesAsync(dto.CompanyId, dto.CustomerId);
        if (await repository.ExistsByUsernameAsync(dto.Username!))
        {
            throw new BusinessException("登录账号已经存在");
        }
    }

    /// <inheritdoc />
    protected override async Task ValidateUpdateAsync(Guid id, UpdateCustomerSubAccountDto dto)
    {
        await ValidateReferencesAsync(dto.CompanyId, dto.CustomerId);
        if (await repository.ExistsByUsernameAsync(dto.Username!, id))
        {
            throw new BusinessException("登录账号已经存在");
        }
    }

    private async Task ValidateReferencesAsync(Guid companyId, Guid? customerId)
    {
        if (!await companyRepository.ExistsAsync(companyId))
        {
            throw new BusinessException("所属公司不存在");
        }

        if (customerId.HasValue && !await customerRepository.ExistsAsync(customerId.Value))
        {
            throw new BusinessException("授权客户不存在");
        }
    }
}
