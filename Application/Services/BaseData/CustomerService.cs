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

public class CustomerService(
    ICustomerRepository repository,
    ICompanyRepository companyRepository,
    IQuotationRepository quotationRepository,
    IWareRepository wareRepository,
    ICustomerTagRepository customerTagRepository,
    ICompanyInfoProvider companyInfoProvider,
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
    protected override string DisplayName => "客户";

    protected override Expression<Func<Customer, bool>> BuildPredicate(CustomerQueryParameters parameters)
    {
        return parameters.QueryBuild();
    }

    protected override async Task ValidateCreateAsync(CreateCustomerDto dto)
    {
        await base.ValidateCreateAsync(dto);
        await ValidateReferencesAsync(dto.CompanyId, dto.QuotationId, dto.DefaultWareId, dto.TagIds);
    }

    protected override async Task ValidateUpdateAsync(Guid id, UpdateCustomerDto dto)
    {
        await base.ValidateUpdateAsync(id, dto);
        await ValidateReferencesAsync(dto.CompanyId, dto.QuotationId, dto.DefaultWareId, dto.TagIds);
    }

    protected override async Task AfterCreateAsync(Customer entity, CreateCustomerDto dto)
    {
        await EnrichBusinessInfoAsync(entity);
        await repository.ReplaceTagRelationsAsync(entity.Id, dto.TagIds);
    }

    protected override async Task AfterUpdateAsync(Customer entity, UpdateCustomerDto dto)
    {
        await EnrichBusinessInfoAsync(entity);
        await repository.ReplaceTagRelationsAsync(entity.Id, dto.TagIds);
    }

    private async Task EnrichBusinessInfoAsync(Customer entity)
    {
        if (!ShouldFetchCompanyInfo(entity))
        {
            return;
        }

        var info = await companyInfoProvider.GetCompanyInfoAsync(entity.Name);
        if (info is null)
        {
            return;
        }

        ApplyBusinessInfo(entity, info);
    }

    private static bool ShouldFetchCompanyInfo(Customer entity)
    {
        return !string.IsNullOrWhiteSpace(entity.Name) &&
               (ContainsAny(entity.Name, "学校", "学院", "大学", "中学", "小学", "幼儿园", "公司") ||
                !string.IsNullOrWhiteSpace(entity.UnifiedSocialCreditCode) ||
                !string.IsNullOrWhiteSpace(entity.TaxpayerIdentificationNumber));
    }

    private static void ApplyBusinessInfo(Customer entity, CompanyBusinessInfo info)
    {
        entity.UnifiedSocialCreditCode = Coalesce(entity.UnifiedSocialCreditCode, info.UnifiedSocialCreditCode);
        entity.LegalRepresentative = Coalesce(entity.LegalRepresentative, info.LegalRepresentative);
        entity.RegisteredCapital = Coalesce(entity.RegisteredCapital, info.RegisteredCapital);
        entity.EstablishDate ??= info.EstablishDate;
        entity.BusinessTerm = Coalesce(entity.BusinessTerm, info.BusinessTerm);
        entity.RegistrationStatus = Coalesce(entity.RegistrationStatus, info.RegistrationStatus);
        entity.RegistrationAuthority = Coalesce(entity.RegistrationAuthority, info.RegistrationAuthority);
        entity.RegisteredAddress = Coalesce(entity.RegisteredAddress, info.RegisteredAddress);
        entity.BusinessScope = Coalesce(entity.BusinessScope, info.BusinessScope);
        entity.TaxpayerIdentificationNumber = Coalesce(entity.TaxpayerIdentificationNumber, info.UnifiedSocialCreditCode);
        entity.InvoiceTitle = Coalesce(entity.InvoiceTitle, info.Name ?? entity.Name);
        entity.InvoiceAddress = Coalesce(entity.InvoiceAddress, info.RegisteredAddress);
        entity.InvoicePhone = Coalesce(entity.InvoicePhone, info.ContactPhone);
        entity.InvoiceEmail = Coalesce(entity.InvoiceEmail, info.Email);
        entity.ContactPhone = Coalesce(entity.ContactPhone, info.ContactPhone);
        entity.Address = Coalesce(entity.Address, info.RegisteredAddress);
    }

    private static bool ContainsAny(string value, params string[] keywords)
    {
        return keywords.Any(keyword => value.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static string? Coalesce(string? current, string? incoming)
    {
        return string.IsNullOrWhiteSpace(current) ? incoming : current;
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

