using Application.Exceptions;
using Application.Interfaces;
using Domain.Entities;
using Domain.Entities.Finance;
using Domain.Entities.Storage;
using Domain.Interfaces;
using Shared.Constants;
using Application.Extensions;

namespace Application.Services;

/// <summary>
/// 供应商待结单据同步服务，负责把采购入库和采购退货出库审核金额写入应付单据。
/// </summary>
public class SupplierBillService(
    ISupplierBillRepository supplierBillRepository,
    ISupplierSettlementRepository supplierSettlementRepository,
    ICurrentUserService currentUserService,
    IDocumentNoGenerator documentNoGenerator) : ISupplierBillService
{
    /// <inheritdoc />
    public async Task<SupplierBill> SyncPurchaseStockInAsync(StockInOrder stockInOrder)
    {
        if (stockInOrder.OrderType != StockInOrderType.Purchase)
        {
            throw new BusinessException("仅采购入库审核后才能生成供应商待结单据");
        }

        if (stockInOrder.BusinessStatus != StockDocumentStatus.Audited)
        {
            throw new BusinessException("采购入库审核通过后才能生成供应商待结单据");
        }

        if (!stockInOrder.SupplierId.HasValue)
        {
            throw new BusinessException("采购入库缺少供应商，无法生成供应商待结单据");
        }

        var bill = await supplierBillRepository.GetByStockInOrderIdForUpdateAsync(stockInOrder.Id);
        if (bill is null)
        {
            bill = await CreateBillAsync(
                stockInOrder.SupplierId.Value,
                stockInOrder.SupplierNameSnapshot ?? string.Empty,
                SupplierBillSourceType.PurchaseStockIn,
                stockInOrder.Id,
                null,
                stockInOrder.InNo,
                stockInOrder.AuditTime ?? stockInOrder.InTime);
            await supplierBillRepository.AddAsync(bill);
        }
        else
        {
            bill.SupplierNameSnapshot = stockInOrder.SupplierNameSnapshot ?? bill.SupplierNameSnapshot;
            bill.SourceDocumentNoSnapshot = stockInOrder.InNo;
            bill.ApplyUpdateAudit(currentUserService);
        }

        await RebuildDetailsAsync(
            bill,
            SupplierBillSourceType.PurchaseStockIn,
            stockInOrder.Id,
            stockInOrder.Details
                .OrderBy(x => x.Id)
                .Select(detail => BuildStockInDetail(bill.Id, stockInOrder, detail))
                .ToList(),
            stockInOrder.AuditTime ?? stockInOrder.InTime);
        Recalculate(bill);
        return bill;
    }

    /// <inheritdoc />
    public async Task<SupplierBill> SyncPurchaseReturnOutAsync(StockOutOrder stockOutOrder)
    {
        if (stockOutOrder.OrderType != StockOutOrderType.PurchaseReturn)
        {
            throw new BusinessException("仅采购退货出库审核后才能生成供应商待结单据");
        }

        if (stockOutOrder.BusinessStatus != StockDocumentStatus.Audited)
        {
            throw new BusinessException("采购退货出库审核通过后才能生成供应商待结单据");
        }

        if (!stockOutOrder.SupplierId.HasValue)
        {
            throw new BusinessException("采购退货出库缺少供应商，无法生成供应商待结单据");
        }

        var bill = await supplierBillRepository.GetByStockOutOrderIdForUpdateAsync(stockOutOrder.Id);
        if (bill is null)
        {
            bill = await CreateBillAsync(
                stockOutOrder.SupplierId.Value,
                stockOutOrder.SupplierNameSnapshot ?? string.Empty,
                SupplierBillSourceType.PurchaseReturnOut,
                null,
                stockOutOrder.Id,
                stockOutOrder.OutNo,
                stockOutOrder.AuditTime ?? stockOutOrder.OutTime);
            await supplierBillRepository.AddAsync(bill);
        }
        else
        {
            bill.SupplierNameSnapshot = stockOutOrder.SupplierNameSnapshot ?? bill.SupplierNameSnapshot;
            bill.SourceDocumentNoSnapshot = stockOutOrder.OutNo;
            bill.ApplyUpdateAudit(currentUserService);
        }

        await RebuildDetailsAsync(
            bill,
            SupplierBillSourceType.PurchaseReturnOut,
            stockOutOrder.Id,
            stockOutOrder.Details
                .OrderBy(x => x.Id)
                .Select(detail => BuildStockOutDetail(bill.Id, stockOutOrder, detail))
                .ToList(),
            stockOutOrder.AuditTime ?? stockOutOrder.OutTime);
        Recalculate(bill);
        return bill;
    }

    /// <inheritdoc />
    public async Task EnsureCanReverseSourceDocumentAsync(Guid? stockInOrderId, Guid? stockOutOrderId)
    {
        SupplierBill? bill = null;
        if (stockInOrderId.HasValue)
        {
            bill = await supplierBillRepository.GetByStockInOrderIdAsync(stockInOrderId.Value);
        }
        else if (stockOutOrderId.HasValue)
        {
            bill = await supplierBillRepository.GetByStockOutOrderIdAsync(stockOutOrderId.Value);
        }

        if (bill is null)
        {
            return;
        }

        if (bill.SettledAmount > 0m)
        {
            throw new BusinessException($"供应商待结单据 {bill.BillNo} 已有结算金额，来源出入库单不能反审核");
        }

        if (await supplierSettlementRepository.ExistsDetailByBillIdAsync(bill.Id))
        {
            throw new BusinessException($"供应商待结单据 {bill.BillNo} 已存在结算记录（含已作废），来源出入库单不能反审核");
        }
    }

    /// <inheritdoc />
    public async Task RemoveBySourceDocumentAsync(Guid? stockInOrderId, Guid? stockOutOrderId)
    {
        SupplierBill? bill = null;
        if (stockInOrderId.HasValue)
        {
            bill = await supplierBillRepository.GetByStockInOrderIdForUpdateAsync(stockInOrderId.Value);
        }
        else if (stockOutOrderId.HasValue)
        {
            bill = await supplierBillRepository.GetByStockOutOrderIdForUpdateAsync(stockOutOrderId.Value);
        }

        if (bill is null)
        {
            return;
        }

        if (bill.SettledAmount > 0m)
        {
            throw new BusinessException($"供应商待结单据 {bill.BillNo} 已有结算金额，不能删除");
        }

        if (await supplierSettlementRepository.ExistsDetailByBillIdAsync(bill.Id))
        {
            throw new BusinessException($"供应商待结单据 {bill.BillNo} 已存在结算记录（含已作废），来源出入库单不能反审核");
        }

        await supplierBillRepository.DeleteAsync(bill);
    }

    private async Task<SupplierBill> CreateBillAsync(
        Guid supplierId,
        string supplierNameSnapshot,
        SupplierBillSourceType sourceType,
        Guid? stockInOrderId,
        Guid? stockOutOrderId,
        string sourceDocumentNo,
        DateTime billDate)
    {
        var bill = new SupplierBill
        {
            Id = Guid.NewGuid(),
            BillNo = await documentNoGenerator.NextAsync(
                DocumentNoKind.SupplierBill,
                no => supplierBillRepository.ExistsBillNoAsync(no)),
            SupplierId = supplierId,
            SupplierNameSnapshot = supplierNameSnapshot,
            SourceType = sourceType,
            StockInOrderId = stockInOrderId,
            StockOutOrderId = stockOutOrderId,
            SourceDocumentNoSnapshot = sourceDocumentNo,
            BillDate = billDate,
            BillStatus = SupplierBillStatus.Pending
        };
        bill.ApplyCreateAudit(currentUserService);
        return bill;
    }

    private async Task RebuildDetailsAsync(
        SupplierBill bill,
        SupplierBillSourceType sourceType,
        Guid sourceDocumentId,
        IReadOnlyList<SupplierBillDetail> currentDetails,
        DateTime businessTime)
    {
        var currentDetailIds = currentDetails.Select(x => x.SourceDetailId).ToHashSet();
        var existingDetails = bill.Details.Where(x => x.SourceType == sourceType).ToList();
        foreach (var obsolete in existingDetails.Where(x => !currentDetailIds.Contains(x.SourceDetailId)))
        {
            bill.Details.Remove(obsolete);
        }

        var existingBySourceDetailId = existingDetails.ToDictionary(x => x.SourceDetailId);
        foreach (var detail in currentDetails)
        {
            detail.BusinessTime = businessTime;
            detail.SourceDocumentId = sourceDocumentId;
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
    }

    private static SupplierBillDetail BuildStockInDetail(Guid billId, StockInOrder order, StockInDetail detail)
    {
        var quantity = NumericPrecision.RoundQuantity(detail.Quantity);
        var baseQuantity = NumericPrecision.RoundQuantity(detail.BaseQuantity);
        var amount = NumericPrecision.RoundMoney(detail.TotalPrice);
        return new SupplierBillDetail
        {
            Id = Guid.NewGuid(),
            SupplierBillId = billId,
            SourceType = SupplierBillSourceType.PurchaseStockIn,
            SourceDocumentId = order.Id,
            SourceDetailId = detail.Id,
            StockInOrderId = order.Id,
            StockInDetailId = detail.Id,
            GoodsId = detail.GoodsId,
            GoodsNameSnapshot = detail.GoodsNameSnapshot,
            GoodsCodeSnapshot = detail.GoodsCodeSnapshot,
            GoodsUnitId = detail.GoodsUnitId,
            GoodsUnitNameSnapshot = detail.GoodsUnitNameSnapshot,
            Quantity = quantity,
            BaseQuantity = baseQuantity,
            ConversionRate = EnsureConversionRate(detail.ConversionRate, detail.GoodsNameSnapshot),
            UnitPrice = NumericPrecision.RoundMoney(detail.UnitPrice),
            Amount = amount,
            Remark = Normalize(detail.Remark)
        };
    }

    private static SupplierBillDetail BuildStockOutDetail(Guid billId, StockOutOrder order, StockOutDetail detail)
    {
        var quantity = -NumericPrecision.RoundQuantity(detail.Quantity);
        var baseQuantity = -NumericPrecision.RoundQuantity(detail.BaseQuantity);
        var amount = -NumericPrecision.RoundMoney(detail.TotalPrice);
        return new SupplierBillDetail
        {
            Id = Guid.NewGuid(),
            SupplierBillId = billId,
            SourceType = SupplierBillSourceType.PurchaseReturnOut,
            SourceDocumentId = order.Id,
            SourceDetailId = detail.Id,
            StockOutOrderId = order.Id,
            StockOutDetailId = detail.Id,
            GoodsId = detail.GoodsId,
            GoodsNameSnapshot = detail.GoodsNameSnapshot,
            GoodsCodeSnapshot = detail.GoodsCodeSnapshot,
            GoodsUnitId = detail.GoodsUnitId,
            GoodsUnitNameSnapshot = detail.GoodsUnitNameSnapshot,
            Quantity = quantity,
            BaseQuantity = baseQuantity,
            ConversionRate = EnsureConversionRate(detail.ConversionRate, detail.GoodsNameSnapshot),
            UnitPrice = NumericPrecision.RoundMoney(detail.UnitPrice),
            Amount = amount,
            Remark = Normalize(detail.Remark)
        };
    }

    private static void Recalculate(SupplierBill bill)
    {
        bill.DocumentAmount = NumericPrecision.RoundMoney(
            bill.Details.Sum(x => Math.Abs(x.Amount)));
        bill.PayableAmount = NumericPrecision.RoundMoney(
            bill.Details.Sum(x => x.Amount));

        if (bill.SettledAmount == 0m)
        {
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

    private async Task AddNewDetailAsync(SupplierBill bill, SupplierBillDetail detail)
    {
        detail.ApplyCreateAudit(currentUserService);
        bill.Details.Add(detail);
        await supplierBillRepository.AddDetailAsync(detail);
    }

    private static void UpdateDetail(SupplierBillDetail target, SupplierBillDetail source)
    {
        target.SourceDocumentId = source.SourceDocumentId;
        target.StockInOrderId = source.StockInOrderId;
        target.StockInDetailId = source.StockInDetailId;
        target.StockOutOrderId = source.StockOutOrderId;
        target.StockOutDetailId = source.StockOutDetailId;
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
            throw new BusinessException($"商品 {goodsName} 的单位换算率无效，无法生成供应商待结单据");
        }

        return rounded;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
