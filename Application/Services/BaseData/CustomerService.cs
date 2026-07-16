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
public class CustomerService(
    ICustomerRepository repository,
    ICompanyRepository companyRepository,
    IQuotationRepository quotationRepository,
    IWareRepository wareRepository,
    ICustomerTagRepository customerTagRepository,
    IUnitOfWork unitOfWork,
    ILogger<CustomerService> logger,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IValidator<CreateCustomerDto> createValidator,
    IValidator<UpdateCustomerDto> updateValidator)
    : NamedCodeBaseDataService<Customer, CustomerDto, CreateCustomerDto, UpdateCustomerDto, CustomerQueryParameters>(
            repository, unitOfWork, logger, mapper, currentUserService, createValidator, updateValidator),
        ICustomerService
{
    /// <inheritdoc />
    protected override string DisplayName => "客户";

    /// <inheritdoc />
    protected override Expression<Func<Customer, bool>> BuildPredicate(CustomerQueryParameters parameters)
    {
        return parameters.QueryBuild();
    }

    /// <inheritdoc />
    protected override async Task ValidateCreateAsync(CreateCustomerDto dto)
    {
        await base.ValidateCreateAsync(dto);
        await ValidateReferencesAsync(dto.CompanyId, dto.QuotationId, dto.DefaultWareId, dto.TagIds);
    }

    /// <inheritdoc />
    protected override async Task ValidateUpdateAsync(Guid id, UpdateCustomerDto dto)
    {
        await base.ValidateUpdateAsync(id, dto);
        await ValidateReferencesAsync(dto.CompanyId, dto.QuotationId, dto.DefaultWareId, dto.TagIds);
    }

    /// <inheritdoc />
    protected override async Task AfterCreateAsync(Customer entity, CreateCustomerDto dto)
    {
        await repository.ReplaceTagRelationsAsync(entity.Id, dto.TagIds);
    }

    /// <inheritdoc />
    protected override async Task AfterUpdateAsync(Customer entity, UpdateCustomerDto dto)
    {
        await repository.ReplaceTagRelationsAsync(entity.Id, dto.TagIds);
    }

    private async Task ValidateReferencesAsync(Guid? companyId, Guid? quotationId, Guid? wareId, IEnumerable<Guid>? tagIds)
    {
        if (companyId.HasValue && !await companyRepository.ExistsAsync(companyId.Value))
        {
            throw new BusinessException("所属公司不存在");
        }

        if (quotationId.HasValue && !await quotationRepository.ExistsAsync(quotationId.Value))
        {
            throw new BusinessException("默认报价单不存在");
        }

        if (wareId.HasValue && !await wareRepository.ExistsAsync(wareId.Value))
        {
            throw new BusinessException("默认仓库不存在");
        }

        var tagIdList = tagIds?.Where(x => x != Guid.Empty).Distinct().ToList() ?? [];
        if (tagIdList.Count > 0)
        {
            var tags = await customerTagRepository.GetByIdsAsync(tagIdList);
            if (tags.Count != tagIdList.Count)
            {
                throw new BusinessException("部分客户标签不存在");
            }
        }
    }
}
