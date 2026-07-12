using Domain.Entities;
using Domain.Entities.AfterSales;
using Domain.Entities.Customers;
using Domain.Entities.Delivery;
using Domain.Entities.Goods;
using Domain.Entities.Orders;
using Domain.Entities.Purchases;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Shared.Constants;
using Xunit;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace SkyRoc.Tests.AfterSales;

/// <summary>
/// 校验售后模型的表映射、状态、精度、约束和历史数据保护关系。
/// </summary>
public class AfterSaleModelTests
{
    private readonly IModel model;

    /// <summary>
    /// 构建设计期 EF Core 模型用于结构断言，不建立真实数据库连接。
    /// </summary>
    public AfterSaleModelTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=skyroc_model_tests;Username=test;Password=test")
            .Options;
        using var context = new ApplicationDbContext(options);
        model = context.GetService<IDesignTimeModel>().Model;
    }

    [Fact]
    public void AfterSaleEntities_MapToExpectedTables()
    {
        Assert.Equal("after_sale", GetEntityType<AfterSale>().GetTableName());
        Assert.Equal("after_sale_goods", GetEntityType<AfterSaleGoods>().GetTableName());
        Assert.Equal("after_sale_audit_log", GetEntityType<AfterSaleAuditLog>().GetTableName());
        Assert.Equal("pickup_task", GetEntityType<PickupTask>().GetTableName());
    }

    [Fact]
    public void AfterSaleStatus_UsesDocumentedBusinessValues()
    {
        Assert.Equal(1, (int)AfterSaleStatus.Draft);
        Assert.Equal(2, (int)AfterSaleStatus.PendingAudit);
        Assert.Equal(3, (int)AfterSaleStatus.ReturnPending);
        Assert.Equal(4, (int)AfterSaleStatus.RefundPending);
        Assert.Equal(5, (int)AfterSaleStatus.Completed);
    }

    [Fact]
    public void AfterSale_ConfiguresDefaultsUniqueNumberAndMoneyPrecision()
    {
        var entityType = GetEntityType<AfterSale>();

        Assert.Equal(AfterSaleStatus.Draft, entityType.FindProperty(nameof(AfterSale.AfterStatus))!.GetDefaultValue());
        Assert.Equal((AfterSaleStatus)0, entityType.FindProperty(nameof(AfterSale.AfterStatus))!.Sentinel);
        Assert.Equal(NumericPrecision.MoneyScale, entityType.FindProperty(nameof(AfterSale.OrderPrice))!.GetScale());
        Assert.Equal(NumericPrecision.MoneyScale, entityType.FindProperty(nameof(AfterSale.SettlementPrice))!.GetScale());
        Assert.True(entityType.GetIndexes().Single(x => x.GetDatabaseName() == "idx_after_sale_no").IsUnique);
        Assert.Contains(entityType.GetCheckConstraints(), x => x.Name == "ck_after_sale_amounts");
        Assert.Contains(entityType.GetCheckConstraints(), x => x.Name == "ck_after_sale_status");
    }

    [Fact]
    public void AfterSaleGoods_ConfiguresGlobalPrecisionAndPositiveValueConstraints()
    {
        var entityType = GetEntityType<AfterSaleGoods>();

        Assert.Equal(NumericPrecision.QuantityScale, entityType.FindProperty(nameof(AfterSaleGoods.ConversionRate))!.GetScale());
        Assert.Equal(NumericPrecision.QuantityScale, entityType.FindProperty(nameof(AfterSaleGoods.ActualRefundQuantity))!.GetScale());
        Assert.Equal(NumericPrecision.QuantityScale, entityType.FindProperty(nameof(AfterSaleGoods.BaseRefundQuantity))!.GetScale());
        Assert.Equal(NumericPrecision.MoneyScale, entityType.FindProperty(nameof(AfterSaleGoods.UnitPrice))!.GetScale());
        Assert.Equal(NumericPrecision.MoneyScale, entityType.FindProperty(nameof(AfterSaleGoods.RefundAmount))!.GetScale());
        Assert.Contains(entityType.GetCheckConstraints(), x => x.Name == "ck_after_sale_goods_conversion_rate");
        Assert.Contains(entityType.GetCheckConstraints(), x => x.Name == "ck_after_sale_goods_quantities");
        Assert.Contains(entityType.GetCheckConstraints(), x => x.Name == "ck_after_sale_goods_amounts");
        Assert.Contains(entityType.GetCheckConstraints(), x => x.Name == "ck_after_sale_goods_type");
        Assert.Contains(entityType.GetCheckConstraints(), x => x.Name == "ck_after_sale_goods_reason");
        Assert.Contains(entityType.GetCheckConstraints(), x => x.Name == "ck_after_sale_goods_handle");

        var sourceIndex = entityType.GetIndexes().Single(
            x => x.GetDatabaseName() == "idx_after_sale_goods_order_detail");
        Assert.True(sourceIndex.IsUnique);
        Assert.Equal(
            [nameof(AfterSaleGoods.AfterSaleId), nameof(AfterSaleGoods.SaleOrderDetailId)],
            sourceIndex.Properties.Select(x => x.Name));
    }

    [Fact]
    public void PickupTask_EnforcesOneTaskPerAfterSaleGoodsAndDefaultsToPendingAssign()
    {
        var entityType = GetEntityType<PickupTask>();

        Assert.True(entityType.GetIndexes().Single(x => x.GetDatabaseName() == "idx_pickup_task_no").IsUnique);
        Assert.True(entityType.GetIndexes().Single(
            x => x.GetDatabaseName() == "idx_pickup_task_after_sale_goods_id").IsUnique);
        Assert.Equal(
            PickupTaskStatus.PendingAssign,
            entityType.FindProperty(nameof(PickupTask.PickupStatus))!.GetDefaultValue());
        Assert.Equal((PickupTaskStatus)0, entityType.FindProperty(nameof(PickupTask.PickupStatus))!.Sentinel);
        Assert.Contains(entityType.GetCheckConstraints(), x => x.Name == "ck_pickup_task_status");
    }

    [Fact]
    public void AfterSaleAuditLog_ConstrainsActionAndStatusValues()
    {
        var entityType = GetEntityType<AfterSaleAuditLog>();

        Assert.Contains(entityType.GetCheckConstraints(), x => x.Name == "ck_after_sale_audit_action");
        Assert.Contains(entityType.GetCheckConstraints(), x => x.Name == "ck_after_sale_audit_statuses");
    }

    [Fact]
    public void AfterSaleRelationships_CascadeOwnedRecordsAndProtectBusinessHistory()
    {
        AssertForeignKey<AfterSale, SaleOrder>(nameof(AfterSale.SaleOrderId), DeleteBehavior.Restrict);
        AssertForeignKey<AfterSale, Customer>(nameof(AfterSale.CustomerId), DeleteBehavior.Restrict);
        AssertForeignKey<AfterSaleGoods, AfterSale>(nameof(AfterSaleGoods.AfterSaleId), DeleteBehavior.Cascade);
        AssertForeignKey<AfterSaleGoods, SaleOrderDetail>(nameof(AfterSaleGoods.SaleOrderDetailId), DeleteBehavior.Restrict);
        AssertForeignKey<AfterSaleGoods, GoodsEntity>(nameof(AfterSaleGoods.GoodsId), DeleteBehavior.Restrict);
        AssertForeignKey<AfterSaleGoods, GoodsUnit>(nameof(AfterSaleGoods.GoodsUnitId), DeleteBehavior.Restrict);
        AssertForeignKey<AfterSaleGoods, Supplier>(nameof(AfterSaleGoods.SupplierId), DeleteBehavior.SetNull);
        AssertForeignKey<AfterSaleGoods, Department>(nameof(AfterSaleGoods.DepartmentId), DeleteBehavior.SetNull);
        AssertForeignKey<AfterSaleAuditLog, AfterSale>(nameof(AfterSaleAuditLog.AfterSaleId), DeleteBehavior.Cascade);
        AssertForeignKey<AfterSaleAuditLog, User>(nameof(AfterSaleAuditLog.AuditUserId), DeleteBehavior.SetNull);
        AssertForeignKey<PickupTask, AfterSale>(nameof(PickupTask.AfterSaleId), DeleteBehavior.Restrict);
        AssertForeignKey<PickupTask, Driver>(nameof(PickupTask.DriverId), DeleteBehavior.Restrict);

        var goodsForeignKey = GetEntityType<PickupTask>().GetForeignKeys().Single(
            x => x.PrincipalEntityType.ClrType == typeof(AfterSaleGoods));
        Assert.Equal(DeleteBehavior.Restrict, goodsForeignKey.DeleteBehavior);
        Assert.Equal(
            [nameof(PickupTask.AfterSaleGoodsId), nameof(PickupTask.AfterSaleId)],
            goodsForeignKey.Properties.Select(x => x.Name));
        Assert.Equal(
            [nameof(AfterSaleGoods.Id), nameof(AfterSaleGoods.AfterSaleId)],
            goodsForeignKey.PrincipalKey.Properties.Select(x => x.Name));
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
