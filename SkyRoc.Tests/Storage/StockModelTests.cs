using Domain.Entities;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Orders;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace SkyRoc.Tests.Storage;

public class StockModelTests
{
    private readonly IModel model;

    public StockModelTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=skyroc_model_tests;Username=test;Password=test")
            .Options;
        using var context = new ApplicationDbContext(options);
        model = context.GetService<IDesignTimeModel>().Model;
    }

    [Fact]
    public void StockEntities_MapToExpectedTables()
    {
        Assert.Equal("stock_batch", GetEntityType<StockBatch>().GetTableName());
        Assert.Equal("stock_ledger", GetEntityType<StockLedger>().GetTableName());
        Assert.Equal("stock_in_order", GetEntityType<StockInOrder>().GetTableName());
        Assert.Equal("stock_in_detail", GetEntityType<StockInDetail>().GetTableName());
        Assert.Equal("stock_out_order", GetEntityType<StockOutOrder>().GetTableName());
        Assert.Equal("stock_out_detail", GetEntityType<StockOutDetail>().GetTableName());
        Assert.Equal("stocktaking_order", GetEntityType<StocktakingOrder>().GetTableName());
        Assert.Equal("stocktaking_detail", GetEntityType<StocktakingDetail>().GetTableName());
    }

    [Fact]
    public void StockBatch_ConfiguresUniqueIdentityAndQuantityPrecision()
    {
        var entityType = GetEntityType<StockBatch>();
        var identityIndex = entityType.GetIndexes().Single(
            index => index.GetDatabaseName() == "idx_stock_batch_ware_goods_batch");

        Assert.True(identityIndex.IsUnique);
        Assert.Equal(
            [nameof(StockBatch.WareId), nameof(StockBatch.GoodsId), nameof(StockBatch.BatchNo)],
            identityIndex.Properties.Select(property => property.Name));
        Assert.Equal("numeric(18,6)", entityType.FindProperty(nameof(StockBatch.CurrentQuantity))!.GetColumnType());
        Assert.Equal("numeric(18,6)", entityType.FindProperty(nameof(StockBatch.AvailableQuantity))!.GetColumnType());
        Assert.Equal("numeric(18,4)", entityType.FindProperty(nameof(StockBatch.UnitCost))!.GetColumnType());
        Assert.Contains(entityType.GetCheckConstraints(), constraint => constraint.Name == "ck_stock_batch_available_quantity");
    }

    [Fact]
    public void StockLedger_PreservesSourceBalanceAndSingleReversal()
    {
        var entityType = GetEntityType<StockLedger>();
        var reversalIndex = entityType.GetIndexes().Single(
            index => index.GetDatabaseName() == "idx_stock_ledger_reversed_from");

        Assert.True(reversalIndex.IsUnique);
        Assert.Equal("numeric(18,6)", entityType.FindProperty(nameof(StockLedger.ChangeQuantity))!.GetColumnType());
        Assert.Equal("numeric(18,6)", entityType.FindProperty(nameof(StockLedger.BalanceQuantity))!.GetColumnType());
        Assert.Equal("numeric(18,4)", entityType.FindProperty(nameof(StockLedger.TotalCost))!.GetColumnType());
        Assert.Contains(
            entityType.GetIndexes(),
            index => index.GetDatabaseName() == "idx_stock_ledger_source"
                     && index.Properties.Select(property => property.Name)
                         .SequenceEqual([nameof(StockLedger.SourceOrderId), nameof(StockLedger.SourceDetailId)]));
        AssertForeignKey<StockLedger, StockBatch>(nameof(StockLedger.StockBatchId), DeleteBehavior.Restrict);
        AssertForeignKey<StockLedger, StockLedger>(nameof(StockLedger.ReversedFromLedgerId), DeleteBehavior.Restrict);
    }

    [Fact]
    public void StockInModel_ConfiguresDocumentDefaultsPrecisionAndSources()
    {
        var orderType = GetEntityType<StockInOrder>();
        var detailType = GetEntityType<StockInDetail>();

        Assert.Equal(StockDocumentStatus.Draft, orderType.FindProperty(nameof(StockInOrder.BusinessStatus))!.GetDefaultValue());
        Assert.Equal(StockPrintStatus.NotPrinted, orderType.FindProperty(nameof(StockInOrder.PrintStatus))!.GetDefaultValue());
        Assert.True(orderType.GetIndexes().Single(index => index.GetDatabaseName() == "idx_stock_in_order_in_no").IsUnique);
        Assert.Equal("numeric(18,6)", detailType.FindProperty(nameof(StockInDetail.Quantity))!.GetColumnType());
        Assert.Equal("numeric(18,6)", detailType.FindProperty(nameof(StockInDetail.BaseQuantity))!.GetColumnType());
        Assert.Equal("numeric(18,4)", detailType.FindProperty(nameof(StockInDetail.UnitPrice))!.GetColumnType());
        Assert.Equal("date", detailType.FindProperty(nameof(StockInDetail.ExpireDate))!.GetColumnType());

        AssertForeignKey<StockInOrder, Ware>(nameof(StockInOrder.WareId), DeleteBehavior.Restrict);
        AssertForeignKey<StockInOrder, PurchaseOrder>(nameof(StockInOrder.PurchaseOrderId), DeleteBehavior.Restrict);
        AssertForeignKey<StockInDetail, StockInOrder>(nameof(StockInDetail.StockInOrderId), DeleteBehavior.Cascade);
        AssertForeignKey<StockInDetail, PurchaseOrderDetail>(nameof(StockInDetail.PurchaseOrderDetailId), DeleteBehavior.Restrict);
        AssertForeignKey<StockInDetail, StockBatch>(nameof(StockInDetail.StockBatchId), DeleteBehavior.Restrict);
    }

    [Fact]
    public void StockOutModel_ConfiguresDocumentDefaultsPrecisionAndSources()
    {
        var orderType = GetEntityType<StockOutOrder>();
        var detailType = GetEntityType<StockOutDetail>();

        Assert.Equal(StockDocumentStatus.Draft, orderType.FindProperty(nameof(StockOutOrder.BusinessStatus))!.GetDefaultValue());
        Assert.Equal(StockPrintStatus.NotPrinted, orderType.FindProperty(nameof(StockOutOrder.PrintStatus))!.GetDefaultValue());
        Assert.True(orderType.GetIndexes().Single(index => index.GetDatabaseName() == "idx_stock_out_order_out_no").IsUnique);
        Assert.Equal("numeric(18,6)", detailType.FindProperty(nameof(StockOutDetail.Quantity))!.GetColumnType());
        Assert.Equal("numeric(18,6)", detailType.FindProperty(nameof(StockOutDetail.BaseQuantity))!.GetColumnType());
        Assert.Equal("numeric(18,4)", detailType.FindProperty(nameof(StockOutDetail.TotalPrice))!.GetColumnType());

        AssertForeignKey<StockOutOrder, Ware>(nameof(StockOutOrder.WareId), DeleteBehavior.Restrict);
        AssertForeignKey<StockOutOrder, SaleOrder>(nameof(StockOutOrder.SaleOrderId), DeleteBehavior.Restrict);
        AssertForeignKey<StockOutDetail, StockOutOrder>(nameof(StockOutDetail.StockOutOrderId), DeleteBehavior.Cascade);
        AssertForeignKey<StockOutDetail, SaleOrderDetail>(nameof(StockOutDetail.SaleOrderDetailId), DeleteBehavior.Restrict);
        AssertForeignKey<StockOutDetail, StockBatch>(nameof(StockOutDetail.StockBatchId), DeleteBehavior.Restrict);
    }

    [Fact]
    public void StocktakingModel_PreventsDuplicateBatchAndTracksAdjustmentApplication()
    {
        var orderType = GetEntityType<StocktakingOrder>();
        var detailType = GetEntityType<StocktakingDetail>();
        var batchIndex = detailType.GetIndexes().Single(
            index => index.GetDatabaseName() == "idx_stocktaking_detail_order_batch");

        Assert.Equal(StockDocumentStatus.Draft, orderType.FindProperty(nameof(StocktakingOrder.BusinessStatus))!.GetDefaultValue());
        Assert.Equal(false, orderType.FindProperty(nameof(StocktakingOrder.IsAdjustmentApplied))!.GetDefaultValue());
        Assert.True(orderType.GetIndexes().Single(index => index.GetDatabaseName() == "idx_stocktaking_order_no").IsUnique);
        Assert.True(batchIndex.IsUnique);
        Assert.Equal("numeric(18,6)", detailType.FindProperty(nameof(StocktakingDetail.DifferenceQuantity))!.GetColumnType());
        Assert.Equal("numeric(18,4)", detailType.FindProperty(nameof(StocktakingDetail.DifferenceAmount))!.GetColumnType());

        AssertForeignKey<StocktakingOrder, Ware>(nameof(StocktakingOrder.WareId), DeleteBehavior.Restrict);
        AssertForeignKey<StocktakingDetail, StocktakingOrder>(nameof(StocktakingDetail.StocktakingOrderId), DeleteBehavior.Cascade);
        AssertForeignKey<StocktakingDetail, StockBatch>(nameof(StocktakingDetail.StockBatchId), DeleteBehavior.Restrict);
    }

    [Fact]
    public void StockDetails_KeepGoodsAndUnitReferencesRestricted()
    {
        AssertForeignKey<StockBatch, GoodsEntity>(nameof(StockBatch.GoodsId), DeleteBehavior.Restrict);
        AssertForeignKey<StockBatch, GoodsUnit>(nameof(StockBatch.BaseUnitId), DeleteBehavior.Restrict);
        AssertForeignKey<StockInDetail, GoodsEntity>(nameof(StockInDetail.GoodsId), DeleteBehavior.Restrict);
        AssertForeignKey<StockInDetail, GoodsUnit>(nameof(StockInDetail.GoodsUnitId), DeleteBehavior.Restrict);
        AssertForeignKey<StockOutDetail, GoodsEntity>(nameof(StockOutDetail.GoodsId), DeleteBehavior.Restrict);
        AssertForeignKey<StockOutDetail, GoodsUnit>(nameof(StockOutDetail.GoodsUnitId), DeleteBehavior.Restrict);
        AssertForeignKey<StocktakingDetail, GoodsEntity>(nameof(StocktakingDetail.GoodsId), DeleteBehavior.Restrict);
        AssertForeignKey<StocktakingDetail, GoodsUnit>(nameof(StocktakingDetail.BaseUnitId), DeleteBehavior.Restrict);
    }

    private IEntityType GetEntityType<TEntity>()
    {
        return model.FindEntityType(typeof(TEntity))
               ?? throw new InvalidOperationException($"{typeof(TEntity).Name} is not part of the EF model.");
    }

    private void AssertForeignKey<TDependent, TPrincipal>(string propertyName, DeleteBehavior deleteBehavior)
    {
        var foreignKey = GetEntityType<TDependent>().GetForeignKeys().Single(
            foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(TPrincipal)
                          && foreignKey.Properties.Select(property => property.Name).SequenceEqual([propertyName]));

        Assert.Equal(deleteBehavior, foreignKey.DeleteBehavior);
    }
}
