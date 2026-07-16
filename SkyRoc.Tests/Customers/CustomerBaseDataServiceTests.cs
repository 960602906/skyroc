using Application.DTOs.Customers;
using Application.Exceptions;
using Application.interfaces;
using Application.Mappers;
using Application.Services;
using Application.Validator;
using AutoMapper;
using Domain.Entities.Customers;
using Domain.Entities.Storage;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace SkyRoc.Tests.Customers;

public class CustomerBaseDataServiceTests
{
    private static readonly Guid CurrentUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task Create_company_should_persist_contact_details_and_audit_fields()
    {
        await using var context = CreateDbContext();

        var result = await CreateCompanyService(context).CreateAsync(new CreateCompanyDto
        {
            Name = "华东学校集团",
            Code = "EAST_SCHOOL_GROUP",
            ContactName = "张老师",
            ContactPhone = "021-88886666",
            Address = "上海市杨浦区"
        });

        var savedCompany = await context.Companies.SingleAsync(x => x.Id == result.Id);
        Assert.Equal("张老师", savedCompany.ContactName);
        Assert.Equal("021-88886666", savedCompany.ContactPhone);
        Assert.Equal("上海市杨浦区", savedCompany.Address);
        Assert.Equal(CurrentUserId, savedCompany.CreateBy);
    }

    [Fact]
    public async Task Update_customer_should_replace_tag_relations_and_default_ware()
    {
        await using var context = CreateDbContext();
        var company = new Company { Id = Guid.NewGuid(), Name = "学校集团", Code = "SCHOOL_GROUP" };
        var ware = new Ware { Id = Guid.NewGuid(), Name = "学校仓", Code = "SCHOOL_WARE" };
        var oldTag = new CustomerTag { Id = Guid.NewGuid(), Name = "旧标签", Code = "OLD_TAG" };
        var newTag = new CustomerTag { Id = Guid.NewGuid(), Name = "新标签", Code = "NEW_TAG" };
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = "第一客户",
            Code = "CUSTOMER_001",
            CompanyId = company.Id
        };
        await context.Companies.AddAsync(company);
        await context.Wares.AddAsync(ware);
        await context.CustomerTags.AddRangeAsync(oldTag, newTag);
        await context.Customers.AddAsync(customer);
        await context.CustomerTagRelations.AddAsync(new CustomerTagRelation
        {
            CustomerId = customer.Id,
            CustomerTagId = oldTag.Id
        });
        await context.SaveChangesAsync();

        await CreateCustomerService(context).UpdateAsync(customer.Id, new UpdateCustomerDto
        {
            Id = customer.Id,
            Name = customer.Name,
            Code = customer.Code,
            CompanyId = company.Id,
            DefaultWareId = ware.Id,
            TagIds = [newTag.Id, newTag.Id, Guid.Empty]
        });

        var savedCustomer = await context.Customers.SingleAsync(x => x.Id == customer.Id);
        var relation = await context.CustomerTagRelations.SingleAsync(x => x.CustomerId == customer.Id);
        Assert.Equal(ware.Id, savedCustomer.DefaultWareId);
        Assert.Equal(newTag.Id, relation.CustomerTagId);
        Assert.Equal(CurrentUserId, savedCustomer.UpdateBy);
    }

    [Fact]
    public async Task Get_customer_tag_tree_should_nest_child_tag()
    {
        await using var context = CreateDbContext();
        var parent = new CustomerTag
        {
            Id = Guid.NewGuid(),
            Name = "学校客户",
            Code = "SCHOOL",
            Sort = 1
        };
        var child = new CustomerTag
        {
            Id = Guid.NewGuid(),
            Name = "食堂配送",
            Code = "CANTEEN",
            ParentId = parent.Id,
            Sort = 1
        };
        await context.CustomerTags.AddRangeAsync(parent, child);
        await context.SaveChangesAsync();

        var result = await CreateCustomerTagService(context).GetTreeAsync();

        var root = Assert.Single(result);
        Assert.Equal(parent.Id, root.Id);
        Assert.Equal(child.Id, Assert.Single(root.Children!).Id);
    }

    [Fact]
    public async Task Delete_customer_tag_should_fail_when_referenced_by_customer()
    {
        await using var context = CreateDbContext();
        var tag = new CustomerTag { Id = Guid.NewGuid(), Name = "重点客户", Code = "KEY_CUSTOMER" };
        var customer = new Customer { Id = Guid.NewGuid(), Name = "重点学校", Code = "KEY_SCHOOL" };
        await context.CustomerTags.AddAsync(tag);
        await context.Customers.AddAsync(customer);
        await context.CustomerTagRelations.AddAsync(new CustomerTagRelation
        {
            CustomerId = customer.Id,
            CustomerTagId = tag.Id
        });
        await context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => CreateCustomerTagService(context).DeleteAsync(tag.Id));

        Assert.Equal("客户标签已被客户使用，不能删除", exception.Message);
        Assert.True(await context.CustomerTags.AnyAsync(x => x.Id == tag.Id));
    }

    [Fact]
    public async Task Create_sub_account_should_link_company_and_authorized_customer()
    {
        await using var context = CreateDbContext();
        var company = new Company { Id = Guid.NewGuid(), Name = "学校集团", Code = "SCHOOL_GROUP" };
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = "学校食堂",
            Code = "SCHOOL_CANTEEN",
            CompanyId = company.Id
        };
        await context.Companies.AddAsync(company);
        await context.Customers.AddAsync(customer);
        await context.SaveChangesAsync();
        var service = CreateCustomerSubAccountService(context);

        var created = await service.CreateAsync(new CreateCustomerSubAccountDto
        {
            CompanyId = company.Id,
            CustomerId = customer.Id,
            Username = "school_buyer",
            NickName = "学校采购员",
            Email = "buyer@school.example"
        });
        var result = await service.GetByIdAsync(created.Id);

        Assert.Equal(company.Id, result.CompanyId);
        Assert.Equal(company.Name, result.CompanyName);
        Assert.Equal(customer.Id, result.CustomerId);
        Assert.Equal(customer.Name, result.CustomerName);
        Assert.Equal(CurrentUserId, (await context.CustomerSubAccounts.SingleAsync()).CreateBy);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static CompanyService CreateCompanyService(ApplicationDbContext context)
    {
        return new CompanyService(
            new CompanyRepository(context),
            new UnitOfWork(context),
            NullLogger<CompanyService>.Instance,
            CreateMapper(),
            new FakeCurrentUserService(),
            new CreateCompanyValidator(),
            new UpdateCompanyValidator());
    }

    private static CustomerService CreateCustomerService(ApplicationDbContext context)
    {
        return new CustomerService(
            new CustomerRepository(context),
            new CompanyRepository(context),
            new QuotationRepository(context),
            new WareRepository(context),
            new CustomerTagRepository(context),
            new UnitOfWork(context),
            NullLogger<CustomerService>.Instance,
            CreateMapper(),
            new FakeCurrentUserService(),
            new CreateCustomerValidator(),
            new UpdateCustomerValidator());
    }

    private static CustomerTagService CreateCustomerTagService(ApplicationDbContext context)
    {
        return new CustomerTagService(
            new CustomerTagRepository(context),
            new UnitOfWork(context),
            NullLogger<CustomerTagService>.Instance,
            CreateMapper(),
            new FakeCurrentUserService(),
            new CreateCustomerTagValidator(),
            new UpdateCustomerTagValidator());
    }

    private static CustomerSubAccountService CreateCustomerSubAccountService(ApplicationDbContext context)
    {
        return new CustomerSubAccountService(
            new CustomerSubAccountRepository(context),
            new CompanyRepository(context),
            new CustomerRepository(context),
            new UnitOfWork(context),
            NullLogger<CustomerSubAccountService>.Instance,
            CreateMapper(),
            new FakeCurrentUserService(),
            new CreateCustomerSubAccountValidator(),
            new UpdateCustomerSubAccountValidator());
    }

    private static IMapper CreateMapper()
    {
        return new MapperConfiguration(cfg => cfg.AddProfile<BaseDataMappingProfile>()).CreateMapper();
    }

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
