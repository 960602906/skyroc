using Application.DTOs.Purchases;
using Application.DTOs.Storage;
using Application.interfaces;
using Application.Mappers;
using Application.Services;
using Application.Validator;
using AutoMapper;
using Domain.Entities;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Constants;
using Xunit;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace SkyRoc.Tests.Purchases;

public class PurchaseBaseDataServiceTests
{
    private static readonly Guid CurrentUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task Create_supplier_should_persist_financial_and_contact_details()
    {
        await using var context = CreateDbContext();

        var result = await CreateSupplierService(context).CreateAsync(new CreateSupplierDto
        {
            Name = "绿色蔬菜供应商",
            Code = "GREEN_SUPPLIER",
            ContactName = "李经理",
            ContactPhone = "13800001111",
            BankName = "示例银行",
            BankAccount = "622200001111",
            TaxNo = "91310000SUPPLIER"
        });

        var savedSupplier = await context.Suppliers.SingleAsync(x => x.Id == result.Id);
        Assert.Equal("李经理", savedSupplier.ContactName);
        Assert.Equal("示例银行", savedSupplier.BankName);
        Assert.Equal("622200001111", savedSupplier.BankAccount);
        Assert.Equal("91310000SUPPLIER", savedSupplier.TaxNo);
        Assert.Equal(CurrentUserId, savedSupplier.CreateBy);
    }

    [Fact]
    public async Task Create_purchaser_should_link_system_user_and_department()
    {
        await using var context = CreateDbContext();
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "采购部",
            Code = "PURCHASE"
        };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "purchaser.user",
            NickName = "采购用户",
            Email = "purchaser@example.com",
            PasswordHash = "hash",
            Gender = GenderType.Male,
            DepartmentId = department.Id
        };
        await context.Departments.AddAsync(department);
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        var result = await CreatePurchaserService(context).CreateAsync(new CreatePurchaserDto
        {
            Name = "张采购",
            Code = "PURCHASER_ZHANG",
            Phone = "13900001111",
            UserId = user.Id,
            DepartmentId = department.Id
        });

        var savedPurchaser = await context.Purchasers.SingleAsync(x => x.Id == result.Id);
        Assert.Equal(user.Id, savedPurchaser.UserId);
        Assert.Equal(department.Id, savedPurchaser.DepartmentId);
        Assert.Equal(CurrentUserId, savedPurchaser.CreateBy);
    }

    [Fact]
    public async Task Toggle_ware_status_should_persist_disabled_state()
    {
        await using var context = CreateDbContext();
        var service = CreateWareService(context);
        var ware = await service.CreateAsync(new CreateWareDto
        {
            Name = "中心仓",
            Code = "CENTRAL_WARE",
            ContactName = "仓库管理员",
            Address = "上海市宝山区",
            Sort = 1
        });

        var result = await service.ToggleStatusAsync(ware.Id, Status.Disable);

        Assert.Equal(Status.Disable, result.Status);
        var savedWare = await context.Wares.SingleAsync(x => x.Id == ware.Id);
        Assert.Equal(Status.Disable, savedWare.Status);
        Assert.Equal(CurrentUserId, savedWare.UpdateBy);
    }

    [Fact]
    public async Task Create_purchase_rule_should_persist_all_references_and_distinct_relations()
    {
        await using var context = CreateDbContext();
        var data = await SeedPurchaseRuleReferencesAsync(context, 2, 2);
        var service = CreatePurchaseRuleService(context);

        var created = await service.CreateAsync(new CreatePurchaseRuleDto
        {
            Name = "学校蔬菜直供规则",
            Code = "SCHOOL_VEGETABLE_DIRECT",
            SupplierId = data.SupplierId,
            PurchaserId = data.PurchaserId,
            WareId = data.WareId,
            GoodsTypeId = data.GoodsTypeId,
            PurchasePattern = 1,
            GoodsIds = [data.GoodsIds[0], data.GoodsIds[0], data.GoodsIds[1], Guid.Empty],
            CustomerIds = [data.CustomerIds[0], data.CustomerIds[0], data.CustomerIds[1], Guid.Empty]
        });
        var result = await service.GetByIdAsync(created.Id);

        Assert.Equal("绿色蔬菜供应商", result.SupplierName);
        Assert.Equal("张采购", result.PurchaserName);
        Assert.Equal("中心仓", result.WareName);
        Assert.Equal("蔬菜", result.GoodsTypeName);
        Assert.Equal(data.GoodsIds.OrderBy(x => x), result.GoodsIds!.OrderBy(x => x));
        Assert.Equal(data.CustomerIds.OrderBy(x => x), result.CustomerIds!.OrderBy(x => x));
        Assert.Equal(CurrentUserId, (await context.PurchaseRules.SingleAsync()).CreateBy);
    }

    [Fact]
    public async Task Update_purchase_rule_should_replace_goods_and_customer_relations()
    {
        await using var context = CreateDbContext();
        var data = await SeedPurchaseRuleReferencesAsync(context, 2, 2);
        var service = CreatePurchaseRuleService(context);
        var created = await service.CreateAsync(new CreatePurchaseRuleDto
        {
            Name = "学校采购规则",
            Code = "SCHOOL_PURCHASE",
            SupplierId = data.SupplierId,
            PurchaserId = data.PurchaserId,
            WareId = data.WareId,
            GoodsTypeId = data.GoodsTypeId,
            PurchasePattern = 1,
            GoodsIds = [data.GoodsIds[0]],
            CustomerIds = [data.CustomerIds[0]]
        });

        await service.UpdateAsync(created.Id, new UpdatePurchaseRuleDto
        {
            Id = created.Id,
            Name = "学校采购规则",
            Code = "SCHOOL_PURCHASE",
            SupplierId = data.SupplierId,
            PurchaserId = data.PurchaserId,
            WareId = data.WareId,
            GoodsTypeId = data.GoodsTypeId,
            PurchasePattern = 2,
            GoodsIds = [data.GoodsIds[1]],
            CustomerIds = [data.CustomerIds[1]]
        });

        var goodsRelation = await context.PurchaseRuleGoods.SingleAsync(x => x.PurchaseRuleId == created.Id);
        var customerRelation = await context.PurchaseRuleCustomers.SingleAsync(x => x.PurchaseRuleId == created.Id);
        Assert.Equal(data.GoodsIds[1], goodsRelation.GoodsId);
        Assert.Equal(data.CustomerIds[1], customerRelation.CustomerId);
        Assert.Equal(2, (await context.PurchaseRules.SingleAsync()).PurchasePattern);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static SupplierService CreateSupplierService(ApplicationDbContext context)
    {
        return new SupplierService(
            new SupplierRepository(context),
            new UnitOfWork(context),
            NullLogger<SupplierService>.Instance,
            CreateMapper(),
            new FakeCurrentUserService(),
            new CreateSupplierValidator(),
            new UpdateSupplierValidator());
    }

    private static PurchaserService CreatePurchaserService(ApplicationDbContext context)
    {
        return new PurchaserService(
            new PurchaserRepository(context),
            new UserRepository(context),
            new DepartmentRepository(context),
            new UnitOfWork(context),
            NullLogger<PurchaserService>.Instance,
            CreateMapper(),
            new FakeCurrentUserService(),
            new CreatePurchaserValidator(),
            new UpdatePurchaserValidator());
    }

    private static WareService CreateWareService(ApplicationDbContext context)
    {
        return new WareService(
            new WareRepository(context),
            new UnitOfWork(context),
            NullLogger<WareService>.Instance,
            CreateMapper(),
            new FakeCurrentUserService(),
            new CreateWareValidator(),
            new UpdateWareValidator());
    }

    private static PurchaseRuleService CreatePurchaseRuleService(ApplicationDbContext context)
    {
        return new PurchaseRuleService(
            new PurchaseRuleRepository(context),
            new SupplierRepository(context),
            new PurchaserRepository(context),
            new WareRepository(context),
            new GoodsTypeRepository(context),
            new GoodsRepository(context),
            new CustomerRepository(context),
            new UnitOfWork(context),
            NullLogger<PurchaseRuleService>.Instance,
            CreateMapper(),
            new FakeCurrentUserService(),
            new CreatePurchaseRuleValidator(),
            new UpdatePurchaseRuleValidator());
    }

    private static IMapper CreateMapper()
    {
        return new MapperConfiguration(cfg => cfg.AddProfile<BaseDataMappingProfile>()).CreateMapper();
    }

    private static async Task<PurchaseRuleSeed> SeedPurchaseRuleReferencesAsync(
        ApplicationDbContext context,
        int goodsCount,
        int customerCount)
    {
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "绿色蔬菜供应商", Code = "GREEN_SUPPLIER" };
        var purchaser = new Purchaser { Id = Guid.NewGuid(), Name = "张采购", Code = "PURCHASER_ZHANG" };
        var ware = new Ware { Id = Guid.NewGuid(), Name = "中心仓", Code = "CENTRAL_WARE" };
        var goodsType = new GoodsType { Id = Guid.NewGuid(), Name = "蔬菜", Code = "VEGETABLE" };
        var goods = Enumerable.Range(1, goodsCount)
            .Select(index => new GoodsEntity
            {
                Id = Guid.NewGuid(),
                Name = $"蔬菜{index}",
                Code = $"VEGETABLE_{index}",
                GoodsTypeId = goodsType.Id
            })
            .ToList();
        var customers = Enumerable.Range(1, customerCount)
            .Select(index => new Customer
            {
                Id = Guid.NewGuid(),
                Name = $"学校客户{index}",
                Code = $"SCHOOL_CUSTOMER_{index}"
            })
            .ToList();

        await context.Suppliers.AddAsync(supplier);
        await context.Purchasers.AddAsync(purchaser);
        await context.Wares.AddAsync(ware);
        await context.GoodsTypes.AddAsync(goodsType);
        await context.Goods.AddRangeAsync(goods);
        await context.Customers.AddRangeAsync(customers);
        await context.SaveChangesAsync();

        return new PurchaseRuleSeed(
            supplier.Id,
            purchaser.Id,
            ware.Id,
            goodsType.Id,
            goods.Select(x => x.Id).ToList(),
            customers.Select(x => x.Id).ToList());
    }

    private sealed record PurchaseRuleSeed(
        Guid SupplierId,
        Guid PurchaserId,
        Guid WareId,
        Guid GoodsTypeId,
        List<Guid> GoodsIds,
        List<Guid> CustomerIds);

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
