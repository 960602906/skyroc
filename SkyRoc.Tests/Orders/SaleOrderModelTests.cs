using Domain.Entities;
using Domain.Entities.Customers;
using Domain.Entities.Orders;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace SkyRoc.Tests.Orders;

public class SaleOrderModelTests
{
    private readonly IModel model;

    public SaleOrderModelTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=skyroc_model_tests;Username=test;Password=test")
            .Options;
        using var context = new ApplicationDbContext(options);
        model = context.Model;
    }

    [Fact]
    public void OrderEntities_MapToExpectedTables()
    {
        Assert.Equal("sale_order", GetEntityType<SaleOrder>().GetTableName());
        Assert.Equal("sale_order_detail", GetEntityType<SaleOrderDetail>().GetTableName());
        Assert.Equal("order_audit_log", GetEntityType<OrderAuditLog>().GetTableName());
    }

    [Fact]
    public void SaleOrder_ConfiguresBusinessDefaultsAndIndexes()
    {
        var entityType = GetEntityType<SaleOrder>();

        Assert.Equal(SaleOrderStatus.PendingAudit, entityType.FindProperty(nameof(SaleOrder.OrderStatus))!.GetDefaultValue());
        Assert.Equal(OrderReturnStatus.NotReturned, entityType.FindProperty(nameof(SaleOrder.ReturnStatus))!.GetDefaultValue());
        Assert.Equal(OrderPrintStatus.NotPrinted, entityType.FindProperty(nameof(SaleOrder.PrintStatus))!.GetDefaultValue());
        Assert.Equal("numeric(18,4)", entityType.FindProperty(nameof(SaleOrder.OrderPrice))!.GetColumnType());
        Assert.Equal("numeric(18,4)", entityType.FindProperty(nameof(SaleOrder.SettlementPrice))!.GetColumnType());

        var orderNoIndex = entityType.GetIndexes().Single(x => x.GetDatabaseName() == "idx_sale_order_order_no");
        Assert.True(orderNoIndex.IsUnique);
        Assert.Contains(
            entityType.GetIndexes(),
            x => x.GetDatabaseName() == "idx_sale_order_date_status"
                 && x.Properties.Select(property => property.Name)
                     .SequenceEqual([nameof(SaleOrder.OrderDate), nameof(SaleOrder.OrderStatus)]));
    }

    [Fact]
    public void SaleOrderDetail_ConfiguresQuantityAndMoneyPrecision()
    {
        var entityType = GetEntityType<SaleOrderDetail>();

        Assert.Equal("numeric(18,6)", entityType.FindProperty(nameof(SaleOrderDetail.Quantity))!.GetColumnType());
        Assert.Equal("numeric(18,6)", entityType.FindProperty(nameof(SaleOrderDetail.BaseQuantity))!.GetColumnType());
        Assert.Equal("numeric(18,6)", entityType.FindProperty(nameof(SaleOrderDetail.UnitConversion))!.GetColumnType());
        Assert.Equal("numeric(18,4)", entityType.FindProperty(nameof(SaleOrderDetail.FixedPrice))!.GetColumnType());
        Assert.Equal("numeric(18,4)", entityType.FindProperty(nameof(SaleOrderDetail.TotalPrice))!.GetColumnType());
        Assert.False(entityType.FindProperty(nameof(SaleOrderDetail.GoodsNameSnapshot))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(SaleOrderDetail.GoodsCodeSnapshot))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(SaleOrderDetail.GoodsUnitNameSnapshot))!.IsNullable);
    }

    [Fact]
    public void OrderRelationships_PreserveHistoryAndCascadeOwnedRecords()
    {
        AssertForeignKey<SaleOrder, Customer>(nameof(SaleOrder.CustomerId), DeleteBehavior.Restrict);
        AssertForeignKey<SaleOrderDetail, SaleOrder>(nameof(SaleOrderDetail.SaleOrderId), DeleteBehavior.Cascade);
        AssertForeignKey<SaleOrderDetail, GoodsEntity>(nameof(SaleOrderDetail.GoodsId), DeleteBehavior.Restrict);
        AssertForeignKey<OrderAuditLog, SaleOrder>(nameof(OrderAuditLog.SaleOrderId), DeleteBehavior.Cascade);
        AssertForeignKey<OrderAuditLog, User>(nameof(OrderAuditLog.AuditUserId), DeleteBehavior.SetNull);
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
