using Application.DTOs.Customers;
using Application.interfaces;
using Application.Mappers;
using Application.QueryParameters;
using Application.Services;
using Application.Validator;
using AutoMapper;
using Domain.Entities.Customers;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Constants;
using Xunit;

namespace SkyRoc.Tests.Customers;

public class CustomerServiceTests
{
    private static readonly Guid CurrentUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task Create_customer_should_persist_company_and_tag_relations()
    {
        await using var context = CreateDbContext();
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = "北京示例学校后勤服务有限公司",
            Code = "SCHOOL_COMPANY"
        };
        var schoolTag = new CustomerTag
        {
            Id = Guid.NewGuid(),
            Name = "学校客户",
            Code = "SCHOOL",
            Sort = 1
        };

        await context.Companies.AddAsync(company);
        await context.CustomerTags.AddAsync(schoolTag);
        await context.SaveChangesAsync();

        var service = CreateCustomerService(context);

        var result = await service.CreateAsync(new CreateCustomerDto
        {
            Name = "北京示例学校后勤服务有限公司",
            Code = "CUST_SCHOOL_001",
            CompanyId = company.Id,
            ContactName = "王老师",
            TagIds = [schoolTag.Id]
        });

        Assert.Equal("北京示例学校后勤服务有限公司", result.Name);
        Assert.Equal(company.Id, result.CompanyId);
        Assert.Equal("北京示例学校后勤服务有限公司", result.CompanyName);
        var tagId = Assert.Single(result.TagIds!);
        Assert.Equal(schoolTag.Id, tagId);

        var savedCustomer = await context.Customers
            .Include(x => x.TagRelations)
            .SingleAsync(x => x.Id == result.Id);

        Assert.Single(savedCustomer.TagRelations);
        Assert.Equal(CurrentUserId, savedCustomer.CreateBy);
        Assert.Equal("test-user", savedCustomer.CreateName);
    }

    [Fact]
    public async Task Customer_related_tables_should_have_seedable_school_data()
    {
        await using var context = CreateDbContext();
        var data = await SeedSchoolCustomerTablesAsync(context);

        Assert.Equal(2, await context.Companies.CountAsync());
        Assert.Equal(2, await context.Customers.CountAsync());
        Assert.Equal(3, await context.CustomerTags.CountAsync());
        Assert.Equal(3, await context.CustomerTagRelations.CountAsync());
        Assert.Equal(2, await context.CustomerSubAccounts.CountAsync());

        var customer = await context.Customers
            .Include(x => x.Company)
            .Include(x => x.TagRelations)
            .SingleAsync(x => x.Id == data.MiddleSchoolCustomerId);

        Assert.Equal("上海示例中学餐饮管理有限公司", customer.Name);
        Assert.Equal("91310110MA01MIDDLE1", customer.UnifiedSocialCreditCode);
        Assert.Equal("上海示例中学集团", customer.Company!.Name);
        Assert.Contains(customer.TagRelations, x => x.CustomerTagId == data.SchoolTagId);
        Assert.Contains(customer.TagRelations, x => x.CustomerTagId == data.CanteenTagId);

        var subAccount = await context.CustomerSubAccounts
            .Include(x => x.Customer)
            .SingleAsync(x => x.Username == "middle_school_buyer");

        Assert.Equal(data.MiddleSchoolCustomerId, subAccount.CustomerId);
        Assert.Equal("上海示例中学餐饮管理有限公司", subAccount.Customer!.Name);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static CustomerService CreateCustomerService(ApplicationDbContext context)
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<BaseDataMappingProfile>()).CreateMapper();
        var customerRepository = new CustomerRepository(context);

        return new CustomerService(
            customerRepository,
            new CompanyRepository(context),
            new QuotationRepository(context),
            new WareRepository(context),
            new CustomerTagRepository(context),
            new UnitOfWork(context),
            NullLogger<CustomerService>.Instance,
            mapper,
            new FakeCurrentUserService(),
            new CreateCustomerValidator(),
            new UpdateCustomerValidator());
    }

    private static async Task<SeedResult> SeedSchoolCustomerTablesAsync(ApplicationDbContext context)
    {
        var company1 = new Company
        {
            Id = Guid.NewGuid(),
            Name = "上海示例中学集团",
            Code = "SH_SCHOOL_GROUP",
            ContactName = "周主任",
            ContactPhone = "021-66668888",
            Address = "上海市杨浦区大学路88号",
            Status = Status.Enable
        };
        var company2 = new Company
        {
            Id = Guid.NewGuid(),
            Name = "杭州示例小学教育服务有限公司",
            Code = "HZ_PRIMARY_SERVICE",
            ContactName = "陈老师",
            ContactPhone = "0571-88889999",
            Address = "杭州市西湖区文三路66号",
            Status = Status.Enable
        };
        var schoolTag = new CustomerTag
        {
            Id = Guid.NewGuid(),
            Name = "学校客户",
            Code = "SCHOOL",
            Sort = 1,
            Status = Status.Enable
        };
        var canteenTag = new CustomerTag
        {
            Id = Guid.NewGuid(),
            Name = "食堂配送",
            Code = "CANTEEN",
            ParentId = schoolTag.Id,
            Sort = 1,
            Status = Status.Enable
        };
        var kindergartenTag = new CustomerTag
        {
            Id = Guid.NewGuid(),
            Name = "幼儿园",
            Code = "KINDERGARTEN",
            ParentId = schoolTag.Id,
            Sort = 2,
            Status = Status.Enable
        };
        var customer1 = new Customer
        {
            Id = Guid.NewGuid(),
            Name = "上海示例中学餐饮管理有限公司",
            Code = "CUST_SH_MIDDLE",
            CompanyId = company1.Id,
            UnifiedSocialCreditCode = "91310110MA01MIDDLE1",
            LegalRepresentative = "赵校长",
            RegisteredCapital = "3000万人民币",
            EstablishDate = new DateTime(2015, 8, 20),
            RegistrationStatus = "存续",
            RegistrationAuthority = "上海市杨浦区市场监督管理局",
            RegisteredAddress = "上海市杨浦区大学路88号",
            BusinessScope = "学校食堂餐饮管理、农副产品配送。",
            InvoiceTitle = "上海示例中学餐饮管理有限公司",
            TaxpayerIdentificationNumber = "91310110MA01MIDDLE1",
            InvoicePhone = "021-66668888",
            ContactName = "周主任",
            ContactPhone = "021-66668888",
            Address = "上海市杨浦区大学路88号",
            Status = Status.Enable
        };
        var customer2 = new Customer
        {
            Id = Guid.NewGuid(),
            Name = "杭州示例小学教育服务有限公司",
            Code = "CUST_HZ_PRIMARY",
            CompanyId = company2.Id,
            UnifiedSocialCreditCode = "91330106MA01PRIMARY",
            LegalRepresentative = "陈校长",
            RegisteredCapital = "1200万人民币",
            EstablishDate = new DateTime(2018, 3, 12),
            RegistrationStatus = "存续",
            RegistrationAuthority = "杭州市西湖区市场监督管理局",
            RegisteredAddress = "杭州市西湖区文三路66号",
            BusinessScope = "教育后勤服务、校园物资配送。",
            InvoiceTitle = "杭州示例小学教育服务有限公司",
            TaxpayerIdentificationNumber = "91330106MA01PRIMARY",
            InvoicePhone = "0571-88889999",
            ContactName = "陈老师",
            ContactPhone = "0571-88889999",
            Address = "杭州市西湖区文三路66号",
            Status = Status.Enable
        };

        await context.Companies.AddRangeAsync(company1, company2);
        await context.CustomerTags.AddRangeAsync(schoolTag, canteenTag, kindergartenTag);
        await context.Customers.AddRangeAsync(customer1, customer2);
        await context.CustomerTagRelations.AddRangeAsync(
            new CustomerTagRelation
            {
                CustomerId = customer1.Id,
                CustomerTagId = schoolTag.Id
            },
            new CustomerTagRelation
            {
                CustomerId = customer1.Id,
                CustomerTagId = canteenTag.Id
            },
            new CustomerTagRelation
            {
                CustomerId = customer2.Id,
                CustomerTagId = schoolTag.Id
            });
        await context.CustomerSubAccounts.AddRangeAsync(
            new CustomerSubAccount
            {
                Id = Guid.NewGuid(),
                CompanyId = company1.Id,
                CustomerId = customer1.Id,
                Username = "middle_school_buyer",
                NickName = "中学采购员",
                Phone = "13900001111",
                Email = "buyer@middle.example.edu.cn",
                Status = Status.Enable
            },
            new CustomerSubAccount
            {
                Id = Guid.NewGuid(),
                CompanyId = company2.Id,
                CustomerId = customer2.Id,
                Username = "primary_school_buyer",
                NickName = "小学采购员",
                Phone = "13900002222",
                Email = "buyer@primary.example.edu.cn",
                Status = Status.Enable
            });
        await context.SaveChangesAsync();

        return new SeedResult(customer1.Id, schoolTag.Id, canteenTag.Id);
    }

    private sealed record SeedResult(
        Guid MiddleSchoolCustomerId,
        Guid SchoolTagId,
        Guid CanteenTagId);

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public Guid? GetUserId() => CurrentUserId;

        public string? GetUserName() => "test-user";

        public string? GetEmail() => "test-user@example.com";

        public string? GetRole() => "test-role";

        public IReadOnlyList<string> GetRoles() => ["admin"];

        public bool HasClaim(string claimType, string claimValue) => false;
    }
}
