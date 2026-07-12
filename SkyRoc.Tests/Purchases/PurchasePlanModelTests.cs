using Domain.Entities.Goods;
using Domain.Entities.Orders;
using Domain.Entities.Purchases;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace SkyRoc.Tests.Purchases;

public class PurchasePlanModelTests
{
    private readonly IModel model;

    public PurchasePlanModelTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=skyroc_model_tests;Username=test;Password=test")
            .Options;
        using var context = new ApplicationDbContext(options);
        model = context.Model;
    }

    [Fact]
    public void PurchasePlanEntities_MapToExpectedTables()
    {
        Assert.Equal("purchase_plan", GetEntityType<PurchasePlan>().GetTableName());
        Assert.Equal("purchase_plan_detail", GetEntityType<PurchasePlanDetail>().GetTableName());
        Assert.Equal("purchase_plan_order_rel", GetEntityType<PurchasePlanOrderRelation>().GetTableName());
    }

    [Fact]
    public void PurchasePlan_ConfiguresDefaultsAndIndexes()
    {
        var entityType = GetEntityType<PurchasePlan>();

        Assert.Equal(PurchasePattern.SupplierDirect, entityType.FindProperty(nameof(PurchasePlan.PurchasePattern))!.GetDefaultValue());
        Assert.Equal((PurchasePattern)0, entityType.FindProperty(nameof(PurchasePlan.PurchasePattern))!.Sentinel);
        Assert.Equal(PurchasePlanStatus.Unpublished, entityType.FindProperty(nameof(PurchasePlan.PurchaseStatus))!.GetDefaultValue());
        Assert.Equal((PurchasePlanStatus)0, entityType.FindProperty(nameof(PurchasePlan.PurchaseStatus))!.Sentinel);

        var planNoIndex = entityType.GetIndexes().Single(x => x.GetDatabaseName() == "idx_purchase_plan_plan_no");
        Assert.True(planNoIndex.IsUnique);
        Assert.Contains(
            entityType.GetIndexes(),
            x => x.GetDatabaseName() == "idx_purchase_plan_date_status"
                 && x.Properties.Select(property => property.Name)
                     .SequenceEqual([nameof(PurchasePlan.PlanDate), nameof(PurchasePlan.PurchaseStatus)]));
    }

    [Fact]
    public void PurchasePlanDetail_ConfiguresQuantityPrecisionAndRequiredSnapshots()
    {
        var entityType = GetEntityType<PurchasePlanDetail>();

        Assert.Equal("numeric(18,6)", entityType.FindProperty(nameof(PurchasePlanDetail.RequiredQuantity))!.GetColumnType());
        Assert.Equal("numeric(18,6)", entityType.FindProperty(nameof(PurchasePlanDetail.PlannedQuantity))!.GetColumnType());
        Assert.Equal("numeric(18,6)", entityType.FindProperty(nameof(PurchasePlanDetail.PurchasedQuantity))!.GetColumnType());
        Assert.False(entityType.FindProperty(nameof(PurchasePlanDetail.GoodsNameSnapshot))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(PurchasePlanDetail.GoodsCodeSnapshot))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(PurchasePlanDetail.PurchaseUnitNameSnapshot))!.IsNullable);
    }

    [Fact]
    public void PurchasePlanRelationships_PreserveSourcesAndCascadeOwnedRecords()
    {
        AssertForeignKey<PurchasePlan, Supplier>(nameof(PurchasePlan.SupplierId), DeleteBehavior.SetNull);
        AssertForeignKey<PurchasePlan, Purchaser>(nameof(PurchasePlan.PurchaserId), DeleteBehavior.SetNull);
        AssertForeignKey<PurchasePlanDetail, PurchasePlan>(nameof(PurchasePlanDetail.PurchasePlanId), DeleteBehavior.Cascade);
        AssertForeignKey<PurchasePlanDetail, GoodsEntity>(nameof(PurchasePlanDetail.GoodsId), DeleteBehavior.Restrict);
        AssertForeignKey<PurchasePlanDetail, GoodsUnit>(nameof(PurchasePlanDetail.PurchaseUnitId), DeleteBehavior.Restrict);
        AssertForeignKey<PurchasePlanOrderRelation, PurchasePlanDetail>(nameof(PurchasePlanOrderRelation.PurchasePlanDetailId), DeleteBehavior.Cascade);
        AssertForeignKey<PurchasePlanOrderRelation, SaleOrder>(nameof(PurchasePlanOrderRelation.SaleOrderId), DeleteBehavior.Restrict);
        AssertForeignKey<PurchasePlanOrderRelation, SaleOrderDetail>(nameof(PurchasePlanOrderRelation.SaleOrderDetailId), DeleteBehavior.Restrict);
    }

    [Fact]
    public void PurchasePlanOrderRelation_PreventsDuplicateSourceWithinDetail()
    {
        var entityType = GetEntityType<PurchasePlanOrderRelation>();
        var sourceIndex = entityType.GetIndexes().Single(
            x => x.GetDatabaseName() == "idx_purchase_plan_order_rel_detail_source");

        Assert.True(sourceIndex.IsUnique);
        Assert.Equal(
            [nameof(PurchasePlanOrderRelation.PurchasePlanDetailId), nameof(PurchasePlanOrderRelation.SaleOrderDetailId)],
            sourceIndex.Properties.Select(property => property.Name));
        Assert.Equal(
            "numeric(18,6)",
            entityType.FindProperty(nameof(PurchasePlanOrderRelation.RequiredQuantity))!.GetColumnType());
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
