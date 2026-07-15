using Application.DTOs.Finance;
using Application.interfaces;
using Domain.Entities.Finance;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace SkyRoc.Tests.Testing.PostgreSql;

internal sealed class DemoDataSupplierSettlementBuilder(
    ApplicationDbContext context,
    ISupplierSettlementService supplierSettlementService)
{
    internal async Task<GenerationResult> GenerateAsync(CancellationToken cancellationToken)
    {
        var result = await GenerateSupplierSettlementsAsync(
            context,
            supplierSettlementService,
            cancellationToken);
        return new GenerationResult(
            result.CreatedSettlements,
            result.ReusedSettlements,
            result.CreatedDetails,
            result.ReusedDetails);
    }

    private static async Task<(
        int CreatedSettlements,
        int ReusedSettlements,
        int CreatedDetails,
        int ReusedDetails)> GenerateSupplierSettlementsAsync(
            ApplicationDbContext context,
            ISupplierSettlementService supplierSettlementService,
            CancellationToken cancellationToken)
    {
        context.ChangeTracker.Clear();
        // 供应商结算继续只锚定采购入库 001–040 的待结单据；041–050 保留待结以覆盖状态与数量下限。
        var expectedStockInRemarks = Enumerable.Range(1, 40)
            .Select(CreatePurchaseStockInRemark)
            .OrderBy(remark => remark, StringComparer.Ordinal)
            .ToArray();
        var managedBills = await context.SupplierBills
            .AsNoTracking()
            .Include(bill => bill.StockInOrder)
            .Where(bill => bill.SourceType == SupplierBillSourceType.PurchaseStockIn
                           && bill.StockInOrder != null
                           && bill.StockInOrder.Remark != null
                           && expectedStockInRemarks.Contains(bill.StockInOrder.Remark))
            .OrderBy(bill => bill.StockInOrder!.Remark)
            .ToListAsync(cancellationToken);
        var actualStockInRemarks = managedBills
            .Select(bill => bill.StockInOrder!.Remark!)
            .OrderBy(remark => remark, StringComparer.Ordinal)
            .ToArray();
        if (!actualStockInRemarks.SequenceEqual(expectedStockInRemarks, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                $"受管供应商结算需要精确匹配 {expectedStockInRemarks.Length} 个采购入库稳定键，当前匹配 {actualStockInRemarks.Length} 个。");
        }

        var seeds = CreateSupplierSettlementSeeds(managedBills);
        var stableSerialNumbers = seeds
            .Select(seed => CreateSupplierSettlementSerialNo(seed.Sequence))
            .ToArray();
        var existingSettlements = await context.SupplierSettlements
            .AsNoTracking()
            .Include(settlement => settlement.Details)
            .Where(settlement => settlement.SerialNo != null
                                 && stableSerialNumbers.Contains(settlement.SerialNo))
            .ToListAsync(cancellationToken);
        var duplicateStableSerial = existingSettlements
            .GroupBy(settlement => settlement.SerialNo!, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateStableSerial is not null)
        {
            throw new InvalidOperationException(
                $"受管供应商结算稳定流水号 {duplicateStableSerial.Key} 存在 {duplicateStableSerial.Count()} 条冲突记录。");
        }

        var existingBySerial = existingSettlements.ToDictionary(
            settlement => settlement.SerialNo!,
            StringComparer.Ordinal);
        var managedBillIds = managedBills.Select(bill => bill.Id).ToArray();
        var hasNonManagedActiveSettlement = await context.SupplierSettlementDetails
            .AsNoTracking()
            .AnyAsync(
                detail => managedBillIds.Contains(detail.SupplierBillId)
                          && detail.SupplierSettlement.SettlementStatus != SupplierSettlementStatus.Voided
                          && (detail.SupplierSettlement.SerialNo == null
                              || !stableSerialNumbers.Contains(detail.SupplierSettlement.SerialNo)),
                cancellationToken);
        if (hasNonManagedActiveSettlement)
        {
            throw new InvalidOperationException(
                "受管供应商待结单据存在非生成器管理的有效结算凭证；为避免修改非受管财务数据，本轮已停止。");
        }

        EnsureSupplierSettlementStableKeysFormPrefix(seeds, existingBySerial);
        var seedsBySerial = seeds.ToDictionary(
            seed => CreateSupplierSettlementSerialNo(seed.Sequence),
            StringComparer.Ordinal);
        foreach (var existingSettlement in existingSettlements)
        {
            ValidateManagedSupplierSettlement(
                existingSettlement,
                seedsBySerial[existingSettlement.SerialNo!]);
        }

        ValidateManagedSupplierBillBalances(managedBills, existingSettlements);
        var createdSettlements = 0;
        var reusedSettlements = 0;
        var createdDetails = 0;
        var reusedDetails = 0;
        foreach (var seed in seeds)
        {
            var serialNo = CreateSupplierSettlementSerialNo(seed.Sequence);
            if (existingBySerial.TryGetValue(serialNo, out var existingSettlement))
            {
                ValidateManagedSupplierSettlement(existingSettlement, seed);
                if (seed.Scenario == SupplierSettlementScenario.Voided
                    && existingSettlement.SettlementStatus != SupplierSettlementStatus.Voided)
                {
                    await supplierSettlementService.VoidAsync(
                        existingSettlement.Id,
                        new VoidSupplierSettlementDto
                        {
                            Remark = CreateSupplierSettlementVoidRemark(seed.Sequence)
                        });
                    context.ChangeTracker.Clear();
                }

                reusedSettlements++;
                reusedDetails += existingSettlement.Details.Count;
                continue;
            }

            var bill = await context.SupplierBills
                .AsNoTracking()
                .SingleAsync(item => item.Id == seed.SupplierBillId, cancellationToken);
            if (NumericPrecision.RoundMoney(bill.DocumentAmount) != seed.DocumentAmount
                || NumericPrecision.RoundMoney(bill.PayableAmount) != seed.PayableAmount
                || NumericPrecision.RoundMoney(bill.SettledAmount) != seed.PreviousSettledAmount)
            {
                throw new InvalidOperationException(
                    $"受管供应商结算 {serialNo} 创建前待结单据余额与确定性快照不一致，已停止以避免重复核销。");
            }

            var created = await supplierSettlementService.CreateAsync(new CreateSupplierSettlementDto
            {
                SettlementDate = CreateSupplierSettlementDate(seed.Sequence),
                SerialNo = serialNo,
                Remark = CreateSupplierSettlementRemark(seed),
                Details =
                [
                    new CreateSupplierSettlementDetailDto
                    {
                        SupplierBillId = seed.SupplierBillId,
                        PaymentAmount = seed.PaymentAmount,
                        DiscountAmount = seed.DiscountAmount,
                        Remark = CreateSupplierSettlementDetailRemark(seed)
                    }
                ]
            });
            if (created.Details.Count != 1)
            {
                throw new InvalidOperationException(
                    $"受管供应商结算 {serialNo} 应生成 1 条明细，实际为 {created.Details.Count} 条。");
            }

            if (seed.Scenario == SupplierSettlementScenario.Voided)
            {
                await supplierSettlementService.VoidAsync(
                    created.Id,
                    new VoidSupplierSettlementDto
                    {
                        Remark = CreateSupplierSettlementVoidRemark(seed.Sequence)
                    });
            }

            context.ChangeTracker.Clear();
            createdSettlements++;
            createdDetails += created.Details.Count;
        }

        context.ChangeTracker.Clear();
        var finalSettlements = await context.SupplierSettlements
            .AsNoTracking()
            .Include(settlement => settlement.Details)
            .Where(settlement => settlement.SerialNo != null
                                 && stableSerialNumbers.Contains(settlement.SerialNo))
            .ToListAsync(cancellationToken);
        if (finalSettlements.Count != 100 || finalSettlements.Sum(settlement => settlement.Details.Count) != 100)
        {
            throw new InvalidOperationException(
                $"受管供应商结算应为 100 张凭证和 100 条明细，当前为 {finalSettlements.Count} 张凭证、{finalSettlements.Sum(settlement => settlement.Details.Count)} 条明细。");
        }

        foreach (var finalSettlement in finalSettlements)
        {
            ValidateManagedSupplierSettlement(
                finalSettlement,
                seedsBySerial[finalSettlement.SerialNo!]);
        }

        var finalBills = await context.SupplierBills
            .AsNoTracking()
            .Where(bill => managedBillIds.Contains(bill.Id))
            .OrderBy(bill => bill.SourceDocumentNoSnapshot)
            .ToListAsync(cancellationToken);
        ValidateManagedSupplierBillBalances(finalBills, finalSettlements);
        if (finalBills.Count(bill => bill.BillStatus == SupplierBillStatus.Settled) != 20
            || finalBills.Count(bill => bill.BillStatus == SupplierBillStatus.PartiallySettled) != 20)
        {
            throw new InvalidOperationException(
                "受管供应商结算完成后应保留 20 张已结待结单据和 20 张部分结款待结单据，以覆盖长期联调状态。");
        }

        return (createdSettlements, reusedSettlements, createdDetails, reusedDetails);
    }

    private static IReadOnlyList<SupplierSettlementSeed> CreateSupplierSettlementSeeds(
        IReadOnlyList<SupplierBill> managedBills)
    {
        // 001–040 先核销每张待结单据的四分之一，建立首笔部分结款快照。
        var seeds = new List<SupplierSettlementSeed>(100);
        var firstInstallmentSeeds = new SupplierSettlementSeed[managedBills.Count];
        for (var index = 0; index < managedBills.Count; index++)
        {
            var bill = managedBills[index];
            var documentAmount = NumericPrecision.RoundMoney(bill.DocumentAmount);
            var firstAppliedAmount = NumericPrecision.RoundMoney(documentAmount * 0.25m);
            var seed = CreateSupplierSettlementSeed(
                index + 1,
                bill,
                SupplierSettlementScenario.FirstInstallment,
                0m,
                firstAppliedAmount);
            firstInstallmentSeeds[index] = seed;
            seeds.Add(seed);
        }

        // 041–080 对前 20 张单据结清尾款，其余 20 张再次部分核销并保留待结余额。
        var secondInstallmentSeeds = new SupplierSettlementSeed[managedBills.Count];
        for (var index = 0; index < managedBills.Count; index++)
        {
            var bill = managedBills[index];
            var firstSeed = firstInstallmentSeeds[index];
            var pendingAmount = NumericPrecision.RoundMoney(
                firstSeed.DocumentAmount - firstSeed.CurrentSettledAmount);
            var scenario = index < 20
                ? SupplierSettlementScenario.SecondFinalInstallment
                : SupplierSettlementScenario.SecondPartialInstallment;
            var appliedAmount = scenario == SupplierSettlementScenario.SecondFinalInstallment
                ? pendingAmount
                : NumericPrecision.RoundMoney(pendingAmount * 0.5m);
            var seed = CreateSupplierSettlementSeed(
                index + 41,
                bill,
                scenario,
                firstSeed.CurrentSettledAmount,
                appliedAmount);
            secondInstallmentSeeds[index] = seed;
            seeds.Add(seed);
        }

        // 081–100 在仍有余额的 20 张单据上创建后立即作废，验证凭证保留与余额原子回滚。
        for (var index = 20; index < managedBills.Count; index++)
        {
            var bill = managedBills[index];
            var secondSeed = secondInstallmentSeeds[index];
            var pendingAmount = NumericPrecision.RoundMoney(
                secondSeed.DocumentAmount - secondSeed.CurrentSettledAmount);
            seeds.Add(CreateSupplierSettlementSeed(
                index + 61,
                bill,
                SupplierSettlementScenario.Voided,
                secondSeed.CurrentSettledAmount,
                NumericPrecision.RoundMoney(pendingAmount * 0.1m)));
        }

        return seeds;
    }

    private static SupplierSettlementSeed CreateSupplierSettlementSeed(
        int sequence,
        SupplierBill bill,
        SupplierSettlementScenario scenario,
        decimal previousSettledAmount,
        decimal appliedAmount)
    {
        var documentAmount = NumericPrecision.RoundMoney(bill.DocumentAmount);
        var payableAmount = NumericPrecision.RoundMoney(bill.PayableAmount);
        if (bill.SourceType != SupplierBillSourceType.PurchaseStockIn
            || documentAmount <= 0m
            || payableAmount != documentAmount
            || !bill.StockInOrderId.HasValue
            || bill.StockOutOrderId.HasValue)
        {
            throw new InvalidOperationException(
                $"受管供应商待结单据 {bill.BillNo} 不符合采购入库正向应付快照语义。");
        }

        var normalizedPreviousSettledAmount = NumericPrecision.RoundMoney(previousSettledAmount);
        var normalizedAppliedAmount = NumericPrecision.RoundMoney(appliedAmount);
        var shouldAmount = NumericPrecision.RoundMoney(payableAmount - normalizedPreviousSettledAmount);
        if (shouldAmount <= 0m || normalizedAppliedAmount <= 0m || normalizedAppliedAmount > shouldAmount)
        {
            throw new InvalidOperationException(
                $"受管供应商结算 {CreateSupplierSettlementSerialNo(sequence)} 的确定性金额快照不满足正余额约束。");
        }

        var discountAmount = NumericPrecision.RoundMoney(normalizedAppliedAmount * 0.1m);
        var paymentAmount = NumericPrecision.RoundMoney(normalizedAppliedAmount - discountAmount);
        if (paymentAmount <= 0m || discountAmount <= 0m)
        {
            throw new InvalidOperationException(
                $"受管供应商结算 {CreateSupplierSettlementSerialNo(sequence)} 的付款或优惠金额不满足正金额语义。");
        }

        var currentSettledAmount = NumericPrecision.RoundMoney(
            normalizedPreviousSettledAmount + normalizedAppliedAmount);
        var remainingAmount = NumericPrecision.RoundMoney(documentAmount - currentSettledAmount);
        return new SupplierSettlementSeed
        {
            Sequence = sequence,
            SupplierBillId = bill.Id,
            SupplierBillNo = bill.BillNo,
            SupplierId = bill.SupplierId,
            SupplierName = bill.SupplierNameSnapshot,
            SourceType = bill.SourceType,
            SourceDocumentNo = bill.SourceDocumentNoSnapshot,
            StockInOrderId = bill.StockInOrderId,
            StockOutOrderId = bill.StockOutOrderId,
            Scenario = scenario,
            DocumentAmount = documentAmount,
            PayableAmount = payableAmount,
            PreviousSettledAmount = normalizedPreviousSettledAmount,
            ShouldAmount = shouldAmount,
            PaymentAmount = paymentAmount,
            DiscountAmount = discountAmount,
            AppliedAmount = normalizedAppliedAmount,
            CurrentSettledAmount = currentSettledAmount,
            RemainingAmount = remainingAmount,
            CreatedStatus = remainingAmount == 0m
                ? SupplierSettlementStatus.Settled
                : SupplierSettlementStatus.PartiallySettled
        };
    }

    private static void EnsureSupplierSettlementStableKeysFormPrefix(
        IReadOnlyList<SupplierSettlementSeed> seeds,
        IReadOnlyDictionary<string, SupplierSettlement> existingBySerial)
    {
        var missingStableKeyObserved = false;
        foreach (var seed in seeds)
        {
            var serialNo = CreateSupplierSettlementSerialNo(seed.Sequence);
            if (!existingBySerial.ContainsKey(serialNo))
            {
                missingStableKeyObserved = true;
                continue;
            }

            if (missingStableKeyObserved)
            {
                throw new InvalidOperationException(
                    $"受管供应商结算 {serialNo} 之前存在稳定键缺口，无法安全重建既有财务快照。");
            }
        }
    }

    private static void ValidateManagedSupplierBillBalances(
        IReadOnlyList<SupplierBill> managedBills,
        IReadOnlyList<SupplierSettlement> managedSettlements)
    {
        var activeAppliedAmountsByBill = managedSettlements
            .Where(settlement => settlement.SettlementStatus != SupplierSettlementStatus.Voided)
            .SelectMany(settlement => settlement.Details)
            .GroupBy(detail => detail.SupplierBillId)
            .ToDictionary(
                group => group.Key,
                group => NumericPrecision.RoundMoney(group.Sum(detail => detail.AppliedAmount)));
        foreach (var bill in managedBills)
        {
            var expectedSettledAmount = activeAppliedAmountsByBill.GetValueOrDefault(bill.Id);
            var actualSettledAmount = NumericPrecision.RoundMoney(bill.SettledAmount);
            if (actualSettledAmount != expectedSettledAmount)
            {
                throw new InvalidOperationException(
                    $"受管供应商待结单据 {bill.BillNo} 的已结金额 {actualSettledAmount} 与有效受管凭证独立重算值 {expectedSettledAmount} 不一致。");
            }

            var expectedStatus = expectedSettledAmount <= 0m
                ? SupplierBillStatus.Pending
                : expectedSettledAmount >= NumericPrecision.RoundMoney(bill.DocumentAmount)
                    ? SupplierBillStatus.Settled
                    : SupplierBillStatus.PartiallySettled;
            if (bill.BillStatus != expectedStatus)
            {
                throw new InvalidOperationException(
                    $"受管供应商待结单据 {bill.BillNo} 当前状态为 {bill.BillStatus}，独立余额重算状态为 {expectedStatus}。");
            }
        }
    }

    private static void ValidateManagedSupplierSettlement(
        SupplierSettlement settlement,
        SupplierSettlementSeed seed)
    {
        var serialNo = CreateSupplierSettlementSerialNo(seed.Sequence);
        if (!string.Equals(settlement.SerialNo, serialNo, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"受管供应商结算 {serialNo} 的稳定流水号发生漂移。");
        }

        var detail = settlement.Details.SingleOrDefault()
                     ?? throw new InvalidOperationException($"受管供应商结算 {serialNo} 缺少唯一待结单据核销明细。");
        if (detail.SupplierBillId != seed.SupplierBillId)
        {
            throw new InvalidOperationException(
                $"受管供应商结算 {serialNo} 关联了非预期待结单据，无法安全复用。");
        }

        var isInterruptedVoid = seed.Scenario == SupplierSettlementScenario.Voided
                                && settlement.SettlementStatus == seed.CreatedStatus;
        var expectedStoredStatus = seed.Scenario == SupplierSettlementScenario.Voided
            ? SupplierSettlementStatus.Voided
            : seed.CreatedStatus;
        if (settlement.SettlementStatus != expectedStoredStatus && !isInterruptedVoid)
        {
            throw new InvalidOperationException(
                $"受管供应商结算 {serialNo} 当前状态为 {settlement.SettlementStatus}，不符合确定性场景状态。");
        }

        var expectedRemark = settlement.SettlementStatus == SupplierSettlementStatus.Voided
            ? CreateSupplierSettlementVoidRemark(seed.Sequence)
            : CreateSupplierSettlementRemark(seed);
        var mainFingerprintMatches = !string.IsNullOrWhiteSpace(settlement.SettlementNo)
                                     && settlement.SupplierId == seed.SupplierId
                                     && string.Equals(settlement.SupplierNameSnapshot, seed.SupplierName, StringComparison.Ordinal)
                                     && settlement.SettlementDate == CreateSupplierSettlementDate(seed.Sequence)
                                     && settlement.ShouldAmount == seed.ShouldAmount
                                     && settlement.PaymentAmount == seed.PaymentAmount
                                     && settlement.DiscountAmount == seed.DiscountAmount
                                     && settlement.AppliedAmount == seed.AppliedAmount
                                     && settlement.RemainingAmount == seed.RemainingAmount
                                     && string.Equals(settlement.Remark, expectedRemark, StringComparison.Ordinal)
                                     && settlement.CreateBy.HasValue
                                     && !string.IsNullOrWhiteSpace(settlement.CreateName);
        var detailFingerprintMatches = string.Equals(detail.SupplierBillNoSnapshot, seed.SupplierBillNo, StringComparison.Ordinal)
                                       && detail.SourceType == seed.SourceType
                                       && string.Equals(detail.SourceDocumentNoSnapshot, seed.SourceDocumentNo, StringComparison.Ordinal)
                                       && detail.StockInOrderId == seed.StockInOrderId
                                       && detail.StockOutOrderId == seed.StockOutOrderId
                                       && detail.PayableAmountSnapshot == seed.PayableAmount
                                       && detail.PreviousSettledAmount == seed.PreviousSettledAmount
                                       && detail.PaymentAmount == seed.PaymentAmount
                                       && detail.DiscountAmount == seed.DiscountAmount
                                       && detail.AppliedAmount == seed.AppliedAmount
                                       && detail.CurrentSettledAmount == seed.CurrentSettledAmount
                                       && detail.RemainingAmount == seed.RemainingAmount
                                       && string.Equals(detail.Remark, CreateSupplierSettlementDetailRemark(seed), StringComparison.Ordinal)
                                       && detail.CreateBy.HasValue
                                       && !string.IsNullOrWhiteSpace(detail.CreateName);
        if (!mainFingerprintMatches || !detailFingerprintMatches)
        {
            throw new InvalidOperationException(
                $"受管供应商结算 {serialNo} 的金额、来源、日期、备注或审计指纹发生漂移，无法安全复用。");
        }

        if (settlement.SettlementStatus == SupplierSettlementStatus.Voided)
        {
            if (!settlement.VoidedTime.HasValue
                || !settlement.VoidedBy.HasValue
                || string.IsNullOrWhiteSpace(settlement.VoidedByNameSnapshot)
                || !settlement.UpdateBy.HasValue
                || string.IsNullOrWhiteSpace(settlement.UpdateName))
            {
                throw new InvalidOperationException($"受管供应商结算 {serialNo} 的作废审计指纹不完整。");
            }
        }
        else if (settlement.VoidedTime.HasValue
                 || settlement.VoidedBy.HasValue
                 || !string.IsNullOrWhiteSpace(settlement.VoidedByNameSnapshot))
        {
            throw new InvalidOperationException($"受管供应商结算 {serialNo} 的非作废状态包含无效作废审计。");
        }
    }

    private static string CreatePurchaseStockInRemark(int sequence)
    {
        var stableKey = DemoDataStableKeyCatalog.Create("PURCHASE-STOCK-IN", sequence);
        return $"{stableKey} 华东联调采购入库{sequence:D2}：来源受管采购单，用于库存批次、流水和供应商待结链路。";
    }

    private static string CreateSupplierSettlementSerialNo(int sequence)
    {
        return DemoDataStableKeyCatalog.Create("SUPPLIER-SETTLEMENT", sequence);
    }

    private static DateTime CreateSupplierSettlementDate(int sequence)
    {
        return new DateTime(2026, 6, 15, 10, 0, 0, DateTimeKind.Utc)
            .AddDays((sequence - 1) % 30)
            .AddMinutes(sequence);
    }

    private static string CreateSupplierSettlementRemark(SupplierSettlementSeed seed)
    {
        var stage = seed.Scenario switch
        {
            SupplierSettlementScenario.FirstInstallment => "首笔分期付款",
            SupplierSettlementScenario.SecondPartialInstallment => "续付分期款",
            SupplierSettlementScenario.SecondFinalInstallment => "尾款结清",
            SupplierSettlementScenario.Voided => "付款回单复核待作废",
            _ => throw new ArgumentOutOfRangeException(nameof(seed.Scenario), seed.Scenario, null)
        };
        return $"{CreateSupplierSettlementSerialNo(seed.Sequence)} 华东联调供应商结算凭证{seed.Sequence:D3}：{stage}，同步核销受管供应商待结余额。";
    }

    private static string CreateSupplierSettlementDetailRemark(SupplierSettlementSeed seed)
    {
        return $"华东联调供应商待结核销明细{seed.Sequence:D3}：同时记录实际付款与长期合作优惠。";
    }

    private static string CreateSupplierSettlementVoidRemark(int sequence)
    {
        return $"{CreateSupplierSettlementSerialNo(sequence)} 华东联调供应商结算凭证{sequence:D3}：付款回单号复核不一致，凭证作废并回滚待结余额。";
    }

    private sealed record SupplierSettlementSeed
    {
        public required int Sequence { get; init; }
        public required Guid SupplierBillId { get; init; }
        public required string SupplierBillNo { get; init; }
        public required Guid SupplierId { get; init; }
        public required string SupplierName { get; init; }
        public required SupplierBillSourceType SourceType { get; init; }
        public required string SourceDocumentNo { get; init; }
        public required Guid? StockInOrderId { get; init; }
        public required Guid? StockOutOrderId { get; init; }
        public required SupplierSettlementScenario Scenario { get; init; }
        public required decimal DocumentAmount { get; init; }
        public required decimal PayableAmount { get; init; }
        public required decimal PreviousSettledAmount { get; init; }
        public required decimal ShouldAmount { get; init; }
        public required decimal PaymentAmount { get; init; }
        public required decimal DiscountAmount { get; init; }
        public required decimal AppliedAmount { get; init; }
        public required decimal CurrentSettledAmount { get; init; }
        public required decimal RemainingAmount { get; init; }
        public required SupplierSettlementStatus CreatedStatus { get; init; }
    }

    private enum SupplierSettlementScenario
    {
        FirstInstallment,
        SecondPartialInstallment,
        SecondFinalInstallment,
        Voided
    }

    internal sealed record GenerationResult(
        int CreatedSettlements,
        int ReusedSettlements,
        int CreatedDetails,
        int ReusedDetails);
}
