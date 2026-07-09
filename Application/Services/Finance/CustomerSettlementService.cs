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
/// 客户结款服务，事务化创建和作废结款凭证，并维护客户账单已结金额和余额状态。
/// </summary>
public class CustomerSettlementService(
    ICustomerBillRepository customerBillRepository,
    ICustomerSettlementRepository customerSettlementRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IValidator<CreateCustomerSettlementDto> createValidator,
    IValidator<VoidCustomerSettlementDto> voidValidator,
    ILogger<CustomerSettlementService> logger) : ICustomerSettlementService
{
    /// <inheritdoc />
    public async Task<PagedResult<CustomerBillDto>> GetBillsAsync(CustomerBillQueryParameters parameters)
    {
        var result = await customerBillRepository.GetPagedAsync(
            parameters.QueryBuild(),
            parameters.Current,
            parameters.Size,
            x => x.BillDate,
            true);
        return mapper.ToPagedResult<CustomerBill, CustomerBillDto>(result, parameters);
    }

    /// <inheritdoc />
    public async Task<PagedResult<CustomerSettlementDto>> GetPagedAsync(CustomerSettlementQueryParameters parameters)
    {
        var result = await customerSettlementRepository.GetPagedAsync(
            parameters.QueryBuild(),
            parameters.Current,
            parameters.Size,
            x => x.SettlementDate,
            true);
        return mapper.ToPagedResult<CustomerSettlement, CustomerSettlementDto>(result, parameters);
    }

    /// <inheritdoc />
    public async Task<CustomerSettlementDto> GetByIdAsync(Guid id)
    {
        var entity = await customerSettlementRepository.GetByIdAsync(id)
                     ?? throw new NotFoundException("客户结款凭证不存在");
        return mapper.Map<CustomerSettlementDto>(entity);
    }

    /// <inheritdoc />
    public async Task<CustomerSettlementDto> CreateAsync(CreateCustomerSettlementDto dto)
    {
        await ValidateAsync(createValidator, dto);
        var settlementId = Guid.NewGuid();

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var requestedBillIds = dto.Details.Select(x => x.CustomerBillId).ToList();
            if (requestedBillIds.Count != requestedBillIds.Distinct().Count())
            {
                throw new BusinessException("同一客户账单不能在一张结款凭证中重复填写");
            }

            var bills = await customerBillRepository.GetByIdsForUpdateAsync(requestedBillIds);
            if (bills.Count != requestedBillIds.Count)
            {
                throw new BusinessException("部分客户账单不存在");
            }

            var customerIds = bills.Select(x => x.CustomerId).Distinct().ToList();
            if (customerIds.Count != 1)
            {
                throw new BusinessException("一张结款凭证只能处理同一客户的账单");
            }

            var inputByBillId = dto.Details.ToDictionary(x => x.CustomerBillId);
            var details = new List<CustomerSettlementDetail>(bills.Count);
            foreach (var bill in bills)
            {
                var input = inputByBillId[bill.Id];
                var paymentAmount = NumericPrecision.RoundMoney(input.PaymentAmount);
                var discountAmount = NumericPrecision.RoundMoney(input.DiscountAmount);
                var appliedAmount = NumericPrecision.RoundMoney(paymentAmount + discountAmount);
                if (appliedAmount <= 0m)
                {
                    throw new BusinessException("本次结款金额和优惠金额合计必须大于 0");
                }

                var pendingAmount = GetPendingAmount(bill);
                if (pendingAmount <= 0m || bill.BillStatus == CustomerBillStatus.Settled)
                {
                    throw new BusinessException($"客户账单 {bill.BillNo} 已无待结余额");
                }

                if (appliedAmount > pendingAmount)
                {
                    throw new BusinessException($"客户账单 {bill.BillNo} 的本次结款与优惠合计不能超过待结余额");
                }

                var previousSettledAmount = NumericPrecision.RoundMoney(bill.SettledAmount);
                bill.SettledAmount = NumericPrecision.RoundMoney(previousSettledAmount + appliedAmount);
                RecalculateBillStatus(bill);
                ApplyUpdateAudit(bill);

                var detail = new CustomerSettlementDetail
                {
                    Id = Guid.NewGuid(),
                    CustomerSettlementId = settlementId,
                    CustomerBillId = bill.Id,
                    CustomerBillNoSnapshot = bill.BillNo,
                    SaleOrderId = bill.SaleOrderId,
                    SaleOrderNoSnapshot = bill.SaleOrderNoSnapshot,
                    ReceivableAmountSnapshot = bill.ReceivableAmount,
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
                details.Sum(x => x.ReceivableAmountSnapshot - x.PreviousSettledAmount));
            var paymentTotal = NumericPrecision.RoundMoney(details.Sum(x => x.PaymentAmount));
            var discountTotal = NumericPrecision.RoundMoney(details.Sum(x => x.DiscountAmount));
            var appliedTotal = NumericPrecision.RoundMoney(details.Sum(x => x.AppliedAmount));
            var remainingTotal = NumericPrecision.RoundMoney(details.Sum(x => x.RemainingAmount));
            var firstBill = bills[0];
            var settlement = new CustomerSettlement
            {
                Id = settlementId,
                SettlementNo = await GenerateSettlementNoAsync(),
                CustomerId = firstBill.CustomerId,
                CustomerNameSnapshot = firstBill.CustomerNameSnapshot,
                SettlementDate = dto.SettlementDate ?? DateTime.UtcNow,
                SerialNo = Normalize(dto.SerialNo),
                ShouldAmount = shouldAmount,
                PaymentAmount = paymentTotal,
                DiscountAmount = discountTotal,
                AppliedAmount = appliedTotal,
                RemainingAmount = remainingTotal,
                SettlementStatus = remainingTotal == 0m
                    ? CustomerSettlementStatus.Settled
                    : CustomerSettlementStatus.PartiallySettled,
                Remark = Normalize(dto.Remark),
                Details = details
            };
            ApplyCreateAudit(settlement);
            await customerSettlementRepository.AddAsync(settlement);
        });

        logger.LogInformation("客户结款凭证创建成功: {SettlementId}", settlementId);
        return await GetByIdAsync(settlementId);
    }

    /// <inheritdoc />
    public async Task<CustomerSettlementDto> VoidAsync(Guid id, VoidCustomerSettlementDto dto)
    {
        await ValidateAsync(voidValidator, dto);
        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var settlement = await customerSettlementRepository.GetByIdForUpdateAsync(id)
                             ?? throw new NotFoundException("客户结款凭证不存在");
            if (settlement.SettlementStatus == CustomerSettlementStatus.Voided)
            {
                throw new BusinessException("客户结款凭证已作废，不能重复作废");
            }

            var billIds = settlement.Details.Select(x => x.CustomerBillId).ToList();
            var bills = await customerBillRepository.GetByIdsForUpdateAsync(billIds);
            if (bills.Count != billIds.Count)
            {
                throw new BusinessException("部分客户账单不存在，无法作废结款凭证");
            }

            var billsById = bills.ToDictionary(x => x.Id);
            foreach (var detail in settlement.Details.OrderBy(x => x.CustomerBillId))
            {
                var bill = billsById[detail.CustomerBillId];
                if (bill.SettledAmount < detail.AppliedAmount)
                {
                    throw new BusinessException($"客户账单 {bill.BillNo} 已结金额小于凭证核销金额，不能作废");
                }

                bill.SettledAmount = NumericPrecision.RoundMoney(bill.SettledAmount - detail.AppliedAmount);
                RecalculateBillStatus(bill);
                ApplyUpdateAudit(bill);
            }

            settlement.SettlementStatus = CustomerSettlementStatus.Voided;
            settlement.VoidedTime = DateTime.UtcNow;
            settlement.VoidedBy = currentUserService.GetUserId();
            settlement.VoidedByNameSnapshot = currentUserService.GetUserName();
            settlement.Remark = Normalize(dto.Remark);
            ApplyUpdateAudit(settlement);
        });

        logger.LogInformation("客户结款凭证作废成功: {SettlementId}", id);
        return await GetByIdAsync(id);
    }

    private async Task<string> GenerateSettlementNoAsync()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var suffix = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
            var settlementNo = $"CS{DateTime.UtcNow:yyyyMMddHHmmssfff}{suffix}";
            if (!await customerSettlementRepository.ExistsSettlementNoAsync(settlementNo))
            {
                return settlementNo;
            }
        }

        throw new BusinessException("客户结款凭证编号生成失败，请重试");
    }

    private static decimal GetPendingAmount(CustomerBill bill)
    {
        return NumericPrecision.RoundMoney(Math.Max(0m, bill.ReceivableAmount - bill.SettledAmount));
    }

    private static void RecalculateBillStatus(CustomerBill bill)
    {
        bill.SettledAmount = NumericPrecision.RoundMoney(bill.SettledAmount);
        if (bill.SettledAmount <= 0m)
        {
            bill.SettledAmount = 0m;
            bill.BillStatus = CustomerBillStatus.Pending;
        }
        else if (bill.SettledAmount >= bill.ReceivableAmount)
        {
            bill.SettledAmount = bill.ReceivableAmount;
            bill.BillStatus = CustomerBillStatus.Settled;
        }
        else
        {
            bill.BillStatus = CustomerBillStatus.PartiallySettled;
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
