using Application.DTOs.Finance;
using Application.interfaces;
using Domain.Entities.Finance;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace SkyRoc.Tests.Testing.PostgreSql;

internal sealed class DemoDataCustomerSettlementBuilder(
    ApplicationDbContext context,
    ICustomerSettlementService customerSettlementService)
{
    internal async Task<GenerationResult> GenerateAsync(CancellationToken cancellationToken)
    {
        var result = await GenerateCustomerSettlementsAsync(
            context,
            customerSettlementService,
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
        int ReusedDetails)> GenerateCustomerSettlementsAsync(
            ApplicationDbContext context,
            ICustomerSettlementService customerSettlementService,
            CancellationToken cancellationToken)
    {
        context.ChangeTracker.Clear();
        // 客户结款仍仅覆盖 001–060 中 40 张既有签收账单；061–070 新增账单保留待结，留给后续结款扩容切片。
        var expectedSaleOrderKeys = Enumerable.Range(1, 60)
            .Where(sequence => sequence % 3 != 0)
            .Select(sequence => DemoDataStableKeyCatalog.Create("SALE-ORDER", sequence))
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();
        var managedBills = await context.CustomerBills
            .AsNoTracking()
            .Include(bill => bill.SaleOrder)
            .Where(bill => bill.SaleOrder.InnerRemark != null
                           && expectedSaleOrderKeys.Contains(bill.SaleOrder.InnerRemark))
            .OrderBy(bill => bill.SaleOrder.InnerRemark)
            .ToListAsync(cancellationToken);
        var actualSaleOrderKeys = managedBills
            .Select(bill => bill.SaleOrder.InnerRemark!)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();
        if (!actualSaleOrderKeys.SequenceEqual(expectedSaleOrderKeys, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                $"受管客户结款需要精确匹配 {expectedSaleOrderKeys.Length} 个既有签收订单稳定键，当前匹配 {actualSaleOrderKeys.Length} 个。");
        }

        var seeds = CreateCustomerSettlementSeeds(managedBills);
        var stableSerialNumbers = seeds
            .Select(seed => CreateCustomerSettlementSerialNo(seed.Sequence))
            .ToArray();
        var existingSettlements = await context.CustomerSettlements
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
                $"受管客户结款稳定流水号 {duplicateStableSerial.Key} 存在 {duplicateStableSerial.Count()} 条冲突记录。");
        }

        var existingBySerial = existingSettlements.ToDictionary(
            settlement => settlement.SerialNo!,
            StringComparer.Ordinal);
        var managedBillIds = managedBills.Select(bill => bill.Id).ToArray();
        var hasNonManagedActiveSettlement = await context.CustomerSettlementDetails
            .AsNoTracking()
            .AnyAsync(
                detail => managedBillIds.Contains(detail.CustomerBillId)
                          && detail.CustomerSettlement.SettlementStatus != CustomerSettlementStatus.Voided
                          && (detail.CustomerSettlement.SerialNo == null
                              || !stableSerialNumbers.Contains(detail.CustomerSettlement.SerialNo)),
                cancellationToken);
        if (hasNonManagedActiveSettlement)
        {
            throw new InvalidOperationException(
                "受管客户账单存在非生成器管理的有效结款凭证；为避免修改非受管财务数据，本轮已停止。");
        }

        EnsureCustomerSettlementStableKeysFormPrefix(seeds, existingBySerial);
        var seedsBySerial = seeds.ToDictionary(
            seed => CreateCustomerSettlementSerialNo(seed.Sequence),
            StringComparer.Ordinal);
        foreach (var existingSettlement in existingSettlements)
        {
            ValidateManagedCustomerSettlement(
                existingSettlement,
                seedsBySerial[existingSettlement.SerialNo!]);
        }

        ValidateManagedCustomerBillBalances(managedBills, existingSettlements);
        var createdSettlements = 0;
        var reusedSettlements = 0;
        var createdDetails = 0;
        var reusedDetails = 0;
        foreach (var seed in seeds)
        {
            var serialNo = CreateCustomerSettlementSerialNo(seed.Sequence);
            if (existingBySerial.TryGetValue(serialNo, out var existingSettlement))
            {
                ValidateManagedCustomerSettlement(existingSettlement, seed);
                if (seed.Scenario == CustomerSettlementScenario.Voided
                    && existingSettlement.SettlementStatus != CustomerSettlementStatus.Voided)
                {
                    await customerSettlementService.VoidAsync(
                        existingSettlement.Id,
                        new VoidCustomerSettlementDto
                        {
                            Remark = CreateCustomerSettlementVoidRemark(seed.Sequence)
                        });
                    context.ChangeTracker.Clear();
                }

                reusedSettlements++;
                reusedDetails += existingSettlement.Details.Count;
                continue;
            }

            var bill = await context.CustomerBills
                .AsNoTracking()
                .SingleAsync(item => item.Id == seed.CustomerBillId, cancellationToken);
            if (NumericPrecision.RoundMoney(bill.ReceivableAmount) != seed.ReceivableAmount
                || NumericPrecision.RoundMoney(bill.SettledAmount) != seed.PreviousSettledAmount)
            {
                throw new InvalidOperationException(
                    $"受管客户结款 {serialNo} 创建前账单余额与确定性快照不一致，已停止以避免重复核销。");
            }

            var created = await customerSettlementService.CreateAsync(new CreateCustomerSettlementDto
            {
                SettlementDate = CreateCustomerSettlementDate(seed.Sequence),
                SerialNo = serialNo,
                Remark = CreateCustomerSettlementRemark(seed),
                Details =
                [
                    new CreateCustomerSettlementDetailDto
                    {
                        CustomerBillId = seed.CustomerBillId,
                        PaymentAmount = seed.PaymentAmount,
                        DiscountAmount = seed.DiscountAmount,
                        Remark = CreateCustomerSettlementDetailRemark(seed)
                    }
                ]
            });
            if (created.Details.Count != 1)
            {
                throw new InvalidOperationException(
                    $"受管客户结款 {serialNo} 应生成 1 条明细，实际为 {created.Details.Count} 条。");
            }

            if (seed.Scenario == CustomerSettlementScenario.Voided)
            {
                await customerSettlementService.VoidAsync(
                    created.Id,
                    new VoidCustomerSettlementDto
                    {
                        Remark = CreateCustomerSettlementVoidRemark(seed.Sequence)
                    });
            }

            context.ChangeTracker.Clear();
            createdSettlements++;
            createdDetails += created.Details.Count;
        }

        context.ChangeTracker.Clear();
        var finalSettlements = await context.CustomerSettlements
            .AsNoTracking()
            .Include(settlement => settlement.Details)
            .Where(settlement => settlement.SerialNo != null
                                 && stableSerialNumbers.Contains(settlement.SerialNo))
            .ToListAsync(cancellationToken);
        if (finalSettlements.Count != 100 || finalSettlements.Sum(settlement => settlement.Details.Count) != 100)
        {
            throw new InvalidOperationException(
                $"受管客户结款应为 100 张凭证和 100 条明细，当前为 {finalSettlements.Count} 张凭证、{finalSettlements.Sum(settlement => settlement.Details.Count)} 条明细。");
        }

        foreach (var finalSettlement in finalSettlements)
        {
            ValidateManagedCustomerSettlement(
                finalSettlement,
                seedsBySerial[finalSettlement.SerialNo!]);
        }

        var finalBills = await context.CustomerBills
            .AsNoTracking()
            .Where(bill => managedBillIds.Contains(bill.Id))
            .OrderBy(bill => bill.SaleOrderNoSnapshot)
            .ToListAsync(cancellationToken);
        ValidateManagedCustomerBillBalances(finalBills, finalSettlements);
        if (finalBills.Count(bill => bill.BillStatus == CustomerBillStatus.Settled) != 20
            || finalBills.Count(bill => bill.BillStatus == CustomerBillStatus.PartiallySettled) != 20)
        {
            throw new InvalidOperationException(
                "受管客户结款完成后应保留 20 张已结账单和 20 张部分结款账单，以覆盖长期联调状态。");
        }

        return (createdSettlements, reusedSettlements, createdDetails, reusedDetails);
    }

    private static IReadOnlyList<CustomerSettlementSeed> CreateCustomerSettlementSeeds(
        IReadOnlyList<CustomerBill> managedBills)
    {
        // 001–040 先核销每张账单的四分之一，建立所有账单的首笔部分结款快照。
        var seeds = new List<CustomerSettlementSeed>(100);
        var firstInstallmentSeeds = new CustomerSettlementSeed[managedBills.Count];
        for (var index = 0; index < managedBills.Count; index++)
        {
            var bill = managedBills[index];
            var receivableAmount = NumericPrecision.RoundMoney(bill.ReceivableAmount);
            var firstAppliedAmount = NumericPrecision.RoundMoney(receivableAmount * 0.25m);
            var seed = CreateCustomerSettlementSeed(
                index + 1,
                bill,
                CustomerSettlementScenario.FirstInstallment,
                0m,
                firstAppliedAmount);
            firstInstallmentSeeds[index] = seed;
            seeds.Add(seed);
        }

        // 041–080 对前 20 张账单结清尾款，其余 20 张再次部分核销并保留待结余额。
        var secondInstallmentSeeds = new CustomerSettlementSeed[managedBills.Count];
        for (var index = 0; index < managedBills.Count; index++)
        {
            var bill = managedBills[index];
            var firstSeed = firstInstallmentSeeds[index];
            var pendingAmount = NumericPrecision.RoundMoney(
                firstSeed.ReceivableAmount - firstSeed.CurrentSettledAmount);
            var scenario = index < 20
                ? CustomerSettlementScenario.SecondFinalInstallment
                : CustomerSettlementScenario.SecondPartialInstallment;
            var appliedAmount = scenario == CustomerSettlementScenario.SecondFinalInstallment
                ? pendingAmount
                : NumericPrecision.RoundMoney(pendingAmount * 0.5m);
            var seed = CreateCustomerSettlementSeed(
                index + 41,
                bill,
                scenario,
                firstSeed.CurrentSettledAmount,
                appliedAmount);
            secondInstallmentSeeds[index] = seed;
            seeds.Add(seed);
        }

        // 081–100 在仍有余额的 20 张账单上创建后立即作废，验证凭证保留与余额原子回滚。
        for (var index = 20; index < managedBills.Count; index++)
        {
            var bill = managedBills[index];
            var secondSeed = secondInstallmentSeeds[index];
            var pendingAmount = NumericPrecision.RoundMoney(
                secondSeed.ReceivableAmount - secondSeed.CurrentSettledAmount);
            seeds.Add(CreateCustomerSettlementSeed(
                index + 61,
                bill,
                CustomerSettlementScenario.Voided,
                secondSeed.CurrentSettledAmount,
                NumericPrecision.RoundMoney(pendingAmount * 0.1m)));
        }

        return seeds;
    }

    private static CustomerSettlementSeed CreateCustomerSettlementSeed(
        int sequence,
        CustomerBill bill,
        CustomerSettlementScenario scenario,
        decimal previousSettledAmount,
        decimal appliedAmount)
    {
        var receivableAmount = NumericPrecision.RoundMoney(bill.ReceivableAmount);
        var normalizedPreviousSettledAmount = NumericPrecision.RoundMoney(previousSettledAmount);
        var normalizedAppliedAmount = NumericPrecision.RoundMoney(appliedAmount);
        var shouldAmount = NumericPrecision.RoundMoney(receivableAmount - normalizedPreviousSettledAmount);
        if (shouldAmount <= 0m || normalizedAppliedAmount <= 0m || normalizedAppliedAmount > shouldAmount)
        {
            throw new InvalidOperationException(
                $"受管客户结款 {CreateCustomerSettlementSerialNo(sequence)} 的确定性金额快照不满足正余额约束。");
        }

        var discountAmount = NumericPrecision.RoundMoney(normalizedAppliedAmount * 0.1m);
        var paymentAmount = NumericPrecision.RoundMoney(normalizedAppliedAmount - discountAmount);
        if (paymentAmount <= 0m || discountAmount <= 0m)
        {
            throw new InvalidOperationException(
                $"受管客户结款 {CreateCustomerSettlementSerialNo(sequence)} 的收款或优惠金额不满足正金额语义。");
        }

        var currentSettledAmount = NumericPrecision.RoundMoney(
            normalizedPreviousSettledAmount + normalizedAppliedAmount);
        var remainingAmount = NumericPrecision.RoundMoney(receivableAmount - currentSettledAmount);
        return new CustomerSettlementSeed
        {
            Sequence = sequence,
            CustomerBillId = bill.Id,
            CustomerBillNo = bill.BillNo,
            SaleOrderId = bill.SaleOrderId,
            SaleOrderNo = bill.SaleOrderNoSnapshot,
            CustomerId = bill.CustomerId,
            CustomerName = bill.CustomerNameSnapshot,
            Scenario = scenario,
            ReceivableAmount = receivableAmount,
            PreviousSettledAmount = normalizedPreviousSettledAmount,
            ShouldAmount = shouldAmount,
            PaymentAmount = paymentAmount,
            DiscountAmount = discountAmount,
            AppliedAmount = normalizedAppliedAmount,
            CurrentSettledAmount = currentSettledAmount,
            RemainingAmount = remainingAmount,
            CreatedStatus = remainingAmount == 0m
                ? CustomerSettlementStatus.Settled
                : CustomerSettlementStatus.PartiallySettled
        };
    }

    private static void EnsureCustomerSettlementStableKeysFormPrefix(
        IReadOnlyList<CustomerSettlementSeed> seeds,
        IReadOnlyDictionary<string, CustomerSettlement> existingBySerial)
    {
        var missingStableKeyObserved = false;
        foreach (var seed in seeds)
        {
            var serialNo = CreateCustomerSettlementSerialNo(seed.Sequence);
            if (!existingBySerial.ContainsKey(serialNo))
            {
                missingStableKeyObserved = true;
                continue;
            }

            if (missingStableKeyObserved)
            {
                throw new InvalidOperationException(
                    $"受管客户结款 {serialNo} 之前存在稳定键缺口，无法安全重建既有财务快照。");
            }
        }
    }

    private static void ValidateManagedCustomerBillBalances(
        IReadOnlyList<CustomerBill> managedBills,
        IReadOnlyList<CustomerSettlement> managedSettlements)
    {
        var activeAppliedAmountsByBill = managedSettlements
            .Where(settlement => settlement.SettlementStatus != CustomerSettlementStatus.Voided)
            .SelectMany(settlement => settlement.Details)
            .GroupBy(detail => detail.CustomerBillId)
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
                    $"受管客户账单 {bill.BillNo} 的已结金额 {actualSettledAmount} 与有效受管凭证独立重算值 {expectedSettledAmount} 不一致。");
            }

            var expectedStatus = expectedSettledAmount <= 0m
                ? CustomerBillStatus.Pending
                : expectedSettledAmount >= NumericPrecision.RoundMoney(bill.ReceivableAmount)
                    ? CustomerBillStatus.Settled
                    : CustomerBillStatus.PartiallySettled;
            if (bill.BillStatus != expectedStatus)
            {
                throw new InvalidOperationException(
                    $"受管客户账单 {bill.BillNo} 当前状态为 {bill.BillStatus}，独立余额重算状态为 {expectedStatus}。");
            }
        }
    }

    private static void ValidateManagedCustomerSettlement(
        CustomerSettlement settlement,
        CustomerSettlementSeed seed)
    {
        var serialNo = CreateCustomerSettlementSerialNo(seed.Sequence);
        if (!string.Equals(settlement.SerialNo, serialNo, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"受管客户结款 {serialNo} 的稳定流水号发生漂移。");
        }

        var detail = settlement.Details.SingleOrDefault()
                     ?? throw new InvalidOperationException($"受管客户结款 {serialNo} 缺少唯一账单核销明细。");
        if (detail.CustomerBillId != seed.CustomerBillId)
        {
            throw new InvalidOperationException(
                $"受管客户结款 {serialNo} 关联了非预期客户账单，无法安全复用。");
        }

        var isInterruptedVoid = seed.Scenario == CustomerSettlementScenario.Voided
                                && settlement.SettlementStatus == seed.CreatedStatus;
        var expectedStoredStatus = seed.Scenario == CustomerSettlementScenario.Voided
            ? CustomerSettlementStatus.Voided
            : seed.CreatedStatus;
        if (settlement.SettlementStatus != expectedStoredStatus && !isInterruptedVoid)
        {
            throw new InvalidOperationException(
                $"受管客户结款 {serialNo} 当前状态为 {settlement.SettlementStatus}，不符合确定性场景状态。");
        }

        var expectedRemark = settlement.SettlementStatus == CustomerSettlementStatus.Voided
            ? CreateCustomerSettlementVoidRemark(seed.Sequence)
            : CreateCustomerSettlementRemark(seed);
        var mainFingerprintMatches = !string.IsNullOrWhiteSpace(settlement.SettlementNo)
                                     && settlement.CustomerId == seed.CustomerId
                                     && string.Equals(
                                         settlement.CustomerNameSnapshot,
                                         seed.CustomerName,
                                         StringComparison.Ordinal)
                                     && settlement.SettlementDate == CreateCustomerSettlementDate(seed.Sequence)
                                     && settlement.ShouldAmount == seed.ShouldAmount
                                     && settlement.PaymentAmount == seed.PaymentAmount
                                     && settlement.DiscountAmount == seed.DiscountAmount
                                     && settlement.AppliedAmount == seed.AppliedAmount
                                     && settlement.RemainingAmount == seed.RemainingAmount
                                     && string.Equals(settlement.Remark, expectedRemark, StringComparison.Ordinal)
                                     && settlement.CreateBy.HasValue
                                     && !string.IsNullOrWhiteSpace(settlement.CreateName);
        var detailFingerprintMatches = string.Equals(
                                           detail.CustomerBillNoSnapshot,
                                           seed.CustomerBillNo,
                                           StringComparison.Ordinal)
                                       && detail.SaleOrderId == seed.SaleOrderId
                                       && string.Equals(
                                           detail.SaleOrderNoSnapshot,
                                           seed.SaleOrderNo,
                                           StringComparison.Ordinal)
                                       && detail.ReceivableAmountSnapshot == seed.ReceivableAmount
                                       && detail.PreviousSettledAmount == seed.PreviousSettledAmount
                                       && detail.PaymentAmount == seed.PaymentAmount
                                       && detail.DiscountAmount == seed.DiscountAmount
                                       && detail.AppliedAmount == seed.AppliedAmount
                                       && detail.CurrentSettledAmount == seed.CurrentSettledAmount
                                       && detail.RemainingAmount == seed.RemainingAmount
                                       && string.Equals(
                                           detail.Remark,
                                           CreateCustomerSettlementDetailRemark(seed),
                                           StringComparison.Ordinal)
                                       && detail.CreateBy.HasValue
                                       && !string.IsNullOrWhiteSpace(detail.CreateName);
        if (!mainFingerprintMatches || !detailFingerprintMatches)
        {
            throw new InvalidOperationException(
                $"受管客户结款 {serialNo} 的金额、来源、日期、备注或审计指纹发生漂移，无法安全复用。");
        }

        if (settlement.SettlementStatus == CustomerSettlementStatus.Voided)
        {
            if (!settlement.VoidedTime.HasValue
                || !settlement.VoidedBy.HasValue
                || string.IsNullOrWhiteSpace(settlement.VoidedByNameSnapshot)
                || !settlement.UpdateBy.HasValue
                || string.IsNullOrWhiteSpace(settlement.UpdateName))
            {
                throw new InvalidOperationException($"受管客户结款 {serialNo} 的作废审计指纹不完整。");
            }
        }
        else if (settlement.VoidedTime.HasValue
                 || settlement.VoidedBy.HasValue
                 || !string.IsNullOrWhiteSpace(settlement.VoidedByNameSnapshot))
        {
            throw new InvalidOperationException($"受管客户结款 {serialNo} 的非作废状态包含无效作废审计。");
        }
    }

    private static string CreateCustomerSettlementSerialNo(int sequence)
    {
        return DemoDataStableKeyCatalog.Create("CUSTOMER-SETTLEMENT", sequence);
    }

    private static DateTime CreateCustomerSettlementDate(int sequence)
    {
        return new DateTime(2026, 6, 1, 9, 0, 0, DateTimeKind.Utc)
            .AddDays((sequence - 1) % 30)
            .AddMinutes(sequence);
    }

    private static string CreateCustomerSettlementRemark(CustomerSettlementSeed seed)
    {
        var stage = seed.Scenario switch
        {
            CustomerSettlementScenario.FirstInstallment => "首笔分期收款",
            CustomerSettlementScenario.SecondPartialInstallment => "续付分期收款",
            CustomerSettlementScenario.SecondFinalInstallment => "尾款结清",
            CustomerSettlementScenario.Voided => "回单复核待作废",
            _ => throw new ArgumentOutOfRangeException(nameof(seed.Scenario), seed.Scenario, null)
        };
        return $"{CreateCustomerSettlementSerialNo(seed.Sequence)} 华东联调客户结款凭证{seed.Sequence:D3}：{stage}，同步核销受管客户账单余额。";
    }

    private static string CreateCustomerSettlementDetailRemark(CustomerSettlementSeed seed)
    {
        return $"华东联调客户账单核销明细{seed.Sequence:D3}：同时记录实际收款与客户关系维护优惠。";
    }

    private static string CreateCustomerSettlementVoidRemark(int sequence)
    {
        return $"{CreateCustomerSettlementSerialNo(sequence)} 华东联调客户结款凭证{sequence:D3}：银行回单号复核不一致，凭证作废并回滚账单余额。";
    }

    private sealed record CustomerSettlementSeed
    {
        public required int Sequence { get; init; }

        public required Guid CustomerBillId { get; init; }

        public required string CustomerBillNo { get; init; }

        public required Guid SaleOrderId { get; init; }

        public required string SaleOrderNo { get; init; }

        public required Guid CustomerId { get; init; }

        public required string CustomerName { get; init; }

        public required CustomerSettlementScenario Scenario { get; init; }

        public required decimal ReceivableAmount { get; init; }

        public required decimal PreviousSettledAmount { get; init; }

        public required decimal ShouldAmount { get; init; }

        public required decimal PaymentAmount { get; init; }

        public required decimal DiscountAmount { get; init; }

        public required decimal AppliedAmount { get; init; }

        public required decimal CurrentSettledAmount { get; init; }

        public required decimal RemainingAmount { get; init; }

        public required CustomerSettlementStatus CreatedStatus { get; init; }
    }

    private enum CustomerSettlementScenario
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

