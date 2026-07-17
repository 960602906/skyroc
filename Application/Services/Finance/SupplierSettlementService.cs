using Application.DTOs.Finance;
using Application.Exceptions;
using Application.Extensions;
using Application.interfaces;
using Application.QueryParameters.Finance;
using AutoMapper;
using Domain.Entities;
using Domain.Entities.Finance;
using Domain.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared.Constants;
using ValidationException = Application.Exceptions.ValidationException;

namespace Application.Services;

/// <summary>
/// 供应商结算服务，事务化创建和作废结算单，并维护供应商待结单据已结金额和余额状态。
/// </summary>
public class SupplierSettlementService(
    ISupplierBillRepository supplierBillRepository,
    ISupplierSettlementRepository supplierSettlementRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IDocumentNoGenerator documentNoGenerator,
    IValidator<CreateSupplierSettlementDto> createValidator,
    IValidator<VoidSupplierSettlementDto> voidValidator,
    ILogger<SupplierSettlementService> logger) : ISupplierSettlementService
{
    /// <inheritdoc />
    public async Task<PagedResult<SupplierBillDto>> GetBillsAsync(SupplierBillQueryParameters parameters)
    {
        var result = await supplierBillRepository.GetPagedAsync(
            parameters.QueryBuild(),
            parameters.Current,
            parameters.Size,
            x => x.BillDate,
            true);
        return mapper.ToPagedResult<SupplierBill, SupplierBillDto>(result, parameters);
    }

    /// <inheritdoc />
    public async Task<PagedResult<SupplierSettlementDto>> GetPagedAsync(SupplierSettlementQueryParameters parameters)
    {
        var result = await supplierSettlementRepository.GetPagedAsync(
            parameters.QueryBuild(),
            parameters.Current,
            parameters.Size,
            x => x.SettlementDate,
            true);
        return mapper.ToPagedResult<SupplierSettlement, SupplierSettlementDto>(result, parameters);
    }

    /// <inheritdoc />
    public async Task<SupplierSettlementDto> GetByIdAsync(Guid id)
    {
        var entity = await supplierSettlementRepository.GetByIdAsync(id)
                     ?? throw new NotFoundException("供应商结算单不存在");
        return mapper.Map<SupplierSettlementDto>(entity);
    }

    /// <inheritdoc />
    public async Task<SupplierSettlementDto> CreateAsync(CreateSupplierSettlementDto dto)
    {
        await ValidateAsync(createValidator, dto);
        var settlementId = Guid.NewGuid();

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var requestedBillIds = dto.Details.Select(x => x.SupplierBillId).ToList();
            if (requestedBillIds.Count != requestedBillIds.Distinct().Count())
            {
                throw new BusinessException("同一供应商待结单据不能在一张结算单中重复填写");
            }

            var bills = await supplierBillRepository.GetByIdsForUpdateAsync(requestedBillIds);
            if (bills.Count != requestedBillIds.Count)
            {
                throw new BusinessException("部分供应商待结单据不存在");
            }

            var supplierIds = bills.Select(x => x.SupplierId).Distinct().ToList();
            if (supplierIds.Count != 1)
            {
                throw new BusinessException("一张结算单只能处理同一供应商的待结单据");
            }

            var inputByBillId = dto.Details.ToDictionary(x => x.SupplierBillId);
            var details = new List<SupplierSettlementDetail>(bills.Count);
            foreach (var bill in bills)
            {
                var input = inputByBillId[bill.Id];
                var paymentAmount = NumericPrecision.RoundMoney(input.PaymentAmount);
                var discountAmount = NumericPrecision.RoundMoney(input.DiscountAmount);
                var appliedAmount = NumericPrecision.RoundMoney(paymentAmount + discountAmount);
                if (appliedAmount <= 0m)
                {
                    throw new BusinessException("本次付款金额和优惠金额合计必须大于 0");
                }

                var pendingAmount = GetPendingAmount(bill);
                if (pendingAmount <= 0m || bill.BillStatus == SupplierBillStatus.Settled)
                {
                    throw new BusinessException($"供应商待结单据 {bill.BillNo} 已无待结余额");
                }

                if (appliedAmount > pendingAmount)
                {
                    throw new BusinessException($"供应商待结单据 {bill.BillNo} 的本次付款与优惠合计不能超过待结余额");
                }

                var previousSettledAmount = NumericPrecision.RoundMoney(bill.SettledAmount);
                bill.SettledAmount = NumericPrecision.RoundMoney(previousSettledAmount + appliedAmount);
                RecalculateBillStatus(bill);
                ApplyUpdateAudit(bill);

                var detail = new SupplierSettlementDetail
                {
                    Id = Guid.NewGuid(),
                    SupplierSettlementId = settlementId,
                    SupplierBillId = bill.Id,
                    SupplierBillNoSnapshot = bill.BillNo,
                    SourceType = bill.SourceType,
                    SourceDocumentNoSnapshot = bill.SourceDocumentNoSnapshot,
                    StockInOrderId = bill.StockInOrderId,
                    StockOutOrderId = bill.StockOutOrderId,
                    PayableAmountSnapshot = bill.PayableAmount,
                    PreviousSettledAmount = previousSettledAmount,
                    PaymentAmount = paymentAmount,
                    DiscountAmount = discountAmount,
                    AppliedAmount = appliedAmount,
                    CurrentSettledAmount = bill.SettledAmount,
                    RemainingAmount = GetPendingAmount(bill),
                    Remark = Normalize(input.Remark)
                };
                ApplyCreateAudit(detail);
                details.Add(detail);
            }

            var shouldAmount = NumericPrecision.RoundMoney(
                details.Sum(x => Math.Abs(x.PayableAmountSnapshot) - x.PreviousSettledAmount));
            var paymentTotal = NumericPrecision.RoundMoney(details.Sum(x => x.PaymentAmount));
            var discountTotal = NumericPrecision.RoundMoney(details.Sum(x => x.DiscountAmount));
            var appliedTotal = NumericPrecision.RoundMoney(details.Sum(x => x.AppliedAmount));
            var remainingTotal = NumericPrecision.RoundMoney(details.Sum(x => x.RemainingAmount));
            var firstBill = bills[0];
            var settlement = new SupplierSettlement
            {
                Id = settlementId,
                SettlementNo = await documentNoGenerator.NextAsync(
                    DocumentNoKind.SupplierSettlement,
                    no => supplierSettlementRepository.ExistsSettlementNoAsync(no)),
                SupplierId = firstBill.SupplierId,
                SupplierNameSnapshot = firstBill.SupplierNameSnapshot,
                SettlementDate = dto.SettlementDate ?? DateTime.UtcNow,
                SerialNo = Normalize(dto.SerialNo),
                ShouldAmount = shouldAmount,
                PaymentAmount = paymentTotal,
                DiscountAmount = discountTotal,
                AppliedAmount = appliedTotal,
                RemainingAmount = remainingTotal,
                SettlementStatus = remainingTotal == 0m
                    ? SupplierSettlementStatus.Settled
                    : SupplierSettlementStatus.PartiallySettled,
                Remark = Normalize(dto.Remark),
                Details = details
            };
            ApplyCreateAudit(settlement);
            await supplierSettlementRepository.AddAsync(settlement);
        });

        logger.LogInformation("供应商结算单创建成功: {SettlementId}", settlementId);
        return await GetByIdAsync(settlementId);
    }

    /// <inheritdoc />
    public async Task<SupplierSettlementDto> VoidAsync(Guid id, VoidSupplierSettlementDto dto)
    {
        await ValidateAsync(voidValidator, dto);
        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var settlement = await supplierSettlementRepository.GetByIdForUpdateAsync(id)
                             ?? throw new NotFoundException("供应商结算单不存在");
            if (settlement.SettlementStatus == SupplierSettlementStatus.Voided)
            {
                throw new BusinessException("供应商结算单已作废，不能重复作废");
            }

            var billIds = settlement.Details.Select(x => x.SupplierBillId).ToList();
            var bills = await supplierBillRepository.GetByIdsForUpdateAsync(billIds);
            if (bills.Count != billIds.Count)
            {
                throw new BusinessException("部分供应商待结单据不存在，无法作废结算单");
            }

            var billsById = bills.ToDictionary(x => x.Id);
            foreach (var detail in settlement.Details.OrderBy(x => x.SupplierBillId))
            {
                var bill = billsById[detail.SupplierBillId];
                if (bill.SettledAmount < detail.AppliedAmount)
                {
                    throw new BusinessException($"供应商待结单据 {bill.BillNo} 已结金额小于结算单核销金额，不能作废");
                }

                bill.SettledAmount = NumericPrecision.RoundMoney(bill.SettledAmount - detail.AppliedAmount);
                RecalculateBillStatus(bill);
                ApplyUpdateAudit(bill);
            }

            settlement.SettlementStatus = SupplierSettlementStatus.Voided;
            settlement.VoidedTime = DateTime.UtcNow;
            settlement.VoidedBy = currentUserService.GetUserId();
            settlement.VoidedByNameSnapshot = currentUserService.GetUserName();
            settlement.Remark = Normalize(dto.Remark);
            ApplyUpdateAudit(settlement);
        });

        logger.LogInformation("供应商结算单作废成功: {SettlementId}", id);
        return await GetByIdAsync(id);
    }

    private static decimal GetPendingAmount(SupplierBill bill)
    {
        return NumericPrecision.RoundMoney(Math.Max(0m, bill.DocumentAmount - bill.SettledAmount));
    }

    private static void RecalculateBillStatus(SupplierBill bill)
    {
        bill.SettledAmount = NumericPrecision.RoundMoney(bill.SettledAmount);
        if (bill.SettledAmount <= 0m)
        {
            bill.SettledAmount = 0m;
            bill.BillStatus = SupplierBillStatus.Pending;
        }
        else if (bill.SettledAmount >= bill.DocumentAmount)
        {
            bill.SettledAmount = bill.DocumentAmount;
            bill.BillStatus = SupplierBillStatus.Settled;
        }
        else
        {
            bill.BillStatus = SupplierBillStatus.PartiallySettled;
        }
    }

    private static async Task ValidateAsync<T>(IValidator<T> validator, T dto)
    {
        var result = await validator.ValidateAsync(dto);
        if (!result.IsValid)
        {
            throw new ValidationException(result.Errors);
        }
    }

    private void ApplyCreateAudit(BaseEntity entity)
    {
        entity.CreateBy = currentUserService.GetUserId();
        entity.CreateName = currentUserService.GetUserName();
    }

    private void ApplyUpdateAudit(BaseEntity entity)
    {
        entity.UpdateBy = currentUserService.GetUserId();
        entity.UpdateName = currentUserService.GetUserName();
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
