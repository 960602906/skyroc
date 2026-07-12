using Domain.Entities.Goods;
using Domain.Entities.Purchases;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace SkyRoc.Tests.Purchases;

public class PurchaseOrderModelTests
{
    private readonly IModel model;

    public PurchaseOrderModelTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=skyroc_model_tests;Username=test;Password=test")
            .Options;
        using var context = new ApplicationDbContext(options);
        model = context.Model;
    }

    [Fact]
    public void PurchaseOrderEntities_MapToExpectedTables()
    {
        Assert.Equal("purchase_order", GetEntityType<PurchaseOrder>().GetTableName());
        Assert.Equal("purchase_order_detail", GetEntityType<PurchaseOrderDetail>().GetTableName());
        Assert.Equal("purchase_order_plan_rel", GetEntityType<PurchaseOrderPlanRelation>().GetTableName());
    }

    [Fact]
    public void PurchaseOrder_ConfiguresDefaultsAndLookupIndexes()
    {
        var entityType = GetEntityType<PurchaseOrder>();

        Assert.Equal(PurchasePattern.SupplierDirect, entityType.FindProperty(nameof(PurchaseOrder.PurchasePattern))!.GetDefaultValue());
        Assert.Equal((PurchasePattern)0, entityType.FindProperty(nameof(PurchaseOrder.PurchasePattern))!.Sentinel);
        Assert.Equal(PurchaseOrderStatus.Draft, entityType.FindProperty(nameof(PurchaseOrder.BusinessStatus))!.GetDefaultValue());
        Assert.Equal((PurchaseOrderStatus)0, entityType.FindProperty(nameof(PurchaseOrder.BusinessStatus))!.Sentinel);

        var purchaseNoIndex = entityType.GetIndexes().Single(x => x.GetDatabaseName() == "idx_purchase_order_purchase_no");
        Assert.True(purchaseNoIndex.IsUnique);
        Assert.Contains(
            entityType.GetIndexes(),
            x => x.GetDatabaseName() == "idx_purchase_order_receive_status"
                 && x.Properties.Select(property => property.Name)
                     .SequenceEqual([nameof(PurchaseOrder.ReceiveTime), nameof(PurchaseOrder.BusinessStatus)]));
    }

    [Fact]
    public void PurchaseOrderDetail_ConfiguresQuantityMoneyAndDateTypes()
    {
        var entityType = GetEntityType<PurchaseOrderDetail>();

        Assert.Equal("numeric(18,6)", entityType.FindProperty(nameof(PurchaseOrderDetail.RequiredQuantity))!.GetColumnType());
        Assert.Equal("numeric(18,6)", entityType.FindProperty(nameof(PurchaseOrderDetail.PurchaseQuantity))!.GetColumnType());
        Assert.Equal("numeric(18,4)", entityType.FindProperty(nameof(PurchaseOrderDetail.PurchasePrice))!.GetColumnType());
        Assert.Equal("numeric(18,4)", entityType.FindProperty(nameof(PurchaseOrderDetail.PurchaseTotalPrice))!.GetColumnType());
        Assert.Equal("date", entityType.FindProperty(nameof(PurchaseOrderDetail.ProductDate))!.GetColumnType());
        Assert.False(entityType.FindProperty(nameof(PurchaseOrderDetail.GoodsNameSnapshot))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(PurchaseOrderDetail.GoodsCodeSnapshot))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(PurchaseOrderDetail.PurchaseUnitNameSnapshot))!.IsNullable);
    }

    [Fact]
    public void PurchaseOrderRelationships_PreserveSourcesAndCascadeOwnedRecords()
    {
        AssertForeignKey<PurchaseOrder, Supplier>(nameof(PurchaseOrder.SupplierId), DeleteBehavior.SetNull);
        AssertForeignKey<PurchaseOrder, Purchaser>(nameof(PurchaseOrder.PurchaserId), DeleteBehavior.SetNull);
        AssertForeignKey<PurchaseOrderDetail, PurchaseOrder>(nameof(PurchaseOrderDetail.PurchaseOrderId), DeleteBehavior.Cascade);
        AssertForeignKey<PurchaseOrderDetail, GoodsEntity>(nameof(PurchaseOrderDetail.GoodsId), DeleteBehavior.Restrict);
        AssertForeignKey<PurchaseOrderDetail, GoodsUnit>(nameof(PurchaseOrderDetail.PurchaseUnitId), DeleteBehavior.Restrict);
        AssertForeignKey<PurchaseOrderPlanRelation, PurchaseOrderDetail>(nameof(PurchaseOrderPlanRelation.PurchaseOrderDetailId), DeleteBehavior.Cascade);
        AssertForeignKey<PurchaseOrderPlanRelation, PurchasePlanDetail>(nameof(PurchaseOrderPlanRelation.PurchasePlanDetailId), DeleteBehavior.Restrict);
    }

    [Fact]
    public void PurchaseOrderPlanRelation_PreventsDuplicatePlanAllocationWithinDetail()
    {
        var entityType = GetEntityType<PurchaseOrderPlanRelation>();
        var sourceIndex = entityType.GetIndexes().Single(
            x => x.GetDatabaseName() == "idx_purchase_order_plan_rel_detail_plan");

        Assert.True(sourceIndex.IsUnique);
        Assert.Equal(
            [nameof(PurchaseOrderPlanRelation.PurchaseOrderDetailId), nameof(PurchaseOrderPlanRelation.PurchasePlanDetailId)],
            sourceIndex.Properties.Select(property => property.Name));
        Assert.Equal(
            "numeric(18,6)",
            entityType.FindProperty(nameof(PurchaseOrderPlanRelation.AllocatedQuantity))!.GetColumnType());
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
