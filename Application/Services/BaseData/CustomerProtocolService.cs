using System.Linq.Expressions;
using Application.DTOs.Pricing;
using Application.DTOs.Purchases;
using Application.Exceptions;
using Application.Interfaces;
using Application.QueryParameters;
using AutoMapper;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Application.Services;

/// <inheritdoc />
public class CustomerProtocolService(
    ICustomerProtocolRepository repository,
    IQuotationRepository quotationRepository,
    ICustomerRepository customerRepository,
    IUnitOfWork unitOfWork,
    ILogger<CustomerProtocolService> logger,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IValidator<CreateCustomerProtocolDto> createValidator,
    IValidator<UpdateCustomerProtocolDto> updateValidator)
    : NamedCodeBaseDataService<CustomerProtocol, CustomerProtocolDto, CreateCustomerProtocolDto, UpdateCustomerProtocolDto, CustomerProtocolQueryParameters>(
            repository, unitOfWork, logger, mapper, currentUserService, createValidator, updateValidator),
        ICustomerProtocolService
{
    /// <inheritdoc />
    protected override string DisplayName => "客户协议价";

    /// <inheritdoc />
    protected override Expression<Func<CustomerProtocol, bool>> BuildPredicate(CustomerProtocolQueryParameters parameters)
    {
        return parameters.QueryBuild();
    }

    /// <inheritdoc />
    protected override async Task ValidateCreateAsync(CreateCustomerProtocolDto dto)
    {
        await base.ValidateCreateAsync(dto);
        await ValidateReferencesAsync(dto.QuotationId, dto.CustomerIds);
    }

    /// <inheritdoc />
    protected override async Task ValidateUpdateAsync(Guid id, UpdateCustomerProtocolDto dto)
    {
        await base.ValidateUpdateAsync(id, dto);
        await ValidateReferencesAsync(dto.QuotationId, dto.CustomerIds);
    }

    /// <inheritdoc />
    protected override async Task AfterCreateAsync(CustomerProtocol entity, CreateCustomerProtocolDto dto)
    {
        await repository.ReplaceCustomerRelationsAsync(entity.Id, dto.CustomerIds);
    }

    /// <inheritdoc />
    protected override async Task AfterUpdateAsync(CustomerProtocol entity, UpdateCustomerProtocolDto dto)
    {
        await repository.ReplaceCustomerRelationsAsync(entity.Id, dto.CustomerIds);
    }

    private async Task ValidateReferencesAsync(Guid? quotationId, IEnumerable<Guid>? customerIds)
    {
        if (quotationId.HasValue && !await quotationRepository.ExistsAsync(quotationId.Value))
        {
            throw new BusinessException("报价单不存在");
        }

        var idList = customerIds?.Where(x => x != Guid.Empty).Distinct().ToList() ?? [];
        if (idList.Count == 0)
        {
            return;
        }

        var customers = await customerRepository.GetByIdsAsync(idList);
        if (customers.Count != idList.Count)
        {
            throw new BusinessException("部分绑定客户不存在");
        }
    }
}
