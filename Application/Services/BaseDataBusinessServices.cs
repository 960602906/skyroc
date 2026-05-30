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

public class GoodsTypeService(
    IGoodsTypeRepository repository,
    IGoodsRepository goodsRepository,
    IUnitOfWork unitOfWork,
    ILogger<GoodsTypeService> logger,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IValidator<CreateGoodsTypeDto> createValidator,
    IValidator<UpdateGoodsTypeDto> updateValidator)
    : TreeBaseDataService<GoodsType, GoodsTypeDto, CreateGoodsTypeDto, UpdateGoodsTypeDto, GoodsTypeQueryParameters>(
            repository, unitOfWork, logger, mapper, currentUserService, createValidator, updateValidator),
        IGoodsTypeService
{
    protected override string DisplayName => "商品分类";

    protected override Expression<Func<GoodsType, bool>> BuildPredicate(GoodsTypeQueryParameters parameters)
    {
        return parameters.QueryBuild();
    }

    protected override async Task ValidateDeleteAsync(Guid id)
    {
        await base.ValidateDeleteAsync(id);
        if (await goodsRepository.ExistsAsync(x => x.GoodsTypeId == id))
        {
            throw new BusinessException("商品分类已被商品引用，不能删除");
        }
    }
}

public class GoodsService(
    IGoodsRepository repository,
    IGoodsTypeRepository goodsTypeRepository,
    ISupplierRepository supplierRepository,
    IWareRepository wareRepository,
    IUnitOfWork unitOfWork,
    ILogger<GoodsService> logger,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IValidator<CreateGoodsDto> createValidator,
    IValidator<UpdateGoodsDto> updateValidator)
    : NamedCodeBaseDataService<GoodsEntity, GoodsDto, CreateGoodsDto, UpdateGoodsDto, GoodsQueryParameters>(
            repository, unitOfWork, logger, mapper, currentUserService, createValidator, updateValidator),
        IGoodsService
{
    protected override string DisplayName => "商品";

    protected override Expression<Func<GoodsEntity, bool>> BuildPredicate(GoodsQueryParameters parameters)
    {
        return parameters.QueryBuild();
    }

    protected override async Task ValidateCreateAsync(CreateGoodsDto dto)
    {
        await base.ValidateCreateAsync(dto);
        await ValidateReferencesAsync(dto.GoodsTypeId, dto.DefaultSupplierId, dto.DefaultWareId, dto.SupplierIds);
    }

    protected override async Task ValidateUpdateAsync(Guid id, UpdateGoodsDto dto)
    {
        await base.ValidateUpdateAsync(id, dto);
        await ValidateReferencesAsync(dto.GoodsTypeId, dto.DefaultSupplierId, dto.DefaultWareId, dto.SupplierIds);
    }

    protected override async Task AfterCreateAsync(GoodsEntity entity, CreateGoodsDto dto)
    {
        await repository.ReplaceSupplierRelationsAsync(entity.Id, dto.SupplierIds, dto.DefaultSupplierId);
    }

    protected override async Task AfterUpdateAsync(GoodsEntity entity, UpdateGoodsDto dto)
    {
        await repository.ReplaceSupplierRelationsAsync(entity.Id, dto.SupplierIds, dto.DefaultSupplierId);
    }

    public async Task<GoodsDto> ToggleSaleStatusAsync(Guid id, bool isOnSale)
    {
        var entity = await repository.GetByIdAsync(id);
        if (entity is null)
        {
            throw new NotFoundException("商品不存在");
        }

        entity.IsOnSale = isOnSale;
        ApplyUpdateAudit(entity);
        await repository.UpdateAsync(entity);
        await UnitOfWork.SaveChangesAsync();
        return Mapper.Map<GoodsDto>(entity);
    }

    private async Task ValidateReferencesAsync(Guid goodsTypeId, Guid? supplierId, Guid? wareId, IEnumerable<Guid>? supplierIds)
    {
        if (!await goodsTypeRepository.ExistsAsync(goodsTypeId))
        {
            throw new BusinessException("商品分类不存在");
        }

        if (supplierId.HasValue && !await supplierRepository.ExistsAsync(supplierId.Value))
        {
            throw new BusinessException("默认供应商不存在");
        }

        if (wareId.HasValue && !await wareRepository.ExistsAsync(wareId.Value))
        {
            throw new BusinessException("默认仓库不存在");
        }

        var relationSupplierIds = supplierIds?.Where(x => x != Guid.Empty).Distinct().ToList() ?? [];
        if (relationSupplierIds.Count > 0)
        {
            var suppliers = await supplierRepository.GetByIdsAsync(relationSupplierIds);
            if (suppliers.Count != relationSupplierIds.Count)
            {
                throw new BusinessException("部分可供货供应商不存在");
            }
        }
    }
}

public class GoodsUnitService(
    IGoodsUnitRepository repository,
    IGoodsRepository goodsRepository,
    IUnitOfWork unitOfWork,
    ILogger<GoodsUnitService> logger,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IValidator<CreateGoodsUnitDto> createValidator,
    IValidator<UpdateGoodsUnitDto> updateValidator)
    : BaseDataService<GoodsUnit, GoodsUnitDto, CreateGoodsUnitDto, UpdateGoodsUnitDto, GoodsUnitQueryParameters>(
            repository, unitOfWork, logger, mapper, currentUserService, createValidator, updateValidator),
        IGoodsUnitService
{
    protected override string DisplayName => "商品单位";

    protected override Expression<Func<GoodsUnit, bool>> BuildPredicate(GoodsUnitQueryParameters parameters)
    {
        return parameters.QueryBuild();
    }

    public async Task<List<GoodsUnitDto>> GetByGoodsIdAsync(Guid goodsId)
    {
        var units = await repository.GetByGoodsIdAsync(goodsId);
        return Mapper.Map<List<GoodsUnitDto>>(units);
    }

    protected override async Task ValidateCreateAsync(CreateGoodsUnitDto dto)
    {
        if (!await goodsRepository.ExistsAsync(dto.GoodsId))
        {
            throw new BusinessException("商品不存在");
        }

        if (await repository.ExistsByGoodsAndNameAsync(dto.GoodsId, dto.Name!))
        {
            throw new BusinessException("商品单位名称已经存在");
        }
    }

    protected override async Task ValidateUpdateAsync(Guid id, UpdateGoodsUnitDto dto)
    {
        if (!await goodsRepository.ExistsAsync(dto.GoodsId))
        {
            throw new BusinessException("商品不存在");
        }

        if (await repository.ExistsByGoodsAndNameAsync(dto.GoodsId, dto.Name!, id))
        {
            throw new BusinessException("商品单位名称已经存在");
        }
    }

    protected override async Task AfterCreateAsync(GoodsUnit entity, CreateGoodsUnitDto dto)
    {
        if (dto.IsBaseUnit)
        {
            await repository.SetBaseUnitAsync(entity.GoodsId, entity.Id);
        }
    }

    protected override async Task AfterUpdateAsync(GoodsUnit entity, UpdateGoodsUnitDto dto)
    {
        if (dto.IsBaseUnit)
        {
            await repository.SetBaseUnitAsync(entity.GoodsId, entity.Id);
        }
    }
}

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
    protected override string DisplayName => "客户标签";

    protected override Expression<Func<CustomerTag, bool>> BuildPredicate(CustomerTagQueryParameters parameters)
    {
        return parameters.QueryBuild();
    }

    protected override async Task ValidateDeleteAsync(Guid id)
    {
        await base.ValidateDeleteAsync(id);
        if (await repository.HasCustomersAsync(id))
        {
            throw new BusinessException("客户标签已被客户使用，不能删除");
        }
    }
}

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
    protected override string DisplayName => "客户子账号";

    protected override Expression<Func<CustomerSubAccount, bool>> BuildPredicate(CustomerSubAccountQueryParameters parameters)
    {
        return parameters.QueryBuild();
    }

    protected override async Task ValidateCreateAsync(CreateCustomerSubAccountDto dto)
    {
        await ValidateReferencesAsync(dto.CompanyId, dto.CustomerId);
        if (await repository.ExistsByUsernameAsync(dto.Username!))
        {
            throw new BusinessException("登录账号已经存在");
        }
    }

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

public class PurchaserService(
    IPurchaserRepository repository,
    IUserRepository userRepository,
    IDepartmentRepository departmentRepository,
    IUnitOfWork unitOfWork,
    ILogger<PurchaserService> logger,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IValidator<CreatePurchaserDto> createValidator,
    IValidator<UpdatePurchaserDto> updateValidator)
    : NamedCodeBaseDataService<Purchaser, PurchaserDto, CreatePurchaserDto, UpdatePurchaserDto, PurchaserQueryParameters>(
            repository, unitOfWork, logger, mapper, currentUserService, createValidator, updateValidator),
        IPurchaserService
{
    protected override string DisplayName => "采购员";

    protected override Expression<Func<Purchaser, bool>> BuildPredicate(PurchaserQueryParameters parameters)
    {
        return parameters.QueryBuild();
    }

    protected override async Task ValidateCreateAsync(CreatePurchaserDto dto)
    {
        await base.ValidateCreateAsync(dto);
        await ValidateReferencesAsync(dto.UserId, dto.DepartmentId);
    }

    protected override async Task ValidateUpdateAsync(Guid id, UpdatePurchaserDto dto)
    {
        await base.ValidateUpdateAsync(id, dto);
        await ValidateReferencesAsync(dto.UserId, dto.DepartmentId);
    }

    private async Task ValidateReferencesAsync(Guid? userId, Guid? departmentId)
    {
        if (userId.HasValue && !await userRepository.ExistsAsync(userId.Value))
        {
            throw new BusinessException("关联系统用户不存在");
        }

        if (departmentId.HasValue && !await departmentRepository.ExistsAsync(departmentId.Value))
        {
            throw new BusinessException("所属部门不存在");
        }
    }
}

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
    protected override string DisplayName => "仓库";

    protected override Expression<Func<Ware, bool>> BuildPredicate(WareQueryParameters parameters)
    {
        return parameters.QueryBuild();
    }
}
