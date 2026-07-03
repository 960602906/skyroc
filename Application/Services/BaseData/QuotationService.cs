using System.Linq.Expressions;
using Application.DTOs.Pricing;
using Application.DTOs.Purchases;
using Application.Exceptions;
using Application.interfaces;
using Application.QueryParameters;
using AutoMapper;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Application.Services;

/// <inheritdoc />
public class QuotationService(
    IQuotationRepository repository,
    ICustomerRepository customerRepository,
    IUnitOfWork unitOfWork,
    ILogger<QuotationService> logger,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IValidator<CreateQuotationDto> createValidator,
    IValidator<UpdateQuotationDto> updateValidator)
    : NamedCodeBaseDataService<Quotation, QuotationDto, CreateQuotationDto, UpdateQuotationDto, QuotationQueryParameters>(
            repository, unitOfWork, logger, mapper, currentUserService, createValidator, updateValidator),
        IQuotationService
{
    /// <inheritdoc />
    protected override string DisplayName => "报价单";

    /// <inheritdoc />
    protected override Expression<Func<Quotation, bool>> BuildPredicate(QuotationQueryParameters parameters)
    {
        return parameters.QueryBuild();
    }

    /// <inheritdoc />
    protected override async Task ValidateCreateAsync(CreateQuotationDto dto)
    {
        await base.ValidateCreateAsync(dto);
        await ValidateCustomersAsync(dto.CustomerIds);
    }

    /// <inheritdoc />
    protected override async Task ValidateUpdateAsync(Guid id, UpdateQuotationDto dto)
    {
        await base.ValidateUpdateAsync(id, dto);
        await ValidateCustomersAsync(dto.CustomerIds);
    }

    /// <inheritdoc />
    protected override async Task AfterCreateAsync(Quotation entity, CreateQuotationDto dto)
    {
        await repository.ReplaceCustomerRelationsAsync(entity.Id, dto.CustomerIds);
    }

    /// <inheritdoc />
    protected override async Task AfterUpdateAsync(Quotation entity, UpdateQuotationDto dto)
    {
        await repository.ReplaceCustomerRelationsAsync(entity.Id, dto.CustomerIds);
    }

    /// <inheritdoc />
    public async Task<QuotationDto> ToggleAuditAsync(Guid id, bool isAudited)
    {
        var entity = await repository.GetByIdAsync(id);
        if (entity is null)
        {
            throw new NotFoundException("报价单不存在");
        }

        entity.IsAudited = isAudited;
        ApplyUpdateAudit(entity);
        await repository.UpdateAsync(entity);
        await UnitOfWork.SaveChangesAsync();
        return Mapper.Map<QuotationDto>(entity);
    }

    private async Task ValidateCustomersAsync(IEnumerable<Guid>? customerIds)
    {
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
