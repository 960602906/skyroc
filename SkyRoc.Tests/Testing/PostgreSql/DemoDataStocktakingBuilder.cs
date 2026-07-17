using Application.DTOs.Storage;
using Application.Interfaces;
using Domain.Entities.Storage;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace SkyRoc.Tests.Testing.PostgreSql;

/// <summary>
///     基于完整稳定备注和受管采购批次补齐库存盘点，并经应用服务形成真实盘盈盘亏流水。
/// </summary>
internal sealed class DemoDataStocktakingBuilder(
    ApplicationDbContext context,
    IStocktakingService stocktakingService,
    Guid auditUserId,
    string auditUsername)
{
    private const int OrderCount = 60;
    private const int AuditedOrderCount = 40;
    private const int SourcePairCount = 20;
    private const int DetailsPerOrder = 2;
    private const decimal AdjustmentQuantity = 0.125m;
    private const string ManagedOrderPrefix = "SKYROC-DEMO-STOCKTAKING-";

    /// <summary>
    ///     补齐六十张盘点单及一百二十条明细，覆盖盘盈、盘亏、零差异草稿与审核调整。
    /// </summary>
    public async Task<DemoDataStocktakingGenerationResult> GenerateAsync(
        CancellationToken cancellationToken)
    {
        var expectedRemarks = Enumerable.Range(1, OrderCount)
            .Select(CreateOrderRemark)
            .ToArray();
        var candidates = await context.StocktakingOrders
            .AsNoTracking()
            .Include(order => order.Details)
            .Where(order => order.Remark != null && order.Remark.StartsWith(ManagedOrderPrefix))
            .OrderBy(order => order.Remark)
            .ToArrayAsync(cancellationToken);
        EnsureExactKeys(
            candidates.Select(order => order.Remark!),
            expectedRemarks,
            "库存盘点备注");
        var existingByRemark = candidates.ToDictionary(order => order.Remark!, StringComparer.Ordinal);
        var sources = await LoadSourcePairsAsync(cancellationToken);
        var createdOrders = 0;
        var reusedOrders = 0;
        var createdDetails = 0;
        var reusedDetails = 0;
        var createdLedgers = 0;
        var reusedLedgers = 0;

        for (var sequence = 1; sequence <= OrderCount; sequence++)
        {
            var expectedRemark = CreateOrderRemark(sequence);
            var sourcePair = sources[(sequence - 1) % SourcePairCount];
            var orderWasCreated = !existingByRemark.TryGetValue(expectedRemark, out var order);
            var existingLedgerIds = orderWasCreated
                ? new HashSet<Guid>()
                : await context.StockLedgers
                    .AsNoTracking()
                    .Where(ledger => ledger.SourceType == StockLedgerSourceType.Stocktaking
                                     && ledger.SourceOrderId == order!.Id)
                    .Select(ledger => ledger.Id)
                    .ToHashSetAsync(cancellationToken);

            if (orderWasCreated)
            {
                var currentSources = await LoadCurrentSourcesAsync(sourcePair, cancellationToken);
                var created = await stocktakingService.CreateAsync(CreateDto(sequence, currentSources));
                order = await LoadOrderAsync(created.Id, cancellationToken);
                createdOrders++;
                createdDetails += order.Details.Count;
            }
            else
            {
                ValidateOrderSnapshot(order!, sourcePair, sequence);
                reusedOrders++;
                reusedDetails += order!.Details.Count;
            }

            if (sequence <= AuditedOrderCount
                && order!.BusinessStatus is StockDocumentStatus.Draft or StockDocumentStatus.PendingAudit)
            {
                ValidateOrderSnapshot(order, sourcePair, sequence);
                await stocktakingService.AuditAsync(order.Id, CreateAuditRemark(sequence));
                order = await LoadOrderAsync(order.Id, cancellationToken);
            }

            var ledgers = await LoadLedgersAsync(order!.Id, cancellationToken);
            ValidateFinalState(order, sourcePair, ledgers, sequence);
            createdLedgers += ledgers.Count(ledger => !existingLedgerIds.Contains(ledger.Id));
            reusedLedgers += ledgers.Count(ledger => existingLedgerIds.Contains(ledger.Id));
        }

        return new DemoDataStocktakingGenerationResult(
            createdOrders,
            reusedOrders,
            createdDetails,
            reusedDetails,
            createdLedgers,
            reusedLedgers);
    }

    private async Task<IReadOnlyList<StocktakingSourcePair>> LoadSourcePairsAsync(
        CancellationToken cancellationToken)
    {
        var expectedBatchNos = Enumerable.Range(1, SourcePairCount)
            .SelectMany(sequence => Enumerable.Range(1, DetailsPerOrder)
                .Select(detailSequence => CreatePurchaseBatchNo(sequence, detailSequence)))
            .ToArray();
        var batches = await context.StockBatches
            .AsNoTracking()
            .Include(batch => batch.Ware)
            .Where(batch => expectedBatchNos.Contains(batch.BatchNo))
            .OrderBy(batch => batch.BatchNo)
            .ToArrayAsync(cancellationToken);
        EnsureExactKeys(batches.Select(batch => batch.BatchNo), expectedBatchNos, "采购库存批次号");
        if (batches.Length != SourcePairCount * DetailsPerOrder)
        {
            throw new InvalidOperationException(
                $"库存盘点需要 {SourcePairCount * DetailsPerOrder} 个受管采购批次，当前为 {batches.Length} 个。");
        }

        var byBatchNo = batches.ToDictionary(batch => batch.BatchNo, StringComparer.Ordinal);
        var pairs = new List<StocktakingSourcePair>(SourcePairCount);
        for (var sequence = 1; sequence <= SourcePairCount; sequence++)
        {
            var pairBatches = Enumerable.Range(1, DetailsPerOrder)
                .Select(detailSequence => byBatchNo[CreatePurchaseBatchNo(sequence, detailSequence)])
                .OrderBy(batch => batch.BatchNo, StringComparer.Ordinal)
                .ToArray();
            if (pairBatches.Select(batch => batch.WareId).Distinct().Count() != 1)
            {
                throw new InvalidOperationException(
                    $"受管采购批次组 {sequence:D2} 不属于同一仓库，不能安全用于盘点。");
            }

            foreach (var batch in pairBatches)
            {
                ValidateSourceBatch(batch);
            }

            pairs.Add(new StocktakingSourcePair(
                pairBatches[0].WareId,
                pairBatches[0].Ware.Name,
                pairBatches.Select(batch => batch.Id).ToArray(),
                pairBatches.Select(batch => batch.BatchNo).ToArray()));
        }

        return pairs;
    }

    private async Task<IReadOnlyList<StockBatch>> LoadCurrentSourcesAsync(
        StocktakingSourcePair sourcePair,
        CancellationToken cancellationToken)
    {
        context.ChangeTracker.Clear();
        var batches = await context.StockBatches
            .AsNoTracking()
            .Where(batch => sourcePair.BatchIds.Contains(batch.Id))
            .OrderBy(batch => batch.BatchNo)
            .ToArrayAsync(cancellationToken);
        if (batches.Length != DetailsPerOrder)
        {
            throw new InvalidOperationException(
                $"盘点来源批次 {string.Join("、", sourcePair.BatchNos)} 已缺失，拒绝创建不完整盘点单。");
        }

        foreach (var batch in batches)
        {
            ValidateSourceBatch(batch);
        }

        return batches;
    }

    private async Task<StocktakingOrder> LoadOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        context.ChangeTracker.Clear();
        return await context.StocktakingOrders
            .AsNoTracking()
            .Include(order => order.Details)
            .SingleAsync(order => order.Id == orderId, cancellationToken);
    }

    private async Task<IReadOnlyList<StockLedger>> LoadLedgersAsync(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        return await context.StockLedgers
            .AsNoTracking()
            .Where(ledger => ledger.SourceType == StockLedgerSourceType.Stocktaking
                             && ledger.SourceOrderId == orderId)
            .OrderBy(ledger => ledger.SourceDetailId)
            .ToArrayAsync(cancellationToken);
    }

    private static CreateStocktakingDto CreateDto(
        int sequence,
        IReadOnlyList<StockBatch> batches)
    {
        var expectedDifference = GetExpectedDifference(sequence);
        return new CreateStocktakingDto
        {
            WareId = batches[0].WareId,
            Remark = CreateOrderRemark(sequence),
            Details = batches
                .Select((batch, detailIndex) => new CreateStocktakingDetailDto
                {
                    StockBatchId = batch.Id,
                    ActualQuantity = NumericPrecision.RoundQuantity(
                        batch.CurrentQuantity + expectedDifference),
                    Remark = CreateDetailRemark(sequence, detailIndex + 1, expectedDifference)
                })
                .ToList()
        };
    }

    private void ValidateOrderSnapshot(
        StocktakingOrder order,
        StocktakingSourcePair sourcePair,
        int sequence)
    {
        var expectedDifference = GetExpectedDifference(sequence);
        var expectedDetailRemarks = Enumerable.Range(1, DetailsPerOrder)
            .Select(detailSequence => CreateDetailRemark(sequence, detailSequence, expectedDifference))
            .ToArray();
        if (string.IsNullOrWhiteSpace(order.StocktakingNo)
            || order.Remark != CreateOrderRemark(sequence)
            || order.WareId != sourcePair.WareId
            || order.WareNameSnapshot != sourcePair.WareName
            || order.StocktakingTime.Kind != DateTimeKind.Utc
            || order.Details.Count != DetailsPerOrder
            || order.Details.Select(detail => detail.StockBatchId).Order().SequenceEqual(sourcePair.BatchIds.Order()) == false
            || order.Details.Select(detail => detail.BatchNoSnapshot).Order().SequenceEqual(sourcePair.BatchNos.Order(StringComparer.Ordinal)) == false
            || order.Details.Select(detail => detail.Remark).Order().SequenceEqual(expectedDetailRemarks.Order(StringComparer.Ordinal)) == false
            || order.Details.Any(detail => detail.DifferenceQuantity != expectedDifference)
            || order.Details.Any(detail => detail.ActualQuantity != NumericPrecision.RoundQuantity(
                detail.BookQuantity + expectedDifference))
            || order.Details.Any(detail => detail.DifferenceAmount != NumericPrecision.RoundMoney(
                detail.DifferenceQuantity * detail.UnitCost))
            || order.TotalBookQuantity != NumericPrecision.RoundQuantity(order.Details.Sum(detail => detail.BookQuantity))
            || order.TotalActualQuantity != NumericPrecision.RoundQuantity(order.Details.Sum(detail => detail.ActualQuantity))
            || order.TotalDifferenceQuantity != NumericPrecision.RoundQuantity(order.Details.Sum(detail => detail.DifferenceQuantity))
            || order.CreateTime is null
            || order.CreateBy != auditUserId
            || order.CreateName != auditUsername
            || order.Status != Status.Enable
            || order.Details.Any(detail => detail.CreateTime is null
                                           || detail.CreateBy != auditUserId
                                           || detail.CreateName != auditUsername
                                           || detail.Status != Status.Enable
                                           || string.IsNullOrWhiteSpace(detail.GoodsNameSnapshot)
                                           || string.IsNullOrWhiteSpace(detail.GoodsCodeSnapshot)
                                           || string.IsNullOrWhiteSpace(detail.BaseUnitNameSnapshot)))
        {
            throw new InvalidOperationException(
                $"受管库存盘点 {CreateOrderRemark(sequence)} 的来源、账实快照、差异或创建审计已漂移。");
        }
    }

    private void ValidateFinalState(
        StocktakingOrder order,
        StocktakingSourcePair sourcePair,
        IReadOnlyList<StockLedger> ledgers,
        int sequence)
    {
        ValidateOrderSnapshot(order, sourcePair, sequence);
        var shouldBeAudited = sequence <= AuditedOrderCount;
        if (shouldBeAudited)
        {
            if (order.BusinessStatus != StockDocumentStatus.Audited
                || !order.IsAdjustmentApplied
                || !order.AdjustmentTime.HasValue
                || order.AuditUserId != auditUserId
                || order.AuditUserNameSnapshot != auditUsername
                || !order.AuditTime.HasValue
                || order.AuditTime != order.AdjustmentTime
                || order.ReverseUserId.HasValue
                || order.ReverseUserNameSnapshot is not null
                || order.ReverseTime.HasValue
                || order.UpdateTime is null
                || order.UpdateBy != auditUserId
                || order.UpdateName != auditUsername
                || ledgers.Count != DetailsPerOrder)
            {
                throw new InvalidOperationException(
                    $"受管库存盘点 {CreateOrderRemark(sequence)} 的审核状态、调整时间或更新审计已漂移。");
            }

            foreach (var detail in order.Details)
            {
                var ledger = ledgers.SingleOrDefault(item => item.SourceDetailId == detail.Id)
                             ?? throw new InvalidOperationException(
                                 $"受管库存盘点 {CreateOrderRemark(sequence)} 的明细 {detail.Id} 缺少调整流水。");
                var expectedDirection = detail.DifferenceQuantity > 0m
                    ? StockLedgerDirection.Increase
                    : StockLedgerDirection.Decrease;
                if (ledger.StockBatchId != detail.StockBatchId
                    || ledger.WareId != order.WareId
                    || ledger.WareNameSnapshot != order.WareNameSnapshot
                    || ledger.GoodsId != detail.GoodsId
                    || ledger.GoodsNameSnapshot != detail.GoodsNameSnapshot
                    || ledger.GoodsCodeSnapshot != detail.GoodsCodeSnapshot
                    || ledger.BatchNoSnapshot != detail.BatchNoSnapshot
                    || ledger.BaseUnitNameSnapshot != detail.BaseUnitNameSnapshot
                    || ledger.Direction != expectedDirection
                    || ledger.ChangeQuantity != Math.Abs(detail.DifferenceQuantity)
                    || ledger.UnitCost != detail.UnitCost
                    || ledger.TotalCost != NumericPrecision.RoundMoney(
                        ledger.ChangeQuantity * ledger.UnitCost)
                    || ledger.OccurredTime != order.AdjustmentTime
                    || ledger.ReversedFromLedgerId.HasValue
                    || ledger.Remark != CreateAuditRemark(sequence)
                    || ledger.CreateTime is null
                    || ledger.CreateBy != auditUserId
                    || ledger.CreateName != auditUsername
                    || ledger.Status != Status.Enable)
                {
                    throw new InvalidOperationException(
                        $"受管库存盘点 {CreateOrderRemark(sequence)} 的调整流水来源、方向、金额或审计已漂移。");
                }
            }
        }
        else if (order.BusinessStatus != StockDocumentStatus.Draft
                 || order.IsAdjustmentApplied
                 || order.AdjustmentTime.HasValue
                 || order.AuditUserId.HasValue
                 || order.AuditUserNameSnapshot is not null
                 || order.AuditTime.HasValue
                 || order.ReverseUserId.HasValue
                 || order.ReverseUserNameSnapshot is not null
                 || order.ReverseTime.HasValue
                 || order.UpdateTime is not null
                 || order.UpdateBy.HasValue
                 || order.UpdateName is not null
                 || ledgers.Count != 0)
        {
            throw new InvalidOperationException(
                $"零差异受管库存盘点 {CreateOrderRemark(sequence)} 不应伪造审核或库存调整事实。");
        }
    }

    private static void ValidateSourceBatch(StockBatch batch)
    {
        if (batch.WareId == Guid.Empty
            || batch.GoodsId == Guid.Empty
            || batch.BaseUnitId == Guid.Empty
            || string.IsNullOrWhiteSpace(batch.GoodsNameSnapshot)
            || string.IsNullOrWhiteSpace(batch.GoodsCodeSnapshot)
            || string.IsNullOrWhiteSpace(batch.BatchNo)
            || string.IsNullOrWhiteSpace(batch.BaseUnitNameSnapshot)
            || batch.CurrentQuantity < 0m
            || batch.AvailableQuantity < 0m
            || batch.UnitCost < 0m
            || batch.Status != Status.Enable)
        {
            throw new InvalidOperationException(
                $"受管采购库存批次 {batch.BatchNo} 的归属、快照、余额、成本或启用状态已漂移。");
        }
    }

    private static void EnsureExactKeys(
        IEnumerable<string> actualKeys,
        IReadOnlyCollection<string> expectedKeys,
        string keyName)
    {
        var actual = actualKeys.ToArray();
        var duplicates = actual
            .GroupBy(key => key, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (duplicates.Length > 0)
        {
            throw new InvalidOperationException(
                $"检测到重复受管{keyName}：{string.Join("、", duplicates)}。");
        }

        var unknown = actual
            .Where(key => !expectedKeys.Contains(key, StringComparer.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (unknown.Length > 0)
        {
            throw new InvalidOperationException(
                $"检测到未知受管{keyName}：{string.Join("、", unknown)}。");
        }
    }

    private static decimal GetExpectedDifference(int sequence)
    {
        return sequence switch
        {
            <= SourcePairCount => AdjustmentQuantity,
            <= AuditedOrderCount => -AdjustmentQuantity,
            _ => 0m
        };
    }

    private static string CreatePurchaseBatchNo(int sequence, int detailSequence)
    {
        return $"{DemoDataStableKeyCatalog.Create("PURCHASE-BATCH", sequence)}-{detailSequence:D2}";
    }

    private static string CreateOrderRemark(int sequence)
    {
        var stableKey = DemoDataStableKeyCatalog.Create("STOCKTAKING", sequence);
        return $"{stableKey} 华东联调库存盘点{sequence:D2}：核对受管采购批次账实数量与调整流水。";
    }

    private static string CreateDetailRemark(
        int sequence,
        int detailSequence,
        decimal expectedDifference)
    {
        var differenceDescription = expectedDifference switch
        {
            > 0m => "冷库复核发现到货称重盘盈",
            < 0m => "复盘扣除分拣损耗后确认盘亏",
            _ => "现场复核账实相符并保留草稿"
        };
        return $"SkyRoc 联调盘点明细：第 {sequence:D2} 张盘点第 {detailSequence} 行，{differenceDescription}。";
    }

    private static string CreateAuditRemark(int sequence)
    {
        return $"SkyRoc 联调盘点审核：确认第 {sequence:D2} 张受管盘点差异并生成库存调整流水。";
    }

    private sealed record StocktakingSourcePair(
        Guid WareId,
        string WareName,
        Guid[] BatchIds,
        string[] BatchNos);
}
