using Application.DTOs.Goods;
using Application.Exceptions;
using Application.Interfaces;
using Application.Mappers;
using Application.Services;
using Application.Validator;
using AutoMapper;
using Domain.Entities.Goods;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Constants;
using Xunit;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace SkyRoc.Tests.Goods;

public class GoodsServiceTests
{
    private static readonly Guid CurrentUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task Get_tree_should_nest_child_goods_type()
    {
        await using var context = CreateDbContext();
        var parent = new GoodsType
        {
            Id = Guid.NewGuid(),
            Name = "生鲜",
            Code = "FRESH",
            Sort = 1
        };
        var child = new GoodsType
        {
            Id = Guid.NewGuid(),
            Name = "蔬菜",
            Code = "VEGETABLE",
            ParentId = parent.Id,
            Sort = 1
        };
        await context.Set<GoodsType>().AddRangeAsync(parent, child);
        await context.SaveChangesAsync();

        var result = await CreateGoodsTypeService(context).GetTreeAsync();

        var root = Assert.Single(result);
        Assert.Equal(parent.Id, root.Id);
        Assert.Equal(child.Id, Assert.Single(root.Children!).Id);
    }

    [Fact]
    public async Task Delete_goods_type_should_fail_when_referenced_by_goods()
    {
        await using var context = CreateDbContext();
        var data = await SeedGoodsAsync(context);

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => CreateGoodsTypeService(context).DeleteAsync(data.GoodsTypeId));

        Assert.Equal("商品分类已被商品引用，不能删除", exception.Message);
        Assert.True(await context.Set<GoodsType>().AnyAsync(x => x.Id == data.GoodsTypeId));
    }

    [Fact]
    public async Task Create_base_unit_should_update_goods_and_clear_previous_base_unit()
    {
        await using var context = CreateDbContext();
        var data = await SeedGoodsAsync(context, includeBaseUnit: true);
        var service = CreateGoodsUnitService(context);

        var result = await service.CreateAsync(new CreateGoodsUnitDto
        {
            GoodsId = data.GoodsId,
            Name = "箱",
            Code = "BOX",
            ConversionRate = 20m,
            IsBaseUnit = true,
            Sort = 2
        });

        var goods = await context.Goods.SingleAsync(x => x.Id == data.GoodsId);
        var units = await context.Set<GoodsUnit>()
            .Where(x => x.GoodsId == data.GoodsId)
            .OrderBy(x => x.Sort)
            .ToListAsync();

        Assert.Equal(result.Id, goods.BaseUnitId);
        Assert.False(units[0].IsBaseUnit);
        Assert.True(units[1].IsBaseUnit);
        Assert.Equal(CurrentUserId, units[1].CreateBy);
    }

    [Fact]
    public async Task Create_goods_unit_should_fail_when_name_is_duplicated()
    {
        await using var context = CreateDbContext();
        var data = await SeedGoodsAsync(context, includeBaseUnit: true);

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            CreateGoodsUnitService(context).CreateAsync(new CreateGoodsUnitDto
            {
                GoodsId = data.GoodsId,
                Name = "千克",
                Code = "KG_2",
                ConversionRate = 1m
            }));

        Assert.Equal("商品单位名称已经存在", exception.Message);
    }

    [Fact]
    public async Task Toggle_sale_status_should_persist_on_and_off_state()
    {
        await using var context = CreateDbContext();
        var data = await SeedGoodsAsync(context);
        var service = CreateGoodsService(context);

        var offSale = await service.ToggleSaleStatusAsync(data.GoodsId, false);
        var onSale = await service.ToggleSaleStatusAsync(data.GoodsId, true);

        Assert.False(offSale.IsOnSale);
        Assert.True(onSale.IsOnSale);
        var savedGoods = await context.Goods.SingleAsync(x => x.Id == data.GoodsId);
        Assert.True(savedGoods.IsOnSale);
        Assert.Equal(CurrentUserId, savedGoods.UpdateBy);
    }

    [Fact]
    public async Task Update_goods_should_preserve_existing_base_unit_id()
    {
        await using var context = CreateDbContext();
        var data = await SeedGoodsAsync(context, includeBaseUnit: true);
        var service = CreateGoodsService(context);
        var goods = await context.Goods.SingleAsync(x => x.Id == data.GoodsId);

        await service.UpdateAsync(goods.Id, new UpdateGoodsDto
        {
            Id = goods.Id,
            Code = goods.Code,
            Name = "番茄（联调更新）",
            GoodsTypeId = data.GoodsTypeId,
            Spec = "一级果",
            Brand = "鲜品联调",
            Origin = "华东",
            Description = "更新商品档案不得清空基础单位。",
            TaxRate = 0.09m,
            IsOnSale = true,
            Status = Status.Enable,
            BaseUnitId = null,
            SupplierIds = []
        });

        var savedGoods = await context.Goods.SingleAsync(x => x.Id == data.GoodsId);
        Assert.Equal(data.BaseUnitId, savedGoods.BaseUnitId);
        Assert.Equal("番茄（联调更新）", savedGoods.Name);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static GoodsTypeService CreateGoodsTypeService(ApplicationDbContext context)
    {
        return new GoodsTypeService(
            new GoodsTypeRepository(context),
            new GoodsRepository(context),
            new UnitOfWork(context),
            NullLogger<GoodsTypeService>.Instance,
            CreateMapper(),
            new FakeCurrentUserService(),
            new CreateGoodsTypeValidator(),
            new UpdateGoodsTypeValidator());
    }

    private static GoodsService CreateGoodsService(ApplicationDbContext context)
    {
        return new GoodsService(
            new GoodsRepository(context),
            new GoodsTypeRepository(context),
            new SupplierRepository(context),
            new WareRepository(context),
            new UnitOfWork(context),
            NullLogger<GoodsService>.Instance,
            CreateMapper(),
            new FakeCurrentUserService(),
            new CreateGoodsValidator(),
            new UpdateGoodsValidator());
    }

    private static GoodsUnitService CreateGoodsUnitService(ApplicationDbContext context)
    {
        return new GoodsUnitService(
            new GoodsUnitRepository(context),
            new GoodsRepository(context),
            new UnitOfWork(context),
            NullLogger<GoodsUnitService>.Instance,
            CreateMapper(),
            new FakeCurrentUserService(),
            new CreateGoodsUnitValidator(),
            new UpdateGoodsUnitValidator());
    }

    private static IMapper CreateMapper()
    {
        return new MapperConfiguration(cfg => cfg.AddProfile<BaseDataMappingProfile>()).CreateMapper();
    }

    private static async Task<SeedResult> SeedGoodsAsync(
        ApplicationDbContext context,
        bool includeBaseUnit = false)
    {
        var goodsType = new GoodsType
        {
            Id = Guid.NewGuid(),
            Name = "蔬菜",
            Code = "VEGETABLE",
            Sort = 1
        };
        var goods = new GoodsEntity
        {
            Id = Guid.NewGuid(),
            Name = "番茄",
            Code = "TOMATO",
            GoodsTypeId = goodsType.Id,
            IsOnSale = true
        };

        await context.Set<GoodsType>().AddAsync(goodsType);
        await context.Goods.AddAsync(goods);

        Guid? baseUnitId = null;
        if (includeBaseUnit)
        {
            var unit = new GoodsUnit
            {
                Id = Guid.NewGuid(),
                GoodsId = goods.Id,
                Name = "千克",
                Code = "KG",
                ConversionRate = 1m,
                IsBaseUnit = true,
                Sort = 1
            };
            baseUnitId = unit.Id;
            goods.BaseUnitId = unit.Id;
            await context.Set<GoodsUnit>().AddAsync(unit);
        }

        await context.SaveChangesAsync();
        return new SeedResult(goodsType.Id, goods.Id, baseUnitId);
    }

    private sealed record SeedResult(Guid GoodsTypeId, Guid GoodsId, Guid? BaseUnitId);

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
