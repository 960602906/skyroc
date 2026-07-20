using Domain.Entities.Storage;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using SkyRoc.Tests.Testing.PostgreSql;
using Xunit;

namespace SkyRoc.Tests.Storage;

/// <summary>
///     T7 收口切片：在专用 PostgreSQL 上核对成本/批次/流水/锁定（占用）/盘点一致性与负库存门禁，并输出质量报告。
/// </summary>
[Collection(PostgreSqlTestCollection.Name)]
public class StockConsistencyClosurePostgreSqlTests(PostgreSqlTestFixture fixture)
{
    private const decimal QuantityTolerance = 0.000001m;
    private const decimal MoneyTolerance = 0.0001m;

    /// <summary>
    ///     全库批次数量与成本非负、可用≤账面、占用=账面-可用；流水净额与最新余额对齐批次；
    ///     已审核盘点差异与盘点台账一一对应；质量报告负库存门禁与联调验收通过；无临时残留。
    /// </summary>
    [Fact]
    public async Task Stock_CostBatchLedgerLockStocktakingConsistency_OnPostgreSql()
    {
        var batch = TestBatchContext.Create();

        // 收口切片只读核对既有联调库存；不调用全量生成器（菜单按钮等漂移属联调基线问题，不在本切片修复）
        await using (var context = fixture.CreateDbContext())
        {
            var managedWareCode = DemoDataStableKeyCatalog.Create("WARE", 1);
            Assert.True(
                await context.Wares.AsNoTracking().AnyAsync(item => item.Code == managedWareCode),
                $"缺少受管仓库 {managedWareCode}，请先完成 T2 联调数据基线");

            var stockBatches = await context.StockBatches.AsNoTracking().ToListAsync();
            Assert.NotEmpty(stockBatches);

            var ledgers = await context.StockLedgers.AsNoTracking()
                .OrderBy(item => item.OccurredTime)
                .ThenBy(item => item.CreateTime)
                .ThenBy(item => item.Id)
                .ToListAsync();
            Assert.NotEmpty(ledgers);

            var ledgersByBatch = ledgers
                .GroupBy(item => item.StockBatchId)
                .ToDictionary(group => group.Key, group => group.ToList());

            foreach (var stockBatch in stockBatches)
            {
                Assert.True(
                    stockBatch.CurrentQuantity >= -QuantityTolerance,
                    $"批次 {stockBatch.BatchNo} 账面数量为负：{stockBatch.CurrentQuantity}");
                Assert.True(
                    stockBatch.AvailableQuantity >= -QuantityTolerance,
                    $"批次 {stockBatch.BatchNo} 可用数量为负：{stockBatch.AvailableQuantity}");
                Assert.True(
                    stockBatch.AvailableQuantity <= stockBatch.CurrentQuantity + QuantityTolerance,
                    $"批次 {stockBatch.BatchNo} 可用数量超过账面：可用 {stockBatch.AvailableQuantity}，账面 {stockBatch.CurrentQuantity}");
                Assert.True(
                    stockBatch.UnitCost >= -MoneyTolerance,
                    $"批次 {stockBatch.BatchNo} 单位成本为负：{stockBatch.UnitCost}");

                var occupied = NumericPrecision.RoundQuantity(
                    stockBatch.CurrentQuantity - stockBatch.AvailableQuantity);
                Assert.True(
                    occupied >= -QuantityTolerance,
                    $"批次 {stockBatch.BatchNo} 占用数量为负：{occupied}");

                if (!ledgersByBatch.TryGetValue(stockBatch.Id, out var batchLedgers) || batchLedgers.Count == 0)
                {
                    Assert.True(
                        stockBatch.CurrentQuantity <= QuantityTolerance
                        && stockBatch.AvailableQuantity <= QuantityTolerance,
                        $"批次 {stockBatch.BatchNo} 无流水却仍有库存：账面 {stockBatch.CurrentQuantity}");
                    continue;
                }

                var netQuantity = batchLedgers.Sum(ledger =>
                    ledger.Direction == StockLedgerDirection.Increase
                        ? ledger.ChangeQuantity
                        : -ledger.ChangeQuantity);
                Assert.True(
                    Math.Abs(NumericPrecision.RoundQuantity(netQuantity) - stockBatch.CurrentQuantity)
                    <= QuantityTolerance,
                    $"批次 {stockBatch.BatchNo} 流水净额 {netQuantity} 与账面 {stockBatch.CurrentQuantity} 不一致");

                var latestLedger = batchLedgers[^1];
                Assert.True(
                    Math.Abs(latestLedger.BalanceQuantity - stockBatch.CurrentQuantity) <= QuantityTolerance,
                    $"批次 {stockBatch.BatchNo} 最新流水余额 {latestLedger.BalanceQuantity} 与账面 {stockBatch.CurrentQuantity} 不一致");
            }

            Assert.All(ledgers, ledger =>
            {
                Assert.True(ledger.ChangeQuantity > 0m, $"流水 {ledger.Id} 变更数量必须为正");
                Assert.True(ledger.UnitCost >= -MoneyTolerance, $"流水 {ledger.Id} 单位成本为负");
                var expectedTotalCost = NumericPrecision.RoundMoney(ledger.ChangeQuantity * ledger.UnitCost);
                Assert.True(
                    Math.Abs(ledger.TotalCost - expectedTotalCost) <= MoneyTolerance,
                    $"流水 {ledger.Id} 总成本 {ledger.TotalCost} 与数量×单价 {expectedTotalCost} 不一致");
            });

            var stocktakingOrders = await context.StocktakingOrders.AsNoTracking()
                .Include(order => order.Details)
                .Where(order => order.BusinessStatus == StockDocumentStatus.Audited
                                && order.IsAdjustmentApplied)
                .ToListAsync();

            foreach (var order in stocktakingOrders)
            {
                var orderLedgers = ledgers
                    .Where(ledger => ledger.SourceOrderId == order.Id
                                     && ledger.SourceType == StockLedgerSourceType.Stocktaking)
                    .ToList();

                foreach (var detail in order.Details)
                {
                    var difference = NumericPrecision.RoundQuantity(detail.DifferenceQuantity);
                    var detailLedgers = orderLedgers
                        .Where(ledger => ledger.SourceDetailId == detail.Id)
                        .ToList();

                    if (Math.Abs(difference) <= QuantityTolerance)
                    {
                        Assert.Empty(detailLedgers);
                        continue;
                    }

                    var ledger = Assert.Single(detailLedgers);
                    Assert.Equal(detail.StockBatchId, ledger.StockBatchId);
                    Assert.Equal(
                        Math.Abs(difference),
                        NumericPrecision.RoundQuantity(ledger.ChangeQuantity));
                    Assert.Equal(
                        difference > 0m ? StockLedgerDirection.Increase : StockLedgerDirection.Decrease,
                        ledger.Direction);
                }
            }

            Assert.DoesNotContain(
                stockBatches,
                item => item.CurrentQuantity < -QuantityTolerance || item.AvailableQuantity < -QuantityTolerance);
        }

        var quality = await fixture.GenerateQualityReportAsync(batch.Id);
        Assert.Equal(fixture.DatabaseName, quality.Report.DatabaseName);
        Assert.True(quality.Report.BusinessConsistencyChecks["stockBatchQuantitiesAreNonNegative"]);
        Assert.True(quality.Report.BusinessConsistencyChecks["temporaryBatchResidueIsZero"]);
        Assert.True(quality.Report.BusinessConsistencyChecks["foreignKeysAreValidated"]);
        Assert.Empty(quality.Report.TemporaryResidues);
        Assert.Empty(quality.Report.OrphanForeignKeys);
        Assert.Empty(quality.Report.DuplicateBusinessCodes);
        Assert.Empty(quality.Report.MetadataFindings);
        Assert.True(quality.Report.DemoDataAcceptance.IsReady);
        Assert.True(File.Exists(quality.Paths.JsonPath));
        Assert.True(File.Exists(quality.Paths.MarkdownPath));
    }
}
