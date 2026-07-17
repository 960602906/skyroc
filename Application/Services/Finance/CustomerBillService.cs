using Application.Exceptions;
using Application.interfaces;
using Domain.Entities;
using Domain.Entities.AfterSales;
using Domain.Entities.Finance;
using Domain.Entities.Orders;
using Domain.Interfaces;
using Shared.Constants;
using Application.Extensions;

namespace Application.Services;

/// <summary>
/// 客户账单同步服务，负责把订单签收金额和售后冲减金额写入同一订单账单。
/// </summary>
public class CustomerBillService(
    ICustomerBillRepository customerBillRepository,
    IAfterSaleRepository afterSaleRepository,
    ICurrentUserService currentUserService,
    IDocumentNoGenerator documentNoGenerator) : ICustomerBillService
{
    /// <inheritdoc />
    public async Task<CustomerBill> SyncOrderAcceptanceAsync(SaleOrder saleOrder)
    {
        if (saleOrder.OrderStatus != SaleOrderStatus.Signed)
        {
            throw new BusinessException("销售订单签收完成后才能生成客户账单");
        }

        var bill = await customerBillRepository.GetBySaleOrderIdForUpdateAsync(saleOrder.Id);
        if (bill is null)
        {
            bill = await CreateBillAsync(saleOrder);
            await customerBillRepository.AddAsync(bill);
        }
        else
        {
            bill.CustomerNameSnapshot = saleOrder.CustomerNameSnapshot;
            bill.SaleOrderNoSnapshot = saleOrder.OrderNo;
            bill.ApplyUpdateAudit(currentUserService);
        }

        var currentOrderDetails = BuildOrderAcceptanceDetails(bill.Id, saleOrder);
        var currentOrderDetailIds = currentOrderDetails.Select(x => x.SourceDetailId).ToHashSet();
        var existingOrderDetails = bill.Details
            .Where(x => x.SourceType == CustomerBillDetailSourceType.OrderAcceptance)
            .ToList();
        foreach (var obsolete in existingOrderDetails.Where(x => !currentOrderDetailIds.Contains(x.SourceDetailId)))
        {
            bill.Details.Remove(obsolete);
        }

        var existingBySourceDetailId = existingOrderDetails.ToDictionary(x => x.SourceDetailId);
        foreach (var detail in currentOrderDetails)
        {
            if (existingBySourceDetailId.TryGetValue(detail.SourceDetailId, out var existing))
            {
                UpdateDetail(existing, detail);
                existing.ApplyUpdateAudit(currentUserService);
            }
            else
            {
                await AddNewDetailAsync(bill, detail);
            }
        }

        var completedAfterSales = await afterSaleRepository.GetCompletedBySaleOrderIdAsync(saleOrder.Id);
        foreach (var afterSale in completedAfterSales)
        {
            await AddMissingAfterSaleAdjustmentsAsync(bill, afterSale);
        }

        Recalculate(bill);
        return bill;
    }

    /// <inheritdoc />
    public async Task<CustomerBill?> ApplyAfterSaleAdjustmentAsync(AfterSale afterSale)
    {
        if (afterSale.AfterStatus != AfterSaleStatus.Completed)
        {
            throw new BusinessException("售后完成后才能同步客户账单调整");
        }

        if (!afterSale.SaleOrderId.HasValue)
        {
            return null;
        }

        var saleOrder = afterSale.SaleOrder
                        ?? throw new BusinessException("售后单缺少来源销售订单，无法同步客户账单");
        var bill = await customerBillRepository.GetBySaleOrderIdForUpdateAsync(saleOrder.Id);
        if (bill is null)
        {
            if (saleOrder.OrderStatus != SaleOrderStatus.Signed)
            {
                return null;
            }

            bill = await CreateBillAsync(saleOrder);
            foreach (var detail in BuildOrderAcceptanceDetails(bill.Id, saleOrder))
            {
                detail.ApplyCreateAudit(currentUserService);
                bill.Details.Add(detail);
            }

            await customerBillRepository.AddAsync(bill);
        }

        await AddMissingAfterSaleAdjustmentsAsync(bill, afterSale);
        Recalculate(bill);
        bill.ApplyUpdateAudit(currentUserService);
        return bill;
    }

    private async Task<CustomerBill> CreateBillAsync(SaleOrder saleOrder)
    {
        var bill = new CustomerBill
        {
            Id = Guid.NewGuid(),
            BillNo = await documentNoGenerator.NextAsync(
                DocumentNoKind.CustomerBill,
                no => customerBillRepository.ExistsBillNoAsync(no)),
            CustomerId = saleOrder.CustomerId,
            CustomerNameSnapshot = saleOrder.CustomerNameSnapshot,
            SaleOrderId = saleOrder.Id,
            SaleOrderNoSnapshot = saleOrder.OrderNo,
            BillDate = DateTime.UtcNow,
            BillStatus = CustomerBillStatus.Pending
        };
        bill.ApplyCreateAudit(currentUserService);
        return bill;
    }

    private static List<CustomerBillDetail> BuildOrderAcceptanceDetails(Guid billId, SaleOrder saleOrder)
    {
        return saleOrder.Details
            .OrderBy(x => x.Id)
            .Select(detail =>
            {
                var acceptedBaseQuantity = NumericPrecision.RoundQuantity(
                    detail.CustomerCheckBaseQuantity ?? detail.BaseQuantity);
                var conversionRate = EnsureConversionRate(detail.UnitConversion, detail.GoodsNameSnapshot);
                var quantity = NumericPrecision.RoundQuantity(acceptedBaseQuantity / conversionRate);
                var amount = NumericPrecision.RoundMoney(detail.CustomerCheckPrice ?? detail.TotalPrice);
                var unitPrice = quantity == 0m
                    ? NumericPrecision.RoundMoney(detail.FixedPrice)
                    : NumericPrecision.RoundMoney(amount / quantity);

                return new CustomerBillDetail
                {
                    Id = Guid.NewGuid(),
                    CustomerBillId = billId,
                    SourceType = CustomerBillDetailSourceType.OrderAcceptance,
                    SourceDocumentId = saleOrder.Id,
                    SourceDetailId = detail.Id,
                    SaleOrderDetailId = detail.Id,
                    GoodsId = detail.GoodsId,
                    GoodsNameSnapshot = detail.GoodsNameSnapshot,
                    GoodsCodeSnapshot = detail.GoodsCodeSnapshot,
                    GoodsTypeNameSnapshot = detail.GoodsTypeNameSnapshot,
                    GoodsUnitId = detail.GoodsUnitId,
                    GoodsUnitNameSnapshot = detail.GoodsUnitNameSnapshot,
                    BaseUnitId = detail.BaseUnitId,
                    BaseUnitNameSnapshot = detail.BaseUnitNameSnapshot,
                    Quantity = quantity,
                    BaseQuantity = acceptedBaseQuantity,
                    ConversionRate = conversionRate,
                    UnitPrice = unitPrice,
                    Amount = amount,
                    BusinessTime = DateTime.UtcNow,
                    Remark = Normalize(detail.Remark)
                };
            })
            .ToList();
    }

    private static List<CustomerBillDetail> BuildAfterSaleAdjustmentDetails(Guid billId, AfterSale afterSale)
    {
        return afterSale.Goods
            .Where(x => NumericPrecision.RoundMoney(x.RefundAmount) > 0m)
            .OrderBy(x => x.Id)
            .Select(goods =>
            {
                var quantity = -NumericPrecision.RoundQuantity(goods.ActualRefundQuantity);
                var baseQuantity = -NumericPrecision.RoundQuantity(goods.BaseRefundQuantity);
                var refundAmount = -NumericPrecision.RoundMoney(goods.RefundAmount);
                return new CustomerBillDetail
                {
                    Id = Guid.NewGuid(),
                    CustomerBillId = billId,
                    SourceType = CustomerBillDetailSourceType.AfterSaleAdjustment,
                    SourceDocumentId = afterSale.Id,
                    SourceDetailId = goods.Id,
                    SaleOrderDetailId = goods.SaleOrderDetailId,
                    AfterSaleId = afterSale.Id,
                    AfterSaleGoodsId = goods.Id,
                    GoodsId = goods.GoodsId,
                    GoodsNameSnapshot = goods.GoodsNameSnapshot,
                    GoodsCodeSnapshot = goods.GoodsCodeSnapshot,
                    GoodsTypeNameSnapshot = goods.GoodsTypeNameSnapshot,
                    GoodsUnitId = goods.GoodsUnitId,
                    GoodsUnitNameSnapshot = goods.GoodsUnitNameSnapshot,
                    BaseUnitId = goods.BaseUnitId,
                    BaseUnitNameSnapshot = goods.BaseUnitNameSnapshot,
                    Quantity = quantity,
                    BaseQuantity = baseQuantity,
                    ConversionRate = EnsureConversionRate(goods.ConversionRate, goods.GoodsNameSnapshot),
                    UnitPrice = NumericPrecision.RoundMoney(goods.UnitPrice),
                    Amount = refundAmount,
                    BusinessTime = DateTime.UtcNow,
                    Remark = Normalize(goods.Remark)
                };
            })
            .ToList();
    }

    private static void Recalculate(CustomerBill bill)
    {
        bill.OrderAmount = NumericPrecision.RoundMoney(
            bill.Details
                .Where(x => x.SourceType == CustomerBillDetailSourceType.OrderAcceptance)
                .Sum(x => x.Amount));
        bill.AfterSaleAdjustmentAmount = NumericPrecision.RoundMoney(
            bill.Details
                .Where(x => x.SourceType == CustomerBillDetailSourceType.AfterSaleAdjustment)
                .Sum(x => x.Amount));
        bill.ReceivableAmount = NumericPrecision.RoundMoney(
            Math.Max(0m, bill.OrderAmount + bill.AfterSaleAdjustmentAmount));

        if (bill.SettledAmount == 0m)
        {
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

    private async Task AddMissingAfterSaleAdjustmentsAsync(CustomerBill bill, AfterSale afterSale)
    {
        var existingAdjustmentIds = bill.Details
            .Where(x => x.SourceType == CustomerBillDetailSourceType.AfterSaleAdjustment)
            .Select(x => x.SourceDetailId)
            .ToHashSet();
        foreach (var detail in BuildAfterSaleAdjustmentDetails(bill.Id, afterSale)
                     .Where(detail => !existingAdjustmentIds.Contains(detail.SourceDetailId)))
        {
            await AddNewDetailAsync(bill, detail);
        }
    }

    private async Task AddNewDetailAsync(CustomerBill bill, CustomerBillDetail detail)
    {
        detail.ApplyCreateAudit(currentUserService);
        bill.Details.Add(detail);
        await customerBillRepository.AddDetailAsync(detail);
    }

    private static void UpdateDetail(CustomerBillDetail target, CustomerBillDetail source)
    {
        target.SourceDocumentId = source.SourceDocumentId;
        target.SaleOrderDetailId = source.SaleOrderDetailId;
        target.AfterSaleId = source.AfterSaleId;
        target.AfterSaleGoodsId = source.AfterSaleGoodsId;
        target.GoodsId = source.GoodsId;
        target.GoodsNameSnapshot = source.GoodsNameSnapshot;
        target.GoodsCodeSnapshot = source.GoodsCodeSnapshot;
        target.GoodsTypeNameSnapshot = source.GoodsTypeNameSnapshot;
        target.GoodsUnitId = source.GoodsUnitId;
        target.GoodsUnitNameSnapshot = source.GoodsUnitNameSnapshot;
        target.BaseUnitId = source.BaseUnitId;
        target.BaseUnitNameSnapshot = source.BaseUnitNameSnapshot;
        target.Quantity = source.Quantity;
        target.BaseQuantity = source.BaseQuantity;
        target.ConversionRate = source.ConversionRate;
        target.UnitPrice = source.UnitPrice;
        target.Amount = source.Amount;
        target.BusinessTime = source.BusinessTime;
        target.Remark = source.Remark;
    }



    private static decimal EnsureConversionRate(decimal conversionRate, string goodsName)
    {
        var rounded = NumericPrecision.RoundQuantity(conversionRate);
        if (rounded <= 0m)
        {
            throw new BusinessException($"商品 {goodsName} 的单位换算率无效，无法生成客户账单");
        }

        return rounded;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
