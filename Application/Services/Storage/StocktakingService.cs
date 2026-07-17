using Application.DTOs.Storage;
using Application.Exceptions;
using Application.Extensions;
using Application.Interfaces;
using Application.QueryParameters;
using AutoMapper;
using Domain.Entities;
using Domain.Entities.Storage;
using Domain.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared.Constants;
using static Shared.Constants.NumericPrecision;
using ValidationException = Application.Exceptions.ValidationException;

namespace Application.Services;

/// <summary>
/// 库存盘点应用服务，创建批次账实快照，并在审核事务中执行一次性的盘盈盘亏调整。
/// </summary>
public class StocktakingService(
    IStocktakingOrderRepository stocktakingOrderRepository,
    IStockBatchRepository stockBatchRepository,
    IStockLedgerRepository stockLedgerRepository,
    IWareRepository wareRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IDocumentNoGenerator documentNoGenerator,
    IValidator<CreateStocktakingDto> createValidator,
    ILogger<StocktakingService> logger) : IStocktakingService
{
    /// <inheritdoc />
    public async Task<PagedResult<StocktakingOrderDto>> GetPagedAsync(StocktakingQueryParameters parameters)
    {
        var result = await stocktakingOrderRepository.GetPagedAsync(
            parameters.QueryBuild(),
            parameters.Current,
            parameters.Size,
            x => x.StocktakingTime,
            true);
        return mapper.ToPagedResult<StocktakingOrder, StocktakingOrderDto>(result, parameters);
    }

    /// <inheritdoc />
    public async Task<StocktakingOrderDto> GetByIdAsync(Guid id)
    {
        return mapper.Map<StocktakingOrderDto>(await GetRequiredOrderAsync(id));
    }

    /// <inheritdoc />
    public async Task<StocktakingOrderDto> CreateAsync(CreateStocktakingDto dto)
    {
        await createValidator.ValidateOrThrowAsync(dto);
        var ware = await wareRepository.GetByIdAsync(dto.WareId)
                   ?? throw new BusinessException("盘点仓库不存在");
        var snapshotTime = DateTime.UtcNow;
        var order = new StocktakingOrder
        {
            Id = Guid.NewGuid(),
            StocktakingNo = await documentNoGenerator.NextAsync(
                DocumentNoKind.Stocktaking,
                no => stocktakingOrderRepository.ExistsStocktakingNoAsync(no)),
            BusinessStatus = StockDocumentStatus.Draft,
            WareId = ware.Id,
            WareNameSnapshot = ware.Name,
            StocktakingTime = snapshotTime,
            Remark = Normalize(dto.Remark)
        };
        order.ApplyCreateAudit(currentUserService);

        var requestedBatchIds = dto.Details.Select(detail => detail.StockBatchId).ToArray();
        var batchesById = (await stockBatchRepository.GetByIdsAsync(requestedBatchIds))
            .ToDictionary(batch => batch.Id);
        foreach (var request in dto.Details)
        {
            if (!batchesById.TryGetValue(request.StockBatchId, out var batch))
            {
                throw new BusinessException($"库存批次 {request.StockBatchId} 不存在");
            }

            if (batch.WareId != ware.Id)
            {
                throw new BusinessException($"库存批次 {batch.BatchNo} 不属于盘点仓库 {ware.Name}");
            }

            var bookQuantity = RoundQuantity(batch.CurrentQuantity);
            var actualQuantity = RoundQuantity(request.ActualQuantity);
            var differenceQuantity = RoundQuantity(actualQuantity - bookQuantity);
            var detail = new StocktakingDetail
            {
                Id = Guid.NewGuid(),
                StocktakingOrderId = order.Id,
                StockBatchId = batch.Id,
                GoodsId = batch.GoodsId,
                GoodsNameSnapshot = batch.GoodsNameSnapshot,
                GoodsCodeSnapshot = batch.GoodsCodeSnapshot,
                BatchNoSnapshot = batch.BatchNo,
                BaseUnitId = batch.BaseUnitId,
                BaseUnitNameSnapshot = batch.BaseUnitNameSnapshot,
                BookQuantity = bookQuantity,
                ActualQuantity = actualQuantity,
                DifferenceQuantity = differenceQuantity,
                UnitCost = RoundMoney(batch.UnitCost),
                DifferenceAmount = RoundMoney(differenceQuantity * batch.UnitCost),
                Remark = Normalize(request.Remark)
            };
            detail.ApplyCreateAudit(currentUserService);
            order.Details.Add(detail);
        }

        RecalculateTotals(order);
        await stocktakingOrderRepository.AddAsync(order);
        await unitOfWork.SaveChangesAsync();
        logger.LogInformation("库存盘点创建成功: {StocktakingOrderId}, {StocktakingNo}", order.Id, order.StocktakingNo);
        return mapper.Map<StocktakingOrderDto>(order);
    }

    /// <inheritdoc />
    public async Task<StocktakingOrderDto> AuditAsync(Guid id, string? remark)
    {
        var auditTime = DateTime.UtcNow;
        var normalizedRemark = Normalize(remark);
        StocktakingOrder? auditedOrder = null;
        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var order = await stocktakingOrderRepository.GetByIdForUpdateAsync(id)
                        ?? throw new NotFoundException("库存盘点单不存在");
            if (order.IsAdjustmentApplied)
            {
                throw new BusinessException($"盘点单 {order.StocktakingNo} 已执行库存调整，不能重复审核");
            }

            if (order.BusinessStatus is not (StockDocumentStatus.Draft or StockDocumentStatus.PendingAudit))
            {
                throw new BusinessException($"盘点单 {order.StocktakingNo} 当前状态不允许审核");
            }

            if (order.Details.Count == 0)
            {
                throw new BusinessException($"盘点单 {order.StocktakingNo} 没有批次明细，无法审核");
            }

            if ((await stockLedgerRepository.GetActiveBySourceOrderAsync(order.Id)).Count > 0)
            {
                throw new BusinessException($"盘点单 {order.StocktakingNo} 已存在生效的调整流水，不能重复审核");
            }

            foreach (var detail in order.Details
                         .OrderBy(detail => detail.StockBatchId)
                         .ThenBy(detail => detail.Id))
            {
                var batch = await stockBatchRepository.GetByIdentityForUpdateAsync(
                    order.WareId,
                    detail.GoodsId,
                    detail.BatchNoSnapshot);
                if (batch is null || batch.Id != detail.StockBatchId)
                {
                    throw new BusinessException($"库存批次 {detail.BatchNoSnapshot} 不存在或已变更");
                }

                EnsureSnapshotIsCurrent(order, detail, batch);
                var differenceQuantity = RoundQuantity(detail.ActualQuantity - detail.BookQuantity);
                detail.DifferenceQuantity = differenceQuantity;
                detail.DifferenceAmount = RoundMoney(differenceQuantity * detail.UnitCost);
                detail.ApplyUpdateAudit(currentUserService);
                if (differenceQuantity == 0m)
                {
                    continue;
                }

                ApplyAdjustmentToBatch(batch, differenceQuantity, auditTime);
                await stockBatchRepository.UpdateAsync(batch);
                await stockLedgerRepository.AddAsync(
                    CreateAdjustmentLedger(order, detail, batch, auditTime, normalizedRemark));
            }

            RecalculateTotals(order);
            order.BusinessStatus = StockDocumentStatus.Audited;
            order.IsAdjustmentApplied = true;
            order.AdjustmentTime = auditTime;
            order.AuditUserId = currentUserService.GetUserId();
            order.AuditUserNameSnapshot = currentUserService.GetUserName();
            order.AuditTime = auditTime;
            order.ApplyUpdateAudit(currentUserService);
            await stocktakingOrderRepository.UpdateAsync(order);
            auditedOrder = order;
        });

        var completedOrder = auditedOrder!;
        logger.LogInformation(
            "库存盘点审核调整成功: {StocktakingOrderId}, {StocktakingNo}",
            completedOrder.Id,
            completedOrder.StocktakingNo);
        return mapper.Map<StocktakingOrderDto>(await GetRequiredOrderAsync(completedOrder.Id));
    }

    private static void EnsureSnapshotIsCurrent(
        StocktakingOrder order,
        StocktakingDetail detail,
        StockBatch batch)
    {
        if (RoundQuantity(batch.CurrentQuantity) != detail.BookQuantity
            || batch.LastMovementTime > order.StocktakingTime)
        {
            throw new BusinessException(
                $"批次 {batch.BatchNo} 在盘点快照后发生库存变更，请重新盘点");
        }
    }

    private void ApplyAdjustmentToBatch(StockBatch batch, decimal differenceQuantity, DateTime auditTime)
    {
        var adjustedCurrentQuantity = RoundQuantity(batch.CurrentQuantity + differenceQuantity);
        var adjustedAvailableQuantity = RoundQuantity(batch.AvailableQuantity + differenceQuantity);
        if (differenceQuantity < 0m
            && (adjustedCurrentQuantity < 0m || adjustedAvailableQuantity < 0m))
        {
            throw new BusinessException(
                $"批次 {batch.BatchNo} 可用库存不足，无法执行盘亏调整 {Math.Abs(differenceQuantity)}");
        }

        batch.CurrentQuantity = adjustedCurrentQuantity;
        batch.AvailableQuantity = adjustedAvailableQuantity;
        batch.LastMovementTime = auditTime;
        batch.ApplyUpdateAudit(currentUserService);
    }

    private StockLedger CreateAdjustmentLedger(
        StocktakingOrder order,
        StocktakingDetail detail,
        StockBatch batch,
        DateTime auditTime,
        string? remark)
    {
        var changeQuantity = Math.Abs(detail.DifferenceQuantity);
        var ledger = new StockLedger
        {
            Id = Guid.NewGuid(),
            StockBatchId = batch.Id,
            WareId = order.WareId,
            WareNameSnapshot = order.WareNameSnapshot,
            GoodsId = detail.GoodsId,
            GoodsNameSnapshot = detail.GoodsNameSnapshot,
            GoodsCodeSnapshot = detail.GoodsCodeSnapshot,
            BatchNoSnapshot = detail.BatchNoSnapshot,
            BaseUnitNameSnapshot = detail.BaseUnitNameSnapshot,
            Direction = detail.DifferenceQuantity > 0m
                ? StockLedgerDirection.Increase
                : StockLedgerDirection.Decrease,
            SourceType = StockLedgerSourceType.Stocktaking,
            SourceOrderId = order.Id,
            SourceDetailId = detail.Id,
            ChangeQuantity = changeQuantity,
            BalanceQuantity = batch.CurrentQuantity,
            UnitCost = detail.UnitCost,
            TotalCost = RoundMoney(changeQuantity * detail.UnitCost),
            OccurredTime = auditTime,
            Remark = remark ?? order.Remark
        };
        ledger.ApplyCreateAudit(currentUserService);
        return ledger;
    }

    private static void RecalculateTotals(StocktakingOrder order)
    {
        order.TotalBookQuantity = RoundQuantity(order.Details.Sum(detail => detail.BookQuantity));
        order.TotalActualQuantity = RoundQuantity(order.Details.Sum(detail => detail.ActualQuantity));
        order.TotalDifferenceQuantity = RoundQuantity(order.Details.Sum(detail => detail.DifferenceQuantity));
    }

    private async Task<StocktakingOrder> GetRequiredOrderAsync(Guid id)
    {
        return await stocktakingOrderRepository.GetByIdAsync(id)
               ?? throw new NotFoundException("库存盘点单不存在");
    }




    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

}
