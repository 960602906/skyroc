using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Orders;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using Domain.Entities.Traceability;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Shared.Constants;
using Xunit;
using GoodsEntity = Domain.Entities.Goods.Goods;

namespace SkyRoc.Tests.Traceability;

/// <summary>
/// 校验溯源模型的表映射、枚举取值、默认值、精度、约束和历史数据保护关系。
/// </summary>
public class TraceabilityModelTests
{
    private readonly IModel model;

    /// <summary>
    /// 构建设计期 EF Core 模型用于结构断言，不建立真实数据库连接。
    /// </summary>
    public TraceabilityModelTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=skyroc_model_tests;Username=test;Password=test")
            .Options;
        using var context = new ApplicationDbContext(options);
        model = context.GetService<IDesignTimeModel>().Model;
    }

    [Fact]
    public void TraceabilityEntities_MapToExpectedTables()
    {
        Assert.Equal("inspection_report", GetEntityType<InspectionReport>().GetTableName());
        Assert.Equal("inspection_report_goods", GetEntityType<InspectionReportGoods>().GetTableName());
        Assert.Equal("inspection_attachment", GetEntityType<InspectionAttachment>().GetTableName());
        Assert.Equal("trace_record", GetEntityType<TraceRecord>().GetTableName());
        Assert.Equal("external_push_log", GetEntityType<ExternalPushLog>().GetTableName());
    }

    [Fact]
    public void TraceabilityEnums_UseDocumentedBusinessValues()
    {
        Assert.Equal(1, (int)InspectionConclusion.Pending);
        Assert.Equal(2, (int)InspectionConclusion.Qualified);
        Assert.Equal(3, (int)InspectionConclusion.Unqualified);
        Assert.Equal(1, (int)InspectionAttachmentType.Report);
        Assert.Equal(2, (int)InspectionAttachmentType.Image);
        Assert.Equal(1, (int)ExternalPushBusinessType.SaleOrder);
        Assert.Equal(2, (int)ExternalPushBusinessType.InspectionReport);
        Assert.Equal(3, (int)ExternalPushBusinessType.TraceRecord);
        Assert.Equal(1, (int)ExternalPushStatus.Pending);
        Assert.Equal(2, (int)ExternalPushStatus.Success);
        Assert.Equal(3, (int)ExternalPushStatus.Failed);
    }

    [Fact]
    public void InspectionReport_ConfiguresUniqueNumberDefaultConclusionAndConstraints()
    {
        var entityType = GetEntityType<InspectionReport>();

        Assert.Equal(
            InspectionConclusion.Pending,
            entityType.FindProperty(nameof(InspectionReport.Conclusion))!.GetDefaultValue());
        Assert.True(entityType.GetIndexes().Single(x => x.GetDatabaseName() == "idx_inspection_report_no").IsUnique);
        Assert.Contains(entityType.GetCheckConstraints(), x => x.Name == "ck_inspection_report_conclusion");
        Assert.Contains(
            entityType.GetIndexes(),
            x => x.GetDatabaseName() == "idx_inspection_report_stock_in_order_id");
        Assert.Contains(
            entityType.GetIndexes(),
            x => x.GetDatabaseName() == "idx_inspection_report_conclusion_time");
    }

    [Fact]
    public void InspectionReportGoods_ConfiguresGlobalPrecisionDefaultsAndSourceUniqueness()
    {
        var entityType = GetEntityType<InspectionReportGoods>();

        Assert.Equal(
            NumericPrecision.QuantityScale,
            entityType.FindProperty(nameof(InspectionReportGoods.SampleQuantity))!.GetScale());
        Assert.Equal(
            InspectionConclusion.Pending,
            entityType.FindProperty(nameof(InspectionReportGoods.Conclusion))!.GetDefaultValue());
        Assert.Contains(entityType.GetCheckConstraints(), x => x.Name == "ck_inspection_report_goods_quantity");
        Assert.Contains(entityType.GetCheckConstraints(), x => x.Name == "ck_inspection_report_goods_conclusion");

        var sourceIndex = entityType.GetIndexes().Single(
            x => x.GetDatabaseName() == "idx_inspection_report_goods_source");
        Assert.True(sourceIndex.IsUnique);
        Assert.Equal(
            [nameof(InspectionReportGoods.InspectionReportId), nameof(InspectionReportGoods.StockInDetailId)],
            sourceIndex.Properties.Select(x => x.Name));
    }

    [Fact]
    public void InspectionAttachment_ConstrainsTypeAndFileSize()
    {
        var entityType = GetEntityType<InspectionAttachment>();

        Assert.Contains(entityType.GetCheckConstraints(), x => x.Name == "ck_inspection_attachment_type");
        Assert.Contains(entityType.GetCheckConstraints(), x => x.Name == "ck_inspection_attachment_file_size");
        Assert.Contains(
            entityType.GetIndexes(),
            x => x.GetDatabaseName() == "idx_inspection_attachment_report_sort");
    }

    [Fact]
    public void TraceRecord_EnforcesUniqueTraceNoAndOneRecordPerOrderDetail()
    {
        var entityType = GetEntityType<TraceRecord>();

        Assert.True(entityType.GetIndexes().Single(x => x.GetDatabaseName() == "idx_trace_record_no").IsUnique);
        Assert.True(entityType.GetIndexes().Single(
            x => x.GetDatabaseName() == "idx_trace_record_sale_order_detail_id").IsUnique);
    }

    [Fact]
    public void ExternalPushLog_DefaultsToPendingConstrainsValuesAndHasNoForeignKeys()
    {
        var entityType = GetEntityType<ExternalPushLog>();

        Assert.Equal(
            ExternalPushStatus.Pending,
            entityType.FindProperty(nameof(ExternalPushLog.PushStatus))!.GetDefaultValue());
        Assert.Equal(0, entityType.FindProperty(nameof(ExternalPushLog.RetryCount))!.GetDefaultValue());
        Assert.Contains(entityType.GetCheckConstraints(), x => x.Name == "ck_external_push_log_business_type");
        Assert.Contains(entityType.GetCheckConstraints(), x => x.Name == "ck_external_push_log_status");
        Assert.Contains(entityType.GetCheckConstraints(), x => x.Name == "ck_external_push_log_retry");
        Assert.Empty(entityType.GetForeignKeys());

        var businessIndex = entityType.GetIndexes().Single(
            x => x.GetDatabaseName() == "idx_external_push_log_business");
        Assert.Equal(
            [nameof(ExternalPushLog.BusinessType), nameof(ExternalPushLog.BusinessId)],
            businessIndex.Properties.Select(x => x.Name));
        Assert.Contains(entityType.GetIndexes(), x => x.GetDatabaseName() == "idx_external_push_log_status_time");
        Assert.Contains(entityType.GetIndexes(), x => x.GetDatabaseName() == "idx_external_push_log_platform_time");
    }

    [Fact]
    public void TraceabilityRelationships_CascadeOwnedRecordsAndProtectBusinessHistory()
    {
        AssertForeignKey<InspectionReport, StockInOrder>(nameof(InspectionReport.StockInOrderId), DeleteBehavior.Restrict);
        AssertForeignKey<InspectionReport, Ware>(nameof(InspectionReport.WareId), DeleteBehavior.Restrict);
        AssertForeignKey<InspectionReport, Supplier>(nameof(InspectionReport.SupplierId), DeleteBehavior.SetNull);
        AssertForeignKey<InspectionReportGoods, InspectionReport>(
            nameof(InspectionReportGoods.InspectionReportId), DeleteBehavior.Cascade);
        AssertForeignKey<InspectionReportGoods, StockInDetail>(
            nameof(InspectionReportGoods.StockInDetailId), DeleteBehavior.Restrict);
        AssertForeignKey<InspectionReportGoods, GoodsEntity>(nameof(InspectionReportGoods.GoodsId), DeleteBehavior.Restrict);
        AssertForeignKey<InspectionReportGoods, GoodsUnit>(nameof(InspectionReportGoods.GoodsUnitId), DeleteBehavior.Restrict);
        AssertForeignKey<InspectionAttachment, InspectionReport>(
            nameof(InspectionAttachment.InspectionReportId), DeleteBehavior.Cascade);
        AssertForeignKey<TraceRecord, SaleOrder>(nameof(TraceRecord.SaleOrderId), DeleteBehavior.Restrict);
        AssertForeignKey<TraceRecord, SaleOrderDetail>(nameof(TraceRecord.SaleOrderDetailId), DeleteBehavior.Restrict);
        AssertForeignKey<TraceRecord, Customer>(nameof(TraceRecord.CustomerId), DeleteBehavior.Restrict);
        AssertForeignKey<TraceRecord, GoodsEntity>(nameof(TraceRecord.GoodsId), DeleteBehavior.Restrict);
        AssertForeignKey<TraceRecord, StockInDetail>(nameof(TraceRecord.StockInDetailId), DeleteBehavior.Restrict);
        AssertForeignKey<TraceRecord, Supplier>(nameof(TraceRecord.SupplierId), DeleteBehavior.SetNull);
        AssertForeignKey<TraceRecord, Ware>(nameof(TraceRecord.WareId), DeleteBehavior.SetNull);
        AssertForeignKey<TraceRecord, InspectionReport>(nameof(TraceRecord.InspectionReportId), DeleteBehavior.Restrict);
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
