using Application.DTOs.Customers;
using Application.DTOs.Goods;
using Application.DTOs.Storage;
using Application.interfaces;
using Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using Shared.Constants;

namespace SkyRoc.Tests.Testing.PostgreSql;

/// <summary>
///     按完整稳定业务键补齐长期前端联调数据；当前先提供公司基础资料层，后续层在同一安全边界内追加。
/// </summary>
public sealed class DemoDataGenerator(PostgreSqlTestFixture fixture)
{
    private const string CompaniesLayer = "companies";
    private const string CustomerTagsLayer = "customer-tags";
    private const string CustomersLayer = "customers";
    private const string GoodsTypesLayer = "goods-types";
    private const string WaresLayer = "wares";

    /// <summary>
    ///     在经白名单验证的真实 PostgreSQL 中幂等生成当前已实现的联调资料层。
    /// </summary>
    /// <param name="cancellationToken">取消生成的令牌。</param>
    /// <returns>按资料层汇总的新增与复用数量。</returns>
    public async Task<DemoDataGenerationResult> GenerateAsync(CancellationToken cancellationToken = default)
    {
        DatabaseSafetyGuard.Validate(fixture.Settings);

        using var factory = fixture.CreateWebApplicationFactory();
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var companyService = scope.ServiceProvider.GetRequiredService<ICompanyService>();
        var customerTagService = scope.ServiceProvider.GetRequiredService<ICustomerTagService>();
        var customerService = scope.ServiceProvider.GetRequiredService<ICustomerService>();
        var goodsTypeService = scope.ServiceProvider.GetRequiredService<IGoodsTypeService>();
        var wareService = scope.ServiceProvider.GetRequiredService<IWareService>();
        var companySeeds = CreateCompanySeeds();
        var customerTagSeeds = CreateCustomerTagSeeds();
        var customerSeeds = CreateCustomerSeeds();
        var goodsTypeSeeds = CreateGoodsTypeSeeds();
        var wareSeeds = CreateWareSeeds();
        var companyCodes = companySeeds.Select(seed => seed.Code).ToArray();
        var customerTagCodes = customerTagSeeds.Select(seed => seed.Code).ToArray();
        var customerCodes = customerSeeds.Select(seed => seed.Code).ToArray();
        var goodsTypeCodes = goodsTypeSeeds.Select(seed => seed.Code).ToArray();
        var wareCodes = wareSeeds.Select(seed => seed.Code).ToArray();
        var auditUser = await context.Users
            .OrderBy(user => user.Username)
            .Select(user => new DemoAuditUser(user.Id, user.Username))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("长期联调数据生成需要已存在的可审计系统用户。");
        SetAuditUser(scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>(), auditUser);
        var existingCompanies = await context.Companies
            .Where(company => companyCodes.Contains(company.Code))
            .ToDictionaryAsync(company => company.Code, StringComparer.Ordinal, cancellationToken);
        var existingCustomerTags = await context.CustomerTags
            .Where(tag => customerTagCodes.Contains(tag.Code))
            .ToDictionaryAsync(tag => tag.Code, StringComparer.Ordinal, cancellationToken);
        var existingWares = await context.Wares
            .Where(ware => wareCodes.Contains(ware.Code))
            .ToDictionaryAsync(ware => ware.Code, StringComparer.Ordinal, cancellationToken);

        var createdCompanies = 0;
        var reusedCompanies = 0;
        var createdCustomerTags = 0;
        var reusedCustomerTags = 0;
        var createdCustomers = 0;
        var reusedCustomers = 0;
        var createdGoodsTypes = 0;
        var reusedGoodsTypes = 0;
        var createdWares = 0;
        var reusedWares = 0;
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var seed in companySeeds)
            {
                if (!existingCompanies.TryGetValue(seed.Code, out var company))
                {
                    await companyService.CreateAsync(seed.ToCreateDto());
                    createdCompanies++;
                    continue;
                }

                if (!seed.Matches(company))
                    await companyService.UpdateAsync(company.Id, seed.ToUpdateDto(company.Id));

                if (company.CreateBy != auditUser.Id || company.CreateName != auditUser.Username)
                {
                    // 创建审计字段没有公开补写接口，只对完整稳定键命中的受管记录受控修复。
                    company.CreateBy = auditUser.Id;
                    company.CreateName = auditUser.Username;
                }

                reusedCompanies++;
            }

            foreach (var seed in customerTagSeeds)
            {
                if (!existingCustomerTags.TryGetValue(seed.Code, out var tag))
                {
                    await customerTagService.CreateAsync(seed.ToCreateDto());
                    createdCustomerTags++;
                    continue;
                }

                if (!seed.Matches(tag))
                    await customerTagService.UpdateAsync(tag.Id, seed.ToUpdateDto(tag.Id));

                if (tag.CreateBy != auditUser.Id || tag.CreateName != auditUser.Username)
                {
                    // 标签没有补写创建审计的公开入口，只修复完整稳定键命中的受管记录。
                    tag.CreateBy = auditUser.Id;
                    tag.CreateName = auditUser.Username;
                }

                reusedCustomerTags++;
            }

            var managedCompanies = await context.Companies
                .Where(company => companyCodes.Contains(company.Code))
                .ToDictionaryAsync(company => company.Code, StringComparer.Ordinal, cancellationToken);
            var managedCustomerTags = await context.CustomerTags
                .Where(tag => customerTagCodes.Contains(tag.Code))
                .ToDictionaryAsync(tag => tag.Code, StringComparer.Ordinal, cancellationToken);
            var existingCustomers = await context.Customers
                .Include(customer => customer.TagRelations)
                .Where(customer => customerCodes.Contains(customer.Code))
                .ToDictionaryAsync(customer => customer.Code, StringComparer.Ordinal, cancellationToken);

            foreach (var seed in customerSeeds)
            {
                var companyId = GetManagedReferenceId(managedCompanies, seed.CompanyCode, "公司");
                var customerTagId = GetManagedReferenceId(managedCustomerTags, seed.CustomerTagCode, "客户标签");
                if (!existingCustomers.TryGetValue(seed.Code, out var customer))
                {
                    await customerService.CreateAsync(seed.ToCreateDto(companyId, customerTagId));
                    createdCustomers++;
                    continue;
                }

                if (!seed.Matches(customer, companyId, customerTagId))
                    await customerService.UpdateAsync(customer.Id, seed.ToUpdateDto(customer.Id, companyId, customerTagId));

                if (customer.CreateBy != auditUser.Id || customer.CreateName != auditUser.Username)
                {
                    // 客户创建审计没有补写入口，仅修复完整稳定编码对应的受管记录。
                    customer.CreateBy = auditUser.Id;
                    customer.CreateName = auditUser.Username;
                }

                reusedCustomers++;
            }

            var existingGoodsTypes = await context.GoodsTypes
                .Where(goodsType => goodsTypeCodes.Contains(goodsType.Code))
                .ToDictionaryAsync(goodsType => goodsType.Code, StringComparer.Ordinal, cancellationToken);
            foreach (var seed in goodsTypeSeeds)
            {
                if (!existingGoodsTypes.TryGetValue(seed.Code, out var goodsType))
                {
                    await goodsTypeService.CreateAsync(seed.ToCreateDto());
                    createdGoodsTypes++;
                    continue;
                }

                if (!seed.Matches(goodsType))
                    await goodsTypeService.UpdateAsync(goodsType.Id, seed.ToUpdateDto(goodsType.Id));

                if (goodsType.CreateBy != auditUser.Id || goodsType.CreateName != auditUser.Username)
                {
                    // 商品分类的创建审计没有公开补写入口，仅修复完整稳定编码对应的受管记录。
                    goodsType.CreateBy = auditUser.Id;
                    goodsType.CreateName = auditUser.Username;
                }

                reusedGoodsTypes++;
            }

            foreach (var seed in wareSeeds)
            {
                if (!existingWares.TryGetValue(seed.Code, out var ware))
                {
                    await wareService.CreateAsync(seed.ToCreateDto());
                    createdWares++;
                    continue;
                }

                if (!seed.Matches(ware))
                    await wareService.UpdateAsync(ware.Id, seed.ToUpdateDto(ware.Id));

                if (ware.CreateBy != auditUser.Id || ware.CreateName != auditUser.Username)
                {
                    // 仓库创建审计没有公开补写入口，仅修复完整稳定编码对应的受管记录。
                    ware.CreateBy = auditUser.Id;
                    ware.CreateName = auditUser.Username;
                }

                reusedWares++;
            }

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return new DemoDataGenerationResult(
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                [CompaniesLayer] = createdCompanies,
                [CustomerTagsLayer] = createdCustomerTags,
                [CustomersLayer] = createdCustomers,
                [GoodsTypesLayer] = createdGoodsTypes,
                [WaresLayer] = createdWares
            },
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                [CompaniesLayer] = reusedCompanies,
                [CustomerTagsLayer] = reusedCustomerTags,
                [CustomersLayer] = reusedCustomers,
                [GoodsTypesLayer] = reusedGoodsTypes,
                [WaresLayer] = reusedWares
            });
    }

    private static IReadOnlyList<CompanySeed> CreateCompanySeeds()
    {
        return Enumerable.Range(1, 30)
            .Select(sequence => new CompanySeed(
                DemoDataStableKeyCatalog.Create("COMPANY", sequence),
                $"华东鲜品供应链有限公司{sequence:D2}",
                $"陈经理{sequence:D2}",
                $"021-6800{sequence:D4}",
                $"上海市浦东新区鲜品大道{sequence}号冷链供应中心",
                $"SkyRoc 联调公司资料：华东区域第 {sequence:D2} 个采购与配送业务主体。"))
            .ToArray();
    }

    private static IReadOnlyList<CustomerTagSeed> CreateCustomerTagSeeds()
    {
        return Enumerable.Range(1, 30)
            .Select(sequence => new CustomerTagSeed(
                DemoDataStableKeyCatalog.Create("CUSTOMER-TAG", sequence),
                $"华东联调客户分群{sequence:D2}",
                sequence,
                $"SkyRoc 联调客户标签：覆盖华东第 {sequence:D2} 个客户服务与价格分群。"))
            .ToArray();
    }

    private static IReadOnlyList<CustomerSeed> CreateCustomerSeeds()
    {
        return Enumerable.Range(1, 30)
            .Select(sequence => new CustomerSeed(
                DemoDataStableKeyCatalog.Create("CUSTOMER", sequence),
                DemoDataStableKeyCatalog.Create("COMPANY", sequence),
                DemoDataStableKeyCatalog.Create("CUSTOMER-TAG", sequence),
                $"华东鲜品团餐客户服务中心{sequence:D2}",
                $"91310115DEMO{sequence:D6}",
                $"李主任{sequence:D2}",
                $"{500 + sequence * 10}万元人民币",
                new DateTime(2020 + sequence % 5, sequence % 12 + 1, sequence % 27 + 1, 0, 0, 0, DateTimeKind.Utc),
                "2020-01-01 至 2040-12-31",
                "存续",
                "上海市浦东新区市场监督管理局",
                $"上海市浦东新区鲜品大道{sequence}号客户服务楼",
                "团餐配送、食材采购、食品销售及供应链管理服务。",
                $"华东鲜品团餐客户服务中心{sequence:D2}",
                $"91310115DEMO{sequence:D6}",
                $"上海市浦东新区鲜品大道{sequence}号客户服务楼",
                $"021-6800{sequence:D4}",
                "上海农商银行浦东鲜品支行",
                $"622580100000{sequence:D6}",
                $"王会计{sequence:D2}",
                $"1390020{sequence:D4}",
                $"上海市浦东新区鲜品大道{sequence}号客户收票室",
                $"billing{sequence:D2}@eastfresh.example",
                $"周经理{sequence:D2}",
                $"1380010{sequence:D4}",
                $"上海市浦东新区鲜品大道{sequence}号配送收货区",
                $"SkyRoc 联调客户资料：华东第 {sequence:D2} 个团餐客户，覆盖采购、配送和结算场景。"))
            .ToArray();
    }

    private static IReadOnlyList<GoodsTypeSeed> CreateGoodsTypeSeeds()
    {
        return Enumerable.Range(1, 30)
            .Select(sequence => new GoodsTypeSeed(
                DemoDataStableKeyCatalog.Create("GOODS-TYPE", sequence),
                $"华东联调生鲜分类{sequence:D2}",
                $"https://assets.skyroc.example/goods-types/east-fresh-{sequence:D2}.png",
                $"1010{sequence:D6}",
                $"华东生鲜食材税收分类{sequence:D2}",
                sequence % 2 == 0 ? "生鲜蔬菜" : "冷链食材",
                sequence % 5 == 0 ? 0m : 0.09m,
                sequence % 5 == 0,
                sequence % 5 == 0 ? "农产品流通环节免征增值税政策。" : null,
                sequence,
                $"SkyRoc 联调商品分类：华东第 {sequence:D2} 个生鲜品类，用于商品建档、报价和采购规则。"))
            .ToArray();
    }

    private static IReadOnlyList<WareSeed> CreateWareSeeds()
    {
        return Enumerable.Range(1, 30)
            .Select(sequence => new WareSeed(
                DemoDataStableKeyCatalog.Create("WARE", sequence),
                $"华东冷链联调仓库{sequence:D2}",
                $"赵库管{sequence:D2}",
                $"021-6900{sequence:D4}",
                $"上海市浦东新区冷链物流园{sequence}号仓储中心",
                sequence,
                $"SkyRoc 联调仓库资料：华东第 {sequence:D2} 个冷链仓储节点，支持订单履约与库存批次场景。"))
            .ToArray();
    }

    private static Guid GetManagedReferenceId<T>(
        IReadOnlyDictionary<string, T> entities,
        string businessCode,
        string referenceName)
        where T : class
    {
        return entities.TryGetValue(businessCode, out var entity)
            ? entity switch
            {
                Domain.Entities.Customers.Company company => company.Id,
                Domain.Entities.Customers.CustomerTag customerTag => customerTag.Id,
                _ => throw new InvalidOperationException($"不支持的{referenceName}受管引用类型。")
            }
            : throw new InvalidOperationException($"未找到稳定编码为 {businessCode} 的受管{referenceName}。 ");
    }

    private static void SetAuditUser(IHttpContextAccessor httpContextAccessor, DemoAuditUser auditUser)
    {
        httpContextAccessor.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, auditUser.Id.ToString()),
                new Claim(ClaimTypes.Name, auditUser.Username)
            ],
            "DemoDataGenerator"))
        };
    }

    private sealed record CompanySeed(
        string Code,
        string Name,
        string ContactName,
        string ContactPhone,
        string Address,
        string Remark)
    {
        public CreateCompanyDto ToCreateDto() => new()
        {
            Code = Code,
            Name = Name,
            ContactName = ContactName,
            ContactPhone = ContactPhone,
            Address = Address,
            Remark = Remark,
            Status = Status.Enable
        };

        public UpdateCompanyDto ToUpdateDto(Guid id) => new()
        {
            Id = id,
            Code = Code,
            Name = Name,
            ContactName = ContactName,
            ContactPhone = ContactPhone,
            Address = Address,
            Remark = Remark,
            Status = Status.Enable
        };

        public bool Matches(Domain.Entities.Customers.Company company)
        {
            return company.Name == Name
                   && company.ContactName == ContactName
                   && company.ContactPhone == ContactPhone
                   && company.Address == Address
                   && company.Remark == Remark
                   && company.Status == Status.Enable;
        }
    }

    private sealed record CustomerTagSeed(
        string Code,
        string Name,
        int Sort,
        string Remark)
    {
        public CreateCustomerTagDto ToCreateDto() => new()
        {
            Code = Code,
            Name = Name,
            Sort = Sort,
            Remark = Remark,
            Status = Status.Enable
        };

        public UpdateCustomerTagDto ToUpdateDto(Guid id) => new()
        {
            Id = id,
            Code = Code,
            Name = Name,
            Sort = Sort,
            Remark = Remark,
            Status = Status.Enable
        };

        public bool Matches(Domain.Entities.Customers.CustomerTag tag)
        {
            return tag.Name == Name
                   && tag.Sort == Sort
                   && tag.Remark == Remark
                   && tag.Status == Status.Enable
                   && tag.ParentId is null;
        }
    }

    private sealed record CustomerSeed(
        string Code,
        string CompanyCode,
        string CustomerTagCode,
        string Name,
        string UnifiedSocialCreditCode,
        string LegalRepresentative,
        string RegisteredCapital,
        DateTime EstablishDate,
        string BusinessTerm,
        string RegistrationStatus,
        string RegistrationAuthority,
        string RegisteredAddress,
        string BusinessScope,
        string InvoiceTitle,
        string TaxpayerIdentificationNumber,
        string InvoiceAddress,
        string InvoicePhone,
        string BankName,
        string BankAccount,
        string InvoiceReceiverName,
        string InvoiceReceiverPhone,
        string InvoiceReceiverAddress,
        string InvoiceEmail,
        string ContactName,
        string ContactPhone,
        string Address,
        string Remark)
    {
        public CreateCustomerDto ToCreateDto(Guid companyId, Guid customerTagId) => new()
        {
            Code = Code,
            Name = Name,
            CompanyId = companyId,
            UnifiedSocialCreditCode = UnifiedSocialCreditCode,
            LegalRepresentative = LegalRepresentative,
            RegisteredCapital = RegisteredCapital,
            EstablishDate = EstablishDate,
            BusinessTerm = BusinessTerm,
            RegistrationStatus = RegistrationStatus,
            RegistrationAuthority = RegistrationAuthority,
            RegisteredAddress = RegisteredAddress,
            BusinessScope = BusinessScope,
            InvoiceTitle = InvoiceTitle,
            TaxpayerIdentificationNumber = TaxpayerIdentificationNumber,
            InvoiceAddress = InvoiceAddress,
            InvoicePhone = InvoicePhone,
            BankName = BankName,
            BankAccount = BankAccount,
            InvoiceReceiverName = InvoiceReceiverName,
            InvoiceReceiverPhone = InvoiceReceiverPhone,
            InvoiceReceiverAddress = InvoiceReceiverAddress,
            InvoiceEmail = InvoiceEmail,
            ContactName = ContactName,
            ContactPhone = ContactPhone,
            Address = Address,
            Remark = Remark,
            TagIds = [customerTagId],
            Status = Status.Enable
        };

        public UpdateCustomerDto ToUpdateDto(Guid id, Guid companyId, Guid customerTagId)
        {
            var dto = ToCreateDto(companyId, customerTagId);
            return new UpdateCustomerDto
            {
                Id = id,
                Code = dto.Code,
                Name = dto.Name,
                CompanyId = dto.CompanyId,
                UnifiedSocialCreditCode = dto.UnifiedSocialCreditCode,
                LegalRepresentative = dto.LegalRepresentative,
                RegisteredCapital = dto.RegisteredCapital,
                EstablishDate = dto.EstablishDate,
                BusinessTerm = dto.BusinessTerm,
                RegistrationStatus = dto.RegistrationStatus,
                RegistrationAuthority = dto.RegistrationAuthority,
                RegisteredAddress = dto.RegisteredAddress,
                BusinessScope = dto.BusinessScope,
                InvoiceTitle = dto.InvoiceTitle,
                TaxpayerIdentificationNumber = dto.TaxpayerIdentificationNumber,
                InvoiceAddress = dto.InvoiceAddress,
                InvoicePhone = dto.InvoicePhone,
                BankName = dto.BankName,
                BankAccount = dto.BankAccount,
                InvoiceReceiverName = dto.InvoiceReceiverName,
                InvoiceReceiverPhone = dto.InvoiceReceiverPhone,
                InvoiceReceiverAddress = dto.InvoiceReceiverAddress,
                InvoiceEmail = dto.InvoiceEmail,
                ContactName = dto.ContactName,
                ContactPhone = dto.ContactPhone,
                Address = dto.Address,
                Remark = dto.Remark,
                TagIds = dto.TagIds,
                Status = dto.Status
            };
        }

        public bool Matches(Domain.Entities.Customers.Customer customer, Guid companyId, Guid customerTagId)
        {
            return customer.CompanyId == companyId
                   && customer.Name == Name
                   && customer.UnifiedSocialCreditCode == UnifiedSocialCreditCode
                   && customer.LegalRepresentative == LegalRepresentative
                   && customer.RegisteredCapital == RegisteredCapital
                   && customer.EstablishDate == EstablishDate
                   && customer.BusinessTerm == BusinessTerm
                   && customer.RegistrationStatus == RegistrationStatus
                   && customer.RegistrationAuthority == RegistrationAuthority
                   && customer.RegisteredAddress == RegisteredAddress
                   && customer.BusinessScope == BusinessScope
                   && customer.InvoiceTitle == InvoiceTitle
                   && customer.TaxpayerIdentificationNumber == TaxpayerIdentificationNumber
                   && customer.InvoiceAddress == InvoiceAddress
                   && customer.InvoicePhone == InvoicePhone
                   && customer.BankName == BankName
                   && customer.BankAccount == BankAccount
                   && customer.InvoiceReceiverName == InvoiceReceiverName
                   && customer.InvoiceReceiverPhone == InvoiceReceiverPhone
                   && customer.InvoiceReceiverAddress == InvoiceReceiverAddress
                   && customer.InvoiceEmail == InvoiceEmail
                   && customer.ContactName == ContactName
                   && customer.ContactPhone == ContactPhone
                   && customer.Address == Address
                   && customer.Remark == Remark
                   && customer.Status == Status.Enable
                   && customer.TagRelations.Count == 1
                   && customer.TagRelations.Single().CustomerTagId == customerTagId;
        }
    }

    private sealed record GoodsTypeSeed(
        string Code,
        string Name,
        string ImageUrl,
        string TaxCategoryCode,
        string TaxCategoryName,
        string InvoiceGoodsShortName,
        decimal DefaultTaxRate,
        bool IsTaxExempt,
        string? TaxPolicyBasis,
        int Sort,
        string Remark)
    {
        public CreateGoodsTypeDto ToCreateDto() => new()
        {
            Code = Code,
            Name = Name,
            ImageUrl = ImageUrl,
            TaxCategoryCode = TaxCategoryCode,
            TaxCategoryName = TaxCategoryName,
            InvoiceGoodsShortName = InvoiceGoodsShortName,
            DefaultTaxRate = DefaultTaxRate,
            IsTaxExempt = IsTaxExempt,
            TaxPolicyBasis = TaxPolicyBasis,
            Sort = Sort,
            Remark = Remark,
            Status = Status.Enable
        };

        public UpdateGoodsTypeDto ToUpdateDto(Guid id)
        {
            var dto = ToCreateDto();
            return new UpdateGoodsTypeDto
            {
                Id = id,
                Code = dto.Code,
                Name = dto.Name,
                ImageUrl = dto.ImageUrl,
                TaxCategoryCode = dto.TaxCategoryCode,
                TaxCategoryName = dto.TaxCategoryName,
                InvoiceGoodsShortName = dto.InvoiceGoodsShortName,
                DefaultTaxRate = dto.DefaultTaxRate,
                IsTaxExempt = dto.IsTaxExempt,
                TaxPolicyBasis = dto.TaxPolicyBasis,
                Sort = dto.Sort,
                Remark = dto.Remark,
                Status = dto.Status
            };
        }

        public bool Matches(Domain.Entities.Goods.GoodsType goodsType)
        {
            return goodsType.Name == Name
                   && goodsType.ImageUrl == ImageUrl
                   && goodsType.TaxCategoryCode == TaxCategoryCode
                   && goodsType.TaxCategoryName == TaxCategoryName
                   && goodsType.InvoiceGoodsShortName == InvoiceGoodsShortName
                   && goodsType.DefaultTaxRate == DefaultTaxRate
                   && goodsType.IsTaxExempt == IsTaxExempt
                   && goodsType.TaxPolicyBasis == TaxPolicyBasis
                   && goodsType.Sort == Sort
                   && goodsType.Remark == Remark
                   && goodsType.Status == Status.Enable
                   && goodsType.ParentId is null;
        }
    }

    private sealed record WareSeed(
        string Code,
        string Name,
        string ContactName,
        string ContactPhone,
        string Address,
        int Sort,
        string Remark)
    {
        public CreateWareDto ToCreateDto() => new()
        {
            Code = Code,
            Name = Name,
            ContactName = ContactName,
            ContactPhone = ContactPhone,
            Address = Address,
            Sort = Sort,
            Remark = Remark,
            Status = Status.Enable
        };

        public UpdateWareDto ToUpdateDto(Guid id) => new()
        {
            Id = id,
            Code = Code,
            Name = Name,
            ContactName = ContactName,
            ContactPhone = ContactPhone,
            Address = Address,
            Sort = Sort,
            Remark = Remark,
            Status = Status.Enable
        };

        public bool Matches(Domain.Entities.Storage.Ware ware)
        {
            return ware.Name == Name
                   && ware.ContactName == ContactName
                   && ware.ContactPhone == ContactPhone
                   && ware.Address == Address
                   && ware.Sort == Sort
                   && ware.Remark == Remark
                   && ware.Status == Status.Enable;
        }
    }

    private sealed record DemoAuditUser(Guid Id, string Username);
}
