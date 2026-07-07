using Domain.Entities.AfterSales;
using Domain.Entities.Customers;
using Domain.Entities.Finance;
using Domain.Entities.Goods;
using Domain.Entities.Orders;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Shared.Constants;
using Xunit;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace SkyRoc.Tests.Finance;

/// <summary>
/// 校验客户账单模型的表映射、状态值、金额数量精度、来源唯一性和历史外键保护。
/// </summary>
public class CustomerBillModelTests
{
    private readonly IModel model;

    /// <summary>
    /// 构建设计期 EF Core 模型用于结构断言，不连接真实数据库。
    /// </summary>
    public CustomerBillModelTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=skyroc_model_tests;Username=test;Password=test")
            .Options;
        using var context = new ApplicationDbContext(options);
        model = context.GetService<IDesignTimeModel>().Model;
    }

    [Fact]
    public void CustomerBillEntities_MapToExpectedTables()
    {
        Assert.Equal("customer_bill", GetEntityType<CustomerBill>().GetTableName());
        Assert.Equal("customer_bill_detail", GetEntityType<CustomerBillDetail>().GetTableName());
        Assert.Equal("customer_settlement", GetEntityType<CustomerSettlement>().GetTableName());
        Assert.Equal("customer_settlement_detail", GetEntityType<CustomerSettlementDetail>().GetTableName());
    }

    [Fact]
    public void FinanceEnums_UseDocumentedBusinessValues()
    {
        Assert.Equal(1, (int)CustomerBillStatus.Pending);
        Assert.Equal(2, (int)CustomerBillStatus.PartiallySettled);
        Assert.Equal(3, (int)CustomerBillStatus.Settled);
        Assert.Equal(1, (int)CustomerBillDetailSourceType.OrderAcceptance);
        Assert.Equal(2, (int)CustomerBillDetailSourceType.AfterSaleAdjustment);
        Assert.Equal(-1, (int)CustomerSettlementStatus.Voided);
        Assert.Equal(1, (int)CustomerSettlementStatus.Pending);
        Assert.Equal(2, (int)CustomerSettlementStatus.PartiallySettled);
        Assert.Equal(3, (int)CustomerSettlementStatus.Settled);
    }

    [Fact]
    public void CustomerBill_ConfiguresUniqueOrderAndMoneyPrecision()
    {
        var entityType = GetEntityType<CustomerBill>();

        Assert.True(entityType.GetIndexes().Single(x => x.GetDatabaseName() == "idx_customer_bill_no").IsUnique);
        Assert.True(entityType.GetIndexes().Single(x => x.GetDatabaseName() == "idx_customer_bill_sale_order_id").IsUnique);
        Assert.Equal(CustomerBillStatus.Pending, entityType.FindProperty(nameof(CustomerBill.BillStatus))!.GetDefaultValue());
        Assert.Equal(NumericPrecision.MoneyScale, entityType.FindProperty(nameof(CustomerBill.OrderAmount))!.GetScale());
        Assert.Equal(NumericPrecision.MoneyScale, entityType.FindProperty(nameof(CustomerBill.AfterSaleAdjustmentAmount))!.GetScale());
        Assert.Equal(NumericPrecision.MoneyScale, entityType.FindProperty(nameof(CustomerBill.ReceivableAmount))!.GetScale());
        Assert.Equal(NumericPrecision.MoneyScale, entityType.FindProperty(nameof(CustomerBill.SettledAmount))!.GetScale());
        Assert.Contains(entityType.GetCheckConstraints(), x => x.Name == "ck_customer_bill_amounts");
        Assert.Contains(entityType.GetCheckConstraints(), x => x.Name == "ck_customer_bill_status");
    }

    [Fact]
    public void CustomerBillDetail_ConfiguresSourceUniquenessAndPrecision()
    {
        var entityType = GetEntityType<CustomerBillDetail>();
        var sourceIndex = entityType.GetIndexes().Single(
            x => x.GetDatabaseName() == "idx_customer_bill_detail_source_detail");

        Assert.True(sourceIndex.IsUnique);
        Assert.Equal(
            [nameof(CustomerBillDetail.SourceType), nameof(CustomerBillDetail.SourceDetailId)],
            sourceIndex.Properties.Select(x => x.Name));
        Assert.Equal(NumericPrecision.QuantityScale, entityType.FindProperty(nameof(CustomerBillDetail.Quantity))!.GetScale());
        Assert.Equal(NumericPrecision.QuantityScale, entityType.FindProperty(nameof(CustomerBillDetail.BaseQuantity))!.GetScale());
        Assert.Equal(NumericPrecision.QuantityScale, entityType.FindProperty(nameof(CustomerBillDetail.ConversionRate))!.GetScale());
        Assert.Equal(NumericPrecision.MoneyScale, entityType.FindProperty(nameof(CustomerBillDetail.UnitPrice))!.GetScale());
        Assert.Equal(NumericPrecision.MoneyScale, entityType.FindProperty(nameof(CustomerBillDetail.Amount))!.GetScale());
        Assert.Contains(entityType.GetCheckConstraints(), x => x.Name == "ck_customer_bill_detail_source_type");
        Assert.Contains(entityType.GetCheckConstraints(), x => x.Name == "ck_customer_bill_detail_conversion_rate");
        Assert.Contains(entityType.GetCheckConstraints(), x => x.Name == "ck_customer_bill_detail_unit_price");
        Assert.Contains(entityType.GetCheckConstraints(), x => x.Name == "ck_customer_bill_detail_source_amount");
    }

    [Fact]
    public void CustomerSettlement_ConfiguresStatusPrecisionAndUniqueNo()
    {
        var entityType = GetEntityType<CustomerSettlement>();

        Assert.True(entityType.GetIndexes().Single(x => x.GetDatabaseName() == "idx_customer_settlement_no").IsUnique);
        Assert.Equal(CustomerSettlementStatus.Pending, entityType.FindProperty(nameof(CustomerSettlement.SettlementStatus))!.GetDefaultValue());
        Assert.Equal(NumericPrecision.MoneyScale, entityType.FindProperty(nameof(CustomerSettlement.ShouldAmount))!.GetScale());
        Assert.Equal(NumericPrecision.MoneyScale, entityType.FindProperty(nameof(CustomerSettlement.PaymentAmount))!.GetScale());
        Assert.Equal(NumericPrecision.MoneyScale, entityType.FindProperty(nameof(CustomerSettlement.DiscountAmount))!.GetScale());
        Assert.Equal(NumericPrecision.MoneyScale, entityType.FindProperty(nameof(CustomerSettlement.AppliedAmount))!.GetScale());
        Assert.Equal(NumericPrecision.MoneyScale, entityType.FindProperty(nameof(CustomerSettlement.RemainingAmount))!.GetScale());
        Assert.Contains(entityType.GetCheckConstraints(), x => x.Name == "ck_customer_settlement_amounts");
        Assert.Contains(entityType.GetCheckConstraints(), x => x.Name == "ck_customer_settlement_status");
    }

    [Fact]
    public void CustomerSettlementDetail_ConfiguresBillUniquenessAndPrecision()
    {
        var entityType = GetEntityType<CustomerSettlementDetail>();
        var billIndex = entityType.GetIndexes().Single(
            x => x.GetDatabaseName() == "idx_customer_settlement_detail_settlement_bill");

        Assert.True(billIndex.IsUnique);
        Assert.Equal(
            [nameof(CustomerSettlementDetail.CustomerSettlementId), nameof(CustomerSettlementDetail.CustomerBillId)],
            billIndex.Properties.Select(x => x.Name));
        Assert.Equal(NumericPrecision.MoneyScale, entityType.FindProperty(nameof(CustomerSettlementDetail.ReceivableAmountSnapshot))!.GetScale());
        Assert.Equal(NumericPrecision.MoneyScale, entityType.FindProperty(nameof(CustomerSettlementDetail.PreviousSettledAmount))!.GetScale());
        Assert.Equal(NumericPrecision.MoneyScale, entityType.FindProperty(nameof(CustomerSettlementDetail.PaymentAmount))!.GetScale());
        Assert.Equal(NumericPrecision.MoneyScale, entityType.FindProperty(nameof(CustomerSettlementDetail.DiscountAmount))!.GetScale());
        Assert.Equal(NumericPrecision.MoneyScale, entityType.FindProperty(nameof(CustomerSettlementDetail.AppliedAmount))!.GetScale());
        Assert.Equal(NumericPrecision.MoneyScale, entityType.FindProperty(nameof(CustomerSettlementDetail.CurrentSettledAmount))!.GetScale());
        Assert.Equal(NumericPrecision.MoneyScale, entityType.FindProperty(nameof(CustomerSettlementDetail.RemainingAmount))!.GetScale());
        Assert.Contains(entityType.GetCheckConstraints(), x => x.Name == "ck_customer_settlement_detail_amounts");
    }

    [Fact]
    public void CustomerBillRelationships_CascadeDetailsAndProtectSourceHistory()
    {
        AssertForeignKey<CustomerBill, Customer>(nameof(CustomerBill.CustomerId), DeleteBehavior.Restrict);
        AssertForeignKey<CustomerBill, SaleOrder>(nameof(CustomerBill.SaleOrderId), DeleteBehavior.Restrict);
        AssertForeignKey<CustomerBillDetail, CustomerBill>(nameof(CustomerBillDetail.CustomerBillId), DeleteBehavior.Cascade);
        AssertForeignKey<CustomerBillDetail, SaleOrderDetail>(nameof(CustomerBillDetail.SaleOrderDetailId), DeleteBehavior.Restrict);
        AssertForeignKey<CustomerBillDetail, AfterSale>(nameof(CustomerBillDetail.AfterSaleId), DeleteBehavior.Restrict);
        AssertForeignKey<CustomerBillDetail, AfterSaleGoods>(nameof(CustomerBillDetail.AfterSaleGoodsId), DeleteBehavior.Restrict);
        AssertForeignKey<CustomerBillDetail, GoodsEntity>(nameof(CustomerBillDetail.GoodsId), DeleteBehavior.Restrict);
        AssertForeignKey<CustomerBillDetail, GoodsUnit>(nameof(CustomerBillDetail.GoodsUnitId), DeleteBehavior.Restrict);
        AssertForeignKey<CustomerBillDetail, GoodsUnit>(nameof(CustomerBillDetail.BaseUnitId), DeleteBehavior.SetNull);
        AssertForeignKey<CustomerSettlement, Customer>(nameof(CustomerSettlement.CustomerId), DeleteBehavior.Restrict);
        AssertForeignKey<CustomerSettlementDetail, CustomerSettlement>(nameof(CustomerSettlementDetail.CustomerSettlementId), DeleteBehavior.Cascade);
        AssertForeignKey<CustomerSettlementDetail, CustomerBill>(nameof(CustomerSettlementDetail.CustomerBillId), DeleteBehavior.Restrict);
        AssertForeignKey<CustomerSettlementDetail, SaleOrder>(nameof(CustomerSettlementDetail.SaleOrderId), DeleteBehavior.Restrict);
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
