using Application.DTOs.Pricing;
using Application.Exceptions;
using Application.Interfaces;
using Application.Mappers;
using Application.Services;
using Application.Validator;
using AutoMapper;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Pricing;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace SkyRoc.Tests.Pricing;

public class PricingServiceTests
{
    private static readonly Guid CurrentUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task Toggle_quotation_audit_should_persist_audited_state()
    {
        await using var context = CreateDbContext();
        var data = await SeedPricingAsync(context);
        var service = CreateQuotationService(context);

        var audited = await service.ToggleAuditAsync(data.QuotationId, true);
        var unaudited = await service.ToggleAuditAsync(data.QuotationId, false);

        Assert.True(audited.IsAudited);
        Assert.False(unaudited.IsAudited);
        var savedQuotation = await context.Quotations.SingleAsync(x => x.Id == data.QuotationId);
        Assert.False(savedQuotation.IsAudited);
        Assert.Equal(CurrentUserId, savedQuotation.UpdateBy);
    }

    [Fact]
    public async Task Create_quotation_goods_should_persist_price_and_sale_state()
    {
        await using var context = CreateDbContext();
        var data = await SeedPricingAsync(context);

        var result = await CreateQuotationGoodsService(context).CreateAsync(new CreateQuotationGoodsDto
        {
            QuotationId = data.QuotationId,
            GoodsId = data.GoodsId,
            GoodsUnitId = data.GoodsUnitId,
            UnitPrice = 12.80m,
            MinOrderQuantity = 5m,
            IsOnSale = false
        });

        var savedDetail = await context.QuotationGoods.SingleAsync(x => x.Id == result.Id);
        Assert.Equal(12.80m, savedDetail.UnitPrice);
        Assert.Equal(5m, savedDetail.MinOrderQuantity);
        Assert.False(savedDetail.IsOnSale);
        Assert.Equal(CurrentUserId, savedDetail.CreateBy);
    }

    [Fact]
    public async Task Create_quotation_goods_should_reject_unit_from_another_goods()
    {
        await using var context = CreateDbContext();
        var data = await SeedPricingAsync(context);
        var otherGoods = await AddGoodsWithUnitAsync(context, "黄瓜", "CUCUMBER", "根");

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            CreateQuotationGoodsService(context).CreateAsync(new CreateQuotationGoodsDto
            {
                QuotationId = data.QuotationId,
                GoodsId = data.GoodsId,
                GoodsUnitId = otherGoods.GoodsUnitId,
                UnitPrice = 8m
            }));

        Assert.Equal("报价单位不存在或不属于该商品", exception.Message);
        Assert.Empty(context.QuotationGoods);
    }

    [Fact]
    public async Task Create_customer_protocol_should_bind_distinct_customers()
    {
        await using var context = CreateDbContext();
        var data = await SeedPricingAsync(context);
        var secondCustomer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = "第二客户",
            Code = "CUSTOMER_002"
        };
        await context.Customers.AddAsync(secondCustomer);
        await context.SaveChangesAsync();

        var result = await CreateCustomerProtocolService(context).CreateAsync(new CreateCustomerProtocolDto
        {
            Name = "学校协议价",
            Code = "SCHOOL_PROTOCOL",
            QuotationId = data.QuotationId,
            EffectiveStart = new DateTime(2026, 7, 1),
            EffectiveEnd = new DateTime(2026, 12, 31),
            CustomerIds = [data.CustomerId, data.CustomerId, secondCustomer.Id, Guid.Empty]
        });

        var customerIds = await context.CustomerProtocolCustomers
            .Where(x => x.CustomerProtocolId == result.Id)
            .Select(x => x.CustomerId)
            .OrderBy(x => x)
            .ToListAsync();
        Assert.Equal(2, customerIds.Count);
        Assert.Contains(data.CustomerId, customerIds);
        Assert.Contains(secondCustomer.Id, customerIds);
    }

    [Fact]
    public async Task Update_customer_protocol_should_replace_customer_relations()
    {
        await using var context = CreateDbContext();
        var data = await SeedPricingAsync(context);
        var secondCustomer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = "第二客户",
            Code = "CUSTOMER_002"
        };
        await context.Customers.AddAsync(secondCustomer);
        await context.SaveChangesAsync();
        var service = CreateCustomerProtocolService(context);
        var created = await service.CreateAsync(new CreateCustomerProtocolDto
        {
            Name = "学校协议价",
            Code = "SCHOOL_PROTOCOL",
            EffectiveStart = new DateTime(2026, 7, 1),
            CustomerIds = [data.CustomerId]
        });

        await service.UpdateAsync(created.Id, new UpdateCustomerProtocolDto
        {
            Id = created.Id,
            Name = "学校协议价",
            Code = "SCHOOL_PROTOCOL",
            EffectiveStart = new DateTime(2026, 7, 1),
            CustomerIds = [secondCustomer.Id]
        });

        var relation = await context.CustomerProtocolCustomers
            .SingleAsync(x => x.CustomerProtocolId == created.Id);
        Assert.Equal(secondCustomer.Id, relation.CustomerId);
    }

    [Fact]
    public async Task Create_customer_protocol_goods_should_persist_protocol_price()
    {
        await using var context = CreateDbContext();
        var data = await SeedPricingAsync(context, includeProtocol: true);

        var result = await CreateCustomerProtocolGoodsService(context).CreateAsync(
            new CreateCustomerProtocolGoodsDto
            {
                CustomerProtocolId = data.CustomerProtocolId!.Value,
                GoodsId = data.GoodsId,
                GoodsUnitId = data.GoodsUnitId,
                ProtocolPrice = 10.50m,
                MinOrderQuantity = 3m
            });

        var savedDetail = await context.CustomerProtocolGoods.SingleAsync(x => x.Id == result.Id);
        Assert.Equal(10.50m, savedDetail.ProtocolPrice);
        Assert.Equal(3m, savedDetail.MinOrderQuantity);
        Assert.Equal(CurrentUserId, savedDetail.CreateBy);
    }

    [Fact]
    public async Task Create_customer_protocol_goods_should_reject_duplicate_detail()
    {
        await using var context = CreateDbContext();
        var data = await SeedPricingAsync(context, includeProtocol: true);
        var service = CreateCustomerProtocolGoodsService(context);
        var dto = new CreateCustomerProtocolGoodsDto
        {
            CustomerProtocolId = data.CustomerProtocolId!.Value,
            GoodsId = data.GoodsId,
            GoodsUnitId = data.GoodsUnitId,
            ProtocolPrice = 10.50m
        };
        await service.CreateAsync(dto);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.CreateAsync(dto));

        Assert.Equal("客户协议价商品明细已经存在", exception.Message);
        Assert.Single(context.CustomerProtocolGoods);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static QuotationService CreateQuotationService(ApplicationDbContext context)
    {
        return new QuotationService(
            new QuotationRepository(context),
            new CustomerRepository(context),
            new UnitOfWork(context),
            NullLogger<QuotationService>.Instance,
            CreateMapper(),
            new FakeCurrentUserService(),
            new CreateQuotationValidator(),
            new UpdateQuotationValidator());
    }

    private static QuotationGoodsService CreateQuotationGoodsService(ApplicationDbContext context)
    {
        return new QuotationGoodsService(
            new QuotationGoodsRepository(context),
            new QuotationRepository(context),
            new GoodsRepository(context),
            new GoodsUnitRepository(context),
            new UnitOfWork(context),
            NullLogger<QuotationGoodsService>.Instance,
            CreateMapper(),
            new FakeCurrentUserService(),
            new CreateQuotationGoodsValidator(),
            new UpdateQuotationGoodsValidator());
    }

    private static CustomerProtocolService CreateCustomerProtocolService(ApplicationDbContext context)
    {
        return new CustomerProtocolService(
            new CustomerProtocolRepository(context),
            new QuotationRepository(context),
            new CustomerRepository(context),
            new UnitOfWork(context),
            NullLogger<CustomerProtocolService>.Instance,
            CreateMapper(),
            new FakeCurrentUserService(),
            new CreateCustomerProtocolValidator(),
            new UpdateCustomerProtocolValidator());
    }

    private static CustomerProtocolGoodsService CreateCustomerProtocolGoodsService(ApplicationDbContext context)
    {
        return new CustomerProtocolGoodsService(
            new CustomerProtocolGoodsRepository(context),
            new CustomerProtocolRepository(context),
            new GoodsRepository(context),
            new GoodsUnitRepository(context),
            new UnitOfWork(context),
            NullLogger<CustomerProtocolGoodsService>.Instance,
            CreateMapper(),
            new FakeCurrentUserService(),
            new CreateCustomerProtocolGoodsValidator(),
            new UpdateCustomerProtocolGoodsValidator());
    }

    private static IMapper CreateMapper()
    {
        return new MapperConfiguration(cfg => cfg.AddProfile<BaseDataMappingProfile>()).CreateMapper();
    }

    private static async Task<PricingSeed> SeedPricingAsync(
        ApplicationDbContext context,
        bool includeProtocol = false)
    {
        var goodsType = new GoodsType
        {
            Id = Guid.NewGuid(),
            Name = "蔬菜",
            Code = "VEGETABLE"
        };
        var goods = new GoodsEntity
        {
            Id = Guid.NewGuid(),
            Name = "番茄",
            Code = "TOMATO",
            GoodsTypeId = goodsType.Id
        };
        var unit = new GoodsUnit
        {
            Id = Guid.NewGuid(),
            GoodsId = goods.Id,
            Name = "千克",
            Code = "KG",
            ConversionRate = 1m,
            IsBaseUnit = true
        };
        goods.BaseUnitId = unit.Id;
        var quotation = new Quotation
        {
            Id = Guid.NewGuid(),
            Name = "标准报价",
            Code = "STANDARD_QUOTATION"
        };
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = "学校客户",
            Code = "CUSTOMER_001"
        };

        await context.Set<GoodsType>().AddAsync(goodsType);
        await context.Goods.AddAsync(goods);
        await context.Set<GoodsUnit>().AddAsync(unit);
        await context.Quotations.AddAsync(quotation);
        await context.Customers.AddAsync(customer);

        Guid? protocolId = null;
        if (includeProtocol)
        {
            var protocol = new CustomerProtocol
            {
                Id = Guid.NewGuid(),
                Name = "学校协议价",
                Code = "SCHOOL_PROTOCOL",
                QuotationId = quotation.Id,
                EffectiveStart = new DateTime(2026, 7, 1)
            };
            protocolId = protocol.Id;
            await context.CustomerProtocols.AddAsync(protocol);
        }

        await context.SaveChangesAsync();
        return new PricingSeed(quotation.Id, customer.Id, goods.Id, unit.Id, protocolId);
    }

    private static async Task<GoodsSeed> AddGoodsWithUnitAsync(
        ApplicationDbContext context,
        string name,
        string code,
        string unitName)
    {
        var goodsTypeId = await context.Set<GoodsType>().Select(x => x.Id).FirstAsync();
        var goods = new GoodsEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            Code = code,
            GoodsTypeId = goodsTypeId
        };
        var unit = new GoodsUnit
        {
            Id = Guid.NewGuid(),
            GoodsId = goods.Id,
            Name = unitName,
            ConversionRate = 1m,
            IsBaseUnit = true
        };
        goods.BaseUnitId = unit.Id;
        await context.Goods.AddAsync(goods);
        await context.Set<GoodsUnit>().AddAsync(unit);
        await context.SaveChangesAsync();
        return new GoodsSeed(goods.Id, unit.Id);
    }

    private sealed record PricingSeed(
        Guid QuotationId,
        Guid CustomerId,
        Guid GoodsId,
        Guid GoodsUnitId,
        Guid? CustomerProtocolId);

    private sealed record GoodsSeed(Guid GoodsId, Guid GoodsUnitId);

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
