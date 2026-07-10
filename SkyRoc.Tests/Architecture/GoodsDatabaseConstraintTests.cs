using Domain.Entities.Goods;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace SkyRoc.Tests.Architecture;

/// <summary>商品名称和编码数据库唯一性约束回归测试，防止并发导入绕过应用层预检。</summary>
public class GoodsDatabaseConstraintTests
{
    [Fact]
    public void GoodsModel_RequiresUniqueNameAndCode()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=skyroc_model_tests;Username=test;Password=test")
            .Options;
        using var context = new ApplicationDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(GoodsEntity))!;

        Assert.True(entityType.FindIndex(entityType.FindProperty(nameof(GoodsEntity.Name))!)!.IsUnique);
        Assert.True(entityType.FindIndex(entityType.FindProperty(nameof(GoodsEntity.Code))!)!.IsUnique);
    }
}
