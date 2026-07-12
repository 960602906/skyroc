using Domain.Entities.Finance;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Shared.Constants;
using Xunit;
using GoodsEntity = Domain.Entities.Goods.Goods;
using Domain.Entities.Goods;

namespace SkyRoc.Tests.Finance;

/// <summary>
/// 校验供应商待结单据与结算单模型的表映射、状态值、金额精度、来源唯一性和历史外键保护。
/// </summary>
public class SupplierBillModelTests
{
    private readonly IModel model;

    /// <summary>
    /// 构建设计期 EF Core 模型用于结构断言，不连接真实数据库。
    /// </summary>
    public SupplierBillModelTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=skyroc_model_tests;Username=test;Password=test")
            .Options;
        using var context = new ApplicationDbContext(options);
        model = context.GetService<IDesignTimeModel>().Model;
    }

    [Fact]
    public void SupplierBillEntities_MapToExpectedTables()
    {
        Assert.Equal("supplier_bill", GetEntityType<SupplierBill>().GetTableName());
        Assert.Equal("supplier_bill_detail", GetEntityType<SupplierBillDetail>().GetTableName());
        Assert.Equal("supplier_settlement", GetEntityType<SupplierSettlement>().GetTableName());
        Assert.Equal("supplier_settlement_detail", GetEntityType<SupplierSettlementDetail>().GetTableName());
    }

    [Fact]
    public void SupplierFinanceEnums_UseDocumentedBusinessValues()
    {
        Assert.Equal(1, (int)SupplierBillStatus.Pending);
        Assert.Equal(2, (int)SupplierBillStatus.PartiallySettled);
        Assert.Equal(3, (int)SupplierBillStatus.Settled);
        Assert.Equal(1, (int)SupplierBillSourceType.PurchaseStockIn);
        Assert.Equal(2, (int)SupplierBillSourceType.PurchaseReturnOut);
        Assert.Equal(-1, (int)SupplierSettlementStatus.Voided);
        Assert.Equal(1, (int)SupplierSettlementStatus.Pending);
        Assert.Equal(2, (int)SupplierSettlementStatus.PartiallySettled);
        Assert.Equal(3, (int)SupplierSettlementStatus.Settled);
    }

    [Fact]
    public void SupplierBill_ConfiguresUniqueSourceAndMoneyPrecision()
    {
        var entityType = GetEntityType<SupplierBill>();

        Assert.True(entityType.GetIndexes().Single(x => x.GetDatabaseName() == "idx_supplier_bill_no").IsUnique);
        Assert.True(entityType.GetIndexes().Single(x => x.GetDatabaseName() == "idx_supplier_bill_stock_in_order_id").IsUnique);
        Assert.True(entityType.GetIndexes().Single(x => x.GetDatabaseName() == "idx_supplier_bill_stock_out_order_id").IsUnique);
        Assert.Equal(SupplierBillStatus.Pending, entityType.FindProperty(nameof(SupplierBill.BillStatus))!.GetDefaultValue());
        Assert.Equal((SupplierBillStatus)0, entityType.FindProperty(nameof(SupplierBill.BillStatus))!.Sentinel);
        Assert.Equal(NumericPrecision.MoneyScale, entityType.FindProperty(nameof(SupplierBill.DocumentAmount))!.GetScale());
        Assert.Equal(NumericPrecision.MoneyScale, entityType.FindProperty(nameof(SupplierBill.PayableAmount))!.GetScale());
        Assert.Equal(NumericPrecision.MoneyScale, entityType.FindProperty(nameof(SupplierBill.SettledAmount))!.GetScale());
        Assert.Contains(entityType.GetCheckConstraints(), x => x.Name == "ck_supplier_bill_amounts");
        Assert.Contains(entityType.GetCheckConstraints(), x => x.Name == "ck_supplier_bill_status");
        Assert.Contains(entityType.GetCheckConstraints(), x => x.Name == "ck_supplier_bill_source");
    }

    [Fact]
    public void SupplierBillDetail_ConfiguresSourceUniquenessAndPrecision()
    {
        var entityType = GetEntityType<SupplierBillDetail>();
        var sourceIndex = entityType.GetIndexes().Single(
            x => x.GetDatabaseName() == "idx_supplier_bill_detail_source_detail");

        Assert.True(sourceIndex.IsUnique);
        Assert.Equal(
            [nameof(SupplierBillDetail.SourceType), nameof(SupplierBillDetail.SourceDetailId)],
            sourceIndex.Properties.Select(x => x.Name));
        Assert.Equal(NumericPrecision.QuantityScale, entityType.FindProperty(nameof(SupplierBillDetail.Quantity))!.GetScale());
        Assert.Equal(NumericPrecision.MoneyScale, entityType.FindProperty(nameof(SupplierBillDetail.Amount))!.GetScale());
    }

    [Fact]
    public void SupplierSettlement_ConfiguresStatusPrecisionAndUniqueNo()
    {
        var entityType = GetEntityType<SupplierSettlement>();

        Assert.True(entityType.GetIndexes().Single(x => x.GetDatabaseName() == "idx_supplier_settlement_no").IsUnique);
        Assert.Equal(SupplierSettlementStatus.Pending, entityType.FindProperty(nameof(SupplierSettlement.SettlementStatus))!.GetDefaultValue());
        Assert.Equal((SupplierSettlementStatus)0, entityType.FindProperty(nameof(SupplierSettlement.SettlementStatus))!.Sentinel);
        Assert.Contains(entityType.GetCheckConstraints(), x => x.Name == "ck_supplier_settlement_amounts");
        Assert.Contains(entityType.GetCheckConstraints(), x => x.Name == "ck_supplier_settlement_status");
    }

    [Fact]
    public void SupplierSettlementDetail_ConfiguresBillUniqueAndPrecision()
    {
        var entityType = GetEntityType<SupplierSettlementDetail>();
        var uniqueIndex = entityType.GetIndexes().Single(
            x => x.GetDatabaseName() == "idx_supplier_settlement_detail_settlement_bill");

        Assert.True(uniqueIndex.IsUnique);
        Assert.Equal(
            [nameof(SupplierSettlementDetail.SupplierSettlementId), nameof(SupplierSettlementDetail.SupplierBillId)],
            uniqueIndex.Properties.Select(x => x.Name));
        Assert.Contains(entityType.GetCheckConstraints(), x => x.Name == "ck_supplier_settlement_detail_amounts");
    }

    [Fact]
    public void SupplierBillRelationships_CascadeDetailsAndProtectSourceHistory()
    {
        AssertForeignKey<SupplierBill, Supplier>(nameof(SupplierBill.SupplierId), DeleteBehavior.Restrict);
        AssertForeignKey<SupplierBill, StockInOrder>(nameof(SupplierBill.StockInOrderId), DeleteBehavior.Restrict);
        AssertForeignKey<SupplierBill, StockOutOrder>(nameof(SupplierBill.StockOutOrderId), DeleteBehavior.Restrict);
        AssertForeignKey<SupplierBillDetail, SupplierBill>(nameof(SupplierBillDetail.SupplierBillId), DeleteBehavior.Cascade);
        AssertForeignKey<SupplierBillDetail, GoodsEntity>(nameof(SupplierBillDetail.GoodsId), DeleteBehavior.Restrict);
        AssertForeignKey<SupplierSettlement, Supplier>(nameof(SupplierSettlement.SupplierId), DeleteBehavior.Restrict);
        AssertForeignKey<SupplierSettlementDetail, SupplierBill>(nameof(SupplierSettlementDetail.SupplierBillId), DeleteBehavior.Restrict);
    }

    private IEntityType GetEntityType<T>() where T : class => model.FindEntityType(typeof(T))!;

    private void AssertForeignKey<TEntity, TPrincipal>(string foreignKeyProperty, DeleteBehavior expectedDeleteBehavior)
        where TEntity : class
        where TPrincipal : class
    {
        var foreignKey = GetEntityType<TEntity>().GetForeignKeys()
            .Single(x => x.Properties.Any(p => p.Name == foreignKeyProperty));
        Assert.Equal(typeof(TPrincipal), foreignKey.PrincipalEntityType.ClrType);
        Assert.Equal(expectedDeleteBehavior, foreignKey.DeleteBehavior);
    }
}
