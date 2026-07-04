using Domain.Entities.Customers;
using Domain.Entities.Delivery;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace SkyRoc.Tests.Delivery;

/// <summary>
/// 校验配送基础模型的表映射、唯一约束、外键删除行为和默认值配置。
/// </summary>
public class DeliveryModelTests
{
    private readonly IModel model;

    /// <summary>
    /// 构建设计期 EF Core 模型用于结构断言，不建立真实数据库连接。
    /// </summary>
    public DeliveryModelTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=skyroc_model_tests;Username=test;Password=test")
            .Options;
        using var context = new ApplicationDbContext(options);
        model = context.GetService<IDesignTimeModel>().Model;
    }

    [Fact]
    public void DeliveryEntities_MapToExpectedTables()
    {
        Assert.Equal("carrier", GetEntityType<Carrier>().GetTableName());
        Assert.Equal("driver", GetEntityType<Driver>().GetTableName());
        Assert.Equal("delivery_route", GetEntityType<DeliveryRoute>().GetTableName());
        Assert.Equal("customer_route", GetEntityType<CustomerRoute>().GetTableName());
        Assert.Equal("delivery_exception", GetEntityType<DeliveryException>().GetTableName());
    }

    [Fact]
    public void Carrier_ConfiguresUniqueCode()
    {
        var index = GetEntityType<Carrier>().GetIndexes().Single(
            x => x.GetDatabaseName() == "idx_carrier_code");

        Assert.True(index.IsUnique);
        Assert.Equal([nameof(Carrier.Code)], index.Properties.Select(x => x.Name));
    }

    [Fact]
    public void Driver_ConfiguresUniqueCodeAndOptionalCarrier()
    {
        var entityType = GetEntityType<Driver>();

        Assert.True(entityType.GetIndexes().Single(x => x.GetDatabaseName() == "idx_driver_code").IsUnique);
        AssertForeignKey<Driver, Carrier>(nameof(Driver.CarrierId), DeleteBehavior.SetNull);
    }

    [Fact]
    public void DeliveryRoute_ConfiguresUniqueCode()
    {
        Assert.True(GetEntityType<DeliveryRoute>().GetIndexes()
            .Single(x => x.GetDatabaseName() == "idx_delivery_route_code").IsUnique);
    }

    [Fact]
    public void CustomerRoute_EnforcesUniquePairAndDeleteBehaviors()
    {
        var entityType = GetEntityType<CustomerRoute>();
        var pairIndex = entityType.GetIndexes().Single(
            x => x.GetDatabaseName() == "idx_customer_route_route_customer");

        Assert.True(pairIndex.IsUnique);
        Assert.Equal(
            [nameof(CustomerRoute.RouteId), nameof(CustomerRoute.CustomerId)],
            pairIndex.Properties.Select(x => x.Name));
        AssertForeignKey<CustomerRoute, DeliveryRoute>(nameof(CustomerRoute.RouteId), DeleteBehavior.Cascade);
        AssertForeignKey<CustomerRoute, Customer>(nameof(CustomerRoute.CustomerId), DeleteBehavior.Restrict);
    }

    [Fact]
    public void DeliveryException_ConfiguresUniqueNumberDefaultStatusAndReferences()
    {
        var entityType = GetEntityType<DeliveryException>();

        Assert.True(entityType.GetIndexes().Single(x => x.GetDatabaseName() == "idx_delivery_exception_no").IsUnique);
        Assert.Equal(
            DeliveryExceptionStatus.Pending,
            entityType.FindProperty(nameof(DeliveryException.HandleStatus))!.GetDefaultValue());
        AssertForeignKey<DeliveryException, Driver>(nameof(DeliveryException.DriverId), DeleteBehavior.SetNull);
        AssertForeignKey<DeliveryException, Customer>(nameof(DeliveryException.CustomerId), DeleteBehavior.Restrict);
    }

    private IEntityType GetEntityType<TEntity>()
    {
        return model.FindEntityType(typeof(TEntity))
               ?? throw new InvalidOperationException($"{typeof(TEntity).Name} is not part of the EF model.");
    }

    private void AssertForeignKey<TDependent, TPrincipal>(string propertyName, DeleteBehavior deleteBehavior)
    {
        var foreignKey = GetEntityType<TDependent>().GetForeignKeys().Single(
            x => x.PrincipalEntityType.ClrType == typeof(TPrincipal)
                 && x.Properties.Select(property => property.Name).SequenceEqual([propertyName]));

        Assert.Equal(deleteBehavior, foreignKey.DeleteBehavior);
    }
}
