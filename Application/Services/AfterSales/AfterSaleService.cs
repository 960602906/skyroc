using Application.DTOs.AfterSales;
using Application.Exceptions;
using Application.Extensions;
using Application.Interfaces;
using Application.QueryParameters.AfterSales;
using AutoMapper;
using Domain.Entities;
using Domain.Entities.AfterSales;
using Domain.Entities.Customers;
using Domain.Entities.Goods;
using Domain.Entities.Orders;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using Domain.Interfaces;
using Domain.ReadModels.AfterSales;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared.Constants;
using GoodsEntity = Domain.Entities.Goods.Goods;
using ValidationException = Application.Exceptions.ValidationException;

namespace Application.Services;

/// <summary>
/// 售后流程服务，事务化维护草稿、来源数量占用、取货生成、退货入库完成条件和审核轨迹。
/// </summary>
public class AfterSaleService(
    IAfterSaleRepository afterSaleRepository,
    IAfterSaleGoodsRepository afterSaleGoodsRepository,
    IAfterSaleAuditLogRepository auditLogRepository,
    IPickupTaskRepository pickupTaskRepository,
    IStockInOrderRepository stockInOrderRepository,
    ISaleOrderRepository saleOrderRepository,
    ICustomerBillService customerBillService,
    ICustomerRepository customerRepository,
    IGoodsRepository goodsRepository,
    IGoodsUnitRepository goodsUnitRepository,
    ISupplierRepository supplierRepository,
    IDepartmentRepository departmentRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IDocumentNoGenerator documentNoGenerator,
    IValidator<CreateAfterSaleDto> createValidator,
    IValidator<UpdateAfterSaleDto> updateValidator,
    ILogger<AfterSaleService> logger) : IAfterSaleService
{
    /// <inheritdoc />
    public async Task<PagedResult<AfterSaleListItemDto>> GetPagedAsync(AfterSaleQueryParameters parameters)
    {
        var result = await afterSaleRepository.GetListPageAsync(
            parameters.QueryBuild(),
            parameters.Current,
            parameters.Size);
        return mapper.ToPagedResult<AfterSaleListItemReadModel, AfterSaleListItemDto>(result, parameters);
    }

    /// <inheritdoc />
    public async Task<AfterSaleDto> GetByIdAsync(Guid id)
    {
        var entity = await afterSaleRepository.GetByIdAsync(id)
                     ?? throw new NotFoundException("售后单不存在");
        return mapper.Map<AfterSaleDto>(entity);
    }

    /// <inheritdoc />
    public async Task<AfterSaleDto> CreateAsync(CreateAfterSaleDto dto)
    {
        await createValidator.ValidateOrThrowAsync(dto);
        var afterSaleId = Guid.NewGuid();

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var saleOrder = await GetAndValidateSaleOrderAsync(dto.SaleOrderId, dto.CustomerId);
            var customer = await GetRequiredCustomerAsync(saleOrder?.CustomerId ?? dto.CustomerId!.Value);
            var goods = await BuildGoodsAsync(afterSaleId, dto.Goods, saleOrder, null);
            var entity = new AfterSale
            {
                Id = afterSaleId,
                AfterSaleNo = await documentNoGenerator.NextAsync(
                    DocumentNoKind.AfterSale,
                    no => afterSaleRepository.ExistsAfterSaleNoAsync(no)),
                SaleOrderId = saleOrder?.Id,
                SaleOrderNoSnapshot = saleOrder?.OrderNo,
                CustomerId = customer.Id,
                CustomerNameSnapshot = customer.Name,
                Source = dto.Source.Trim(),
                AfterStatus = AfterSaleStatus.Draft,
                OrderPrice = saleOrder?.SettlementPrice ?? 0m,
                ContactNameSnapshot = Normalize(dto.ContactName),
                ContactPhoneSnapshot = Normalize(dto.ContactPhone),
                PickupAddressSnapshot = Normalize(dto.PickupAddress),
                Remark = Normalize(dto.Remark),
                Goods = goods
            };
            entity.SettlementPrice = CalculateSettlementPrice(entity.OrderPrice, goods, saleOrder is not null);
            entity.ApplyCreateAudit(currentUserService);
            foreach (var item in goods)
            {
                item.ApplyCreateAudit(currentUserService);
            }

            await afterSaleRepository.AddAsync(entity);
        });

        logger.LogInformation("售后单创建成功: {AfterSaleId}", afterSaleId);
        return await GetByIdAsync(afterSaleId);
    }

    /// <inheritdoc />
    public async Task<AfterSaleDto> UpdateAsync(UpdateAfterSaleDto dto)
    {
        await updateValidator.ValidateOrThrowAsync(dto);
        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var entity = await GetRequiredForUpdateAsync(dto.Id);
            EnsureStatus(entity, AfterSaleStatus.Draft, "编辑");
            var saleOrder = await GetAndValidateSaleOrderAsync(entity.SaleOrderId, entity.CustomerId);
            var replacement = await BuildGoodsAsync(entity.Id, dto.Goods, saleOrder, entity.Id);
            var originalGoods = entity.Goods.ToList();
            await afterSaleGoodsRepository.DeleteRangeAsync(originalGoods);
            // 先在同一事务内落库删除，避免相同来源订单行的替换记录先插入而触发唯一约束。
            await unitOfWork.SaveChangesAsync();
            foreach (var item in replacement)
            {
                item.ApplyCreateAudit(currentUserService);
            }
            await afterSaleGoodsRepository.AddRangeAsync(replacement);

            entity.ContactNameSnapshot = Normalize(dto.ContactName);
            entity.ContactPhoneSnapshot = Normalize(dto.ContactPhone);
            entity.PickupAddressSnapshot = Normalize(dto.PickupAddress);
            entity.Remark = Normalize(dto.Remark);
            entity.SettlementPrice = CalculateSettlementPrice(entity.OrderPrice, replacement, saleOrder is not null);
            entity.ApplyUpdateAudit(currentUserService);
        });

        logger.LogInformation("售后单更新成功: {AfterSaleId}", dto.Id);
        return await GetByIdAsync(dto.Id);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id)
    {
        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var entity = await GetRequiredForUpdateAsync(id);
            EnsureStatus(entity, AfterSaleStatus.Draft, "删除");
            if (entity.AuditLogs.Count > 0)
            {
                throw new BusinessException("已提交过的售后单必须保留审核历史，不能删除");
            }

            await afterSaleRepository.DeleteAsync(entity);
        });

        logger.LogInformation("售后草稿删除成功: {AfterSaleId}", id);
        return true;
    }

    /// <inheritdoc />
    public Task<AfterSaleDto> SubmitAsync(Guid id, string? remark)
    {
        return TransitionAsync(id, AfterSaleAuditAction.Submit, remark, entity =>
        {
            EnsureStatus(entity, AfterSaleStatus.Draft, "提交");
            if (entity.AuditLogs.Count > 0)
            {
                throw new BusinessException("已驳回的售后单请执行重提操作");
            }

            return AfterSaleStatus.PendingAudit;
        });
    }

    /// <inheritdoc />
    public async Task<AfterSaleDto> ApproveAsync(Guid id, string? remark)
    {
        EnsureOptionalRemark(remark, "操作说明");
        var repeated = false;
        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var entity = await GetRequiredForUpdateAsync(id);
            if (entity.AfterStatus is AfterSaleStatus.ReturnPending or AfterSaleStatus.RefundPending
                && GetLatestAuditAction(entity) == AfterSaleAuditAction.Approve)
            {
                await GeneratePickupTasksAsync(entity);
                repeated = true;
                return;
            }

            EnsureStatus(entity, AfterSaleStatus.PendingAudit, "审核通过");
            var previousStatus = entity.AfterStatus;
            var targetStatus = RequiresPhysicalHandling(entity.Goods)
                ? AfterSaleStatus.ReturnPending
                : AfterSaleStatus.RefundPending;
            entity.AfterStatus = targetStatus;
            entity.ApplyUpdateAudit(currentUserService);
            await GeneratePickupTasksAsync(entity);
            await auditLogRepository.AddAsync(CreateAuditLog(
                entity.Id,
                AfterSaleAuditAction.Approve,
                previousStatus,
                targetStatus,
                remark));
        });

        logger.LogInformation(
            repeated ? "售后审核重试保持幂等: {AfterSaleId}" : "售后审核通过: {AfterSaleId}",
            id);
        return await GetByIdAsync(id);
    }

    /// <inheritdoc />
    public Task<AfterSaleDto> RejectAsync(Guid id, string? remark)
    {
        EnsureRequiredRemark(remark, "驳回原因");
        return TransitionAsync(id, AfterSaleAuditAction.Reject, remark, entity =>
        {
            EnsureStatus(entity, AfterSaleStatus.PendingAudit, "驳回");
            return AfterSaleStatus.Draft;
        });
    }

    /// <inheritdoc />
    public Task<AfterSaleDto> ResubmitAsync(Guid id, string? remark)
    {
        return TransitionAsync(id, AfterSaleAuditAction.Resubmit, remark, entity =>
        {
            EnsureStatus(entity, AfterSaleStatus.Draft, "重提");
            var latestAction = GetLatestAuditAction(entity);
            if (latestAction != AfterSaleAuditAction.Reject)
            {
                throw new BusinessException("仅已驳回的售后单可以重新提交");
            }

            return AfterSaleStatus.PendingAudit;
        });
    }

    /// <inheritdoc />
    public Task<AfterSaleDto> ReverseAsync(Guid id, string? remark)
    {
        EnsureRequiredRemark(remark, "反审核说明");
        return TransitionAsync(id, AfterSaleAuditAction.Reverse, remark, entity =>
        {
            if (entity.AfterStatus is not (AfterSaleStatus.ReturnPending or AfterSaleStatus.RefundPending))
            {
                throw new BusinessException($"售后状态为 {entity.AfterStatus}，不能执行反审核操作");
            }

            if (entity.PickupTasks.Count > 0)
            {
                throw new BusinessException("售后单已生成取货任务，不能反审核");
            }

            return AfterSaleStatus.PendingAudit;
        });
    }

    /// <inheritdoc />
    public async Task<AfterSaleDto> CompleteAsync(Guid id)
    {
        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var entity = await GetRequiredForUpdateAsync(id);
            if (entity.AfterStatus is not (AfterSaleStatus.ReturnPending or AfterSaleStatus.RefundPending))
            {
                throw new BusinessException($"售后状态为 {entity.AfterStatus}，不能执行完成操作");
            }

            if (entity.PickupTasks.Any(task => task.PickupStatus != PickupTaskStatus.Completed))
            {
                throw new BusinessException("售后单仍有未完成的取货任务，不能完成处理");
            }

            var returnGoodsIds = entity.Goods
                .Where(goods => goods.AfterSaleType == AfterSaleType.ReturnAndRefund)
                .Select(goods => goods.Id)
                .ToHashSet();
            if (!returnGoodsIds.SetEquals(entity.PickupTasks.Select(task => task.AfterSaleGoodsId)))
            {
                throw new BusinessException("售后退货商品缺少对应取货任务，不能完成处理");
            }

            if (entity.PickupTasks.Any(task => task.StockInDetail is null))
            {
                throw new BusinessException("售后单仍有未完成审核的销售退货入库，不能完成处理");
            }

            var stockInOrderIds = entity.PickupTasks
                .Select(task => task.StockInDetail!.StockInOrderId)
                .Distinct()
                .OrderBy(stockInOrderId => stockInOrderId)
                .ToList();
            var stockInOrders = await stockInOrderRepository.GetByIdsForUpdateAsync(stockInOrderIds);
            if (stockInOrders.Count != stockInOrderIds.Count
                || stockInOrders.Any(stockInOrder =>
                    stockInOrder.BusinessStatus != StockDocumentStatus.Audited
                    || stockInOrder.AfterSaleId != entity.Id))
            {
                throw new BusinessException("售后单仍有未完成审核的销售退货入库，不能完成处理");
            }

            entity.AfterStatus = AfterSaleStatus.Completed;
            entity.ApplyUpdateAudit(currentUserService);
            if (entity.SaleOrderId.HasValue)
            {
                entity.SaleOrder = await saleOrderRepository.GetByIdForUpdateAsync(entity.SaleOrderId.Value)
                                   ?? throw new BusinessException("售后单关联的销售订单不存在");
            }

            await customerBillService.ApplyAfterSaleAdjustmentAsync(entity);
        });

        logger.LogInformation("售后处理完成: {AfterSaleId}", id);
        return await GetByIdAsync(id);
    }

    private async Task<AfterSaleDto> TransitionAsync(
        Guid id,
        AfterSaleAuditAction action,
        string? remark,
        Func<AfterSale, AfterSaleStatus> resolveTarget)
    {
        EnsureOptionalRemark(remark, "操作说明");
        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var entity = await GetRequiredForUpdateAsync(id);
            var previousStatus = entity.AfterStatus;
            var targetStatus = resolveTarget(entity);
            entity.AfterStatus = targetStatus;
            entity.ApplyUpdateAudit(currentUserService);
            await auditLogRepository.AddAsync(CreateAuditLog(
                entity.Id,
                action,
                previousStatus,
                targetStatus,
                remark));
        });

        logger.LogInformation("售后状态流转成功: {AfterSaleId}, {Action}", id, action);
        return await GetByIdAsync(id);
    }

    private async Task<List<AfterSaleGoods>> BuildGoodsAsync(
        Guid afterSaleId,
        IReadOnlyCollection<CreateAfterSaleGoodsDto> inputs,
        SaleOrder? saleOrder,
        Guid? excludeAfterSaleId)
    {
        var sourceIds = inputs.Where(x => x.SaleOrderDetailId.HasValue)
            .Select(x => x.SaleOrderDetailId!.Value)
            .ToList();
        if (sourceIds.Count != sourceIds.Distinct().Count())
        {
            throw new BusinessException("同一销售订单商品行不能在一张售后单中重复申请");
        }

        if (sourceIds.Count > 0 && saleOrder is null)
        {
            throw new BusinessException("关联销售订单商品行时必须选择来源销售订单");
        }

        var sourceDetails = saleOrder?.Details.ToDictionary(x => x.Id)
                            ?? new Dictionary<Guid, SaleOrderDetail>();
        var reservedQuantities = await afterSaleRepository.GetReservedBaseQuantitiesAsync(
            sourceIds,
            excludeAfterSaleId);
        var manualGoodsIds = inputs.Where(x => !x.SaleOrderDetailId.HasValue && x.GoodsId.HasValue)
            .Select(x => x.GoodsId!.Value)
            .Distinct()
            .ToList();
        var goodsById = (await goodsRepository.GetByIdsAsync(manualGoodsIds)).ToDictionary(x => x.Id);
        var manualUnitIds = inputs.Where(x => !x.SaleOrderDetailId.HasValue && x.GoodsUnitId.HasValue)
            .Select(x => x.GoodsUnitId!.Value)
            .Distinct()
            .ToList();
        var unitsById = (await goodsUnitRepository.FindAsync(x => manualUnitIds.Contains(x.Id)))
            .ToDictionary(x => x.Id);
        var supplierIds = inputs.Where(x => x.SupplierId.HasValue).Select(x => x.SupplierId!.Value).Distinct().ToList();
        var suppliersById = (await supplierRepository.FindAsync(x => supplierIds.Contains(x.Id)))
            .ToDictionary(x => x.Id);
        var departmentIds = inputs.Where(x => x.DepartmentId.HasValue).Select(x => x.DepartmentId!.Value).Distinct().ToArray();
        var departmentsById = (await departmentRepository.GetByIdsAsync(departmentIds)).ToDictionary(x => x.Id);

        EnsureAllReferencesExist(manualGoodsIds, goodsById.Keys, "部分手工售后商品不存在");
        EnsureAllReferencesExist(manualUnitIds, unitsById.Keys, "部分手工售后商品单位不存在");
        EnsureAllReferencesExist(supplierIds, suppliersById.Keys, "部分供应商不存在");
        EnsureAllReferencesExist(departmentIds, departmentsById.Keys, "部分责任部门不存在");

        var result = new List<AfterSaleGoods>(inputs.Count);
        foreach (var input in inputs)
        {
            var supplier = input.SupplierId.HasValue ? suppliersById[input.SupplierId.Value] : null;
            var department = input.DepartmentId.HasValue ? departmentsById[input.DepartmentId.Value] : null;
            result.Add(input.SaleOrderDetailId.HasValue
                ? BuildFromOrderDetail(
                    afterSaleId,
                    input,
                    GetSourceDetail(sourceDetails, input.SaleOrderDetailId.Value),
                    reservedQuantities.GetValueOrDefault(input.SaleOrderDetailId.Value),
                    supplier,
                    department)
                : BuildManual(
                    afterSaleId,
                    input,
                    goodsById[input.GoodsId!.Value],
                    unitsById[input.GoodsUnitId!.Value],
                    supplier,
                    department));
        }

        return result;
    }

    private static AfterSaleGoods BuildFromOrderDetail(
        Guid afterSaleId,
        CreateAfterSaleGoodsDto input,
        SaleOrderDetail detail,
        decimal reservedBaseQuantity,
        Supplier? supplier,
        Department? department)
    {
        if (input.GoodsId.HasValue && input.GoodsId.Value != detail.GoodsId)
        {
            throw new BusinessException($"售后商品与订单商品 {detail.GoodsNameSnapshot} 不一致");
        }

        if (input.GoodsUnitId.HasValue && input.GoodsUnitId.Value != detail.GoodsUnitId)
        {
            throw new BusinessException($"售后单位与订单商品 {detail.GoodsNameSnapshot} 的下单单位不一致");
        }

        if (detail.UnitConversion <= 0m || detail.BaseQuantity <= 0m || detail.Quantity <= 0m)
        {
            throw new BusinessException($"订单商品 {detail.GoodsNameSnapshot} 的数量或单位换算快照无效");
        }

        var requestedQuantity = GetRoundedPositiveQuantity(input.ActualRefundQuantity, detail.GoodsNameSnapshot);
        var baseQuantity = NumericPrecision.RoundQuantity(requestedQuantity * detail.UnitConversion);
        EnsurePositiveBaseQuantity(baseQuantity, detail.GoodsNameSnapshot);
        var availableBaseQuantity = detail.CustomerCheckBaseQuantity ?? detail.BaseQuantity;
        if (NumericPrecision.RoundQuantity(reservedBaseQuantity + baseQuantity) > availableBaseQuantity)
        {
            throw new BusinessException($"商品 {detail.GoodsNameSnapshot} 的累计售后数量超过可申请数量");
        }

        var unitPrice = NumericPrecision.RoundMoney(detail.TotalPrice / detail.Quantity);
        var refundAmount = RequiresFinancialAdjustment(input.HandleType)
            ? NumericPrecision.RoundMoney(detail.TotalPrice * baseQuantity / detail.BaseQuantity)
            : 0m;
        return CreateGoodsEntity(afterSaleId, input, new AfterSaleGoodsSnapshot
        {
            GoodsId = detail.GoodsId,
            GoodsName = detail.GoodsNameSnapshot,
            GoodsCode = detail.GoodsCodeSnapshot,
            GoodsTypeName = detail.GoodsTypeNameSnapshot,
            GoodsUnitId = detail.GoodsUnitId,
            GoodsUnitName = detail.GoodsUnitNameSnapshot,
            BaseUnitId = detail.BaseUnitId,
            BaseUnitName = detail.BaseUnitNameSnapshot,
            ConversionRate = detail.UnitConversion,
            BaseQuantity = baseQuantity,
            UnitPrice = unitPrice,
            RefundAmount = refundAmount,
            SupplierId = supplier?.Id,
            SupplierName = supplier?.Name,
            DepartmentId = department?.Id,
            DepartmentName = department?.Name
        });
    }

    private static AfterSaleGoods BuildManual(
        Guid afterSaleId,
        CreateAfterSaleGoodsDto input,
        GoodsEntity goods,
        GoodsUnit unit,
        Supplier? supplier,
        Department? department)
    {
        if (unit.GoodsId != goods.Id)
        {
            throw new BusinessException($"申请单位不属于商品 {goods.Name}");
        }

        if (unit.ConversionRate <= 0m || goods.BaseUnit is null)
        {
            throw new BusinessException($"商品 {goods.Name} 未配置有效基础单位和换算比例");
        }

        var requestedQuantity = GetRoundedPositiveQuantity(input.ActualRefundQuantity, goods.Name);
        var baseQuantity = NumericPrecision.RoundQuantity(requestedQuantity * unit.ConversionRate);
        EnsurePositiveBaseQuantity(baseQuantity, goods.Name);
        var unitPrice = NumericPrecision.RoundMoney(input.UnitPrice!.Value);
        var refundAmount = RequiresFinancialAdjustment(input.HandleType)
            ? NumericPrecision.RoundMoney(requestedQuantity * unitPrice)
            : 0m;
        return CreateGoodsEntity(afterSaleId, input, new AfterSaleGoodsSnapshot
        {
            GoodsId = goods.Id,
            GoodsName = goods.Name,
            GoodsCode = goods.Code,
            GoodsTypeName = goods.GoodsType.Name,
            GoodsUnitId = unit.Id,
            GoodsUnitName = unit.Name,
            BaseUnitId = goods.BaseUnit.Id,
            BaseUnitName = goods.BaseUnit.Name,
            ConversionRate = unit.ConversionRate,
            BaseQuantity = baseQuantity,
            UnitPrice = unitPrice,
            RefundAmount = refundAmount,
            SupplierId = supplier?.Id,
            SupplierName = supplier?.Name,
            DepartmentId = department?.Id,
            DepartmentName = department?.Name
        });
    }

    private static AfterSaleGoods CreateGoodsEntity(
        Guid afterSaleId,
        CreateAfterSaleGoodsDto input,
        AfterSaleGoodsSnapshot snapshot)
    {
        return new AfterSaleGoods
        {
            Id = Guid.NewGuid(),
            AfterSaleId = afterSaleId,
            SaleOrderDetailId = input.SaleOrderDetailId,
            GoodsId = snapshot.GoodsId,
            GoodsNameSnapshot = snapshot.GoodsName,
            GoodsCodeSnapshot = snapshot.GoodsCode,
            GoodsTypeNameSnapshot = snapshot.GoodsTypeName,
            GoodsUnitId = snapshot.GoodsUnitId,
            GoodsUnitNameSnapshot = snapshot.GoodsUnitName,
            BaseUnitId = snapshot.BaseUnitId,
            BaseUnitNameSnapshot = snapshot.BaseUnitName,
            ConversionRate = snapshot.ConversionRate,
            AfterSaleType = input.AfterSaleType,
            ActualRefundQuantity = NumericPrecision.RoundQuantity(input.ActualRefundQuantity),
            BaseRefundQuantity = snapshot.BaseQuantity,
            UnitPrice = snapshot.UnitPrice,
            RefundAmount = snapshot.RefundAmount,
            SupplierId = snapshot.SupplierId,
            SupplierNameSnapshot = snapshot.SupplierName,
            DepartmentId = snapshot.DepartmentId,
            DepartmentNameSnapshot = snapshot.DepartmentName,
            ReasonType = input.ReasonType,
            HandleType = input.HandleType,
            Remark = Normalize(input.Remark)
        };
    }

    private async Task<SaleOrder?> GetAndValidateSaleOrderAsync(Guid? saleOrderId, Guid? customerId)
    {
        if (!saleOrderId.HasValue)
        {
            return null;
        }

        var saleOrder = await saleOrderRepository.GetByIdForUpdateAsync(saleOrderId.Value)
                        ?? throw new BusinessException("来源销售订单不存在");
        if (saleOrder.OrderStatus is SaleOrderStatus.PendingAudit or SaleOrderStatus.Rejected)
        {
            throw new BusinessException("来源销售订单尚未审核通过，不能创建售后");
        }

        if (customerId.HasValue && customerId.Value != saleOrder.CustomerId)
        {
            throw new BusinessException("售后客户与来源销售订单客户不一致");
        }

        return saleOrder;
    }

    private async Task<Customer> GetRequiredCustomerAsync(Guid id)
    {
        return await customerRepository.GetByIdAsync(id)
               ?? throw new BusinessException("客户不存在");
    }

    private async Task<AfterSale> GetRequiredForUpdateAsync(Guid id)
    {
        return await afterSaleRepository.GetByIdForUpdateAsync(id)
               ?? throw new NotFoundException("售后单不存在");
    }

    private async Task GeneratePickupTasksAsync(AfterSale entity)
    {
        var returnGoods = entity.Goods
            .Where(x => x.AfterSaleType == AfterSaleType.ReturnAndRefund)
            .OrderBy(x => x.Id)
            .ToList();
        if (returnGoods.Count == 0)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(entity.PickupAddressSnapshot))
        {
            throw new BusinessException("退货退款商品审核通过前必须填写取货地址");
        }

        var existingGoodsIds = entity.PickupTasks.Select(x => x.AfterSaleGoodsId).ToHashSet();
        var tasks = new List<PickupTask>();
        foreach (var goods in returnGoods.Where(x => !existingGoodsIds.Contains(x.Id)))
        {
            var task = new PickupTask
            {
                Id = Guid.NewGuid(),
                TaskNo = await documentNoGenerator.NextAsync(
                    DocumentNoKind.PickupTask,
                    no => pickupTaskRepository.ExistsAsync(x => x.TaskNo == no)),
                AfterSaleId = entity.Id,
                AfterSaleGoodsId = goods.Id,
                ContactNameSnapshot = entity.ContactNameSnapshot,
                ContactPhoneSnapshot = entity.ContactPhoneSnapshot,
                PickupAddressSnapshot = entity.PickupAddressSnapshot.Trim(),
                PickupStatus = PickupTaskStatus.PendingAssign
            };
            task.ApplyCreateAudit(currentUserService);
            tasks.Add(task);
            entity.PickupTasks.Add(task);
        }

        if (tasks.Count > 0)
        {
            await pickupTaskRepository.AddRangeAsync(tasks);
        }
    }

    private AfterSaleAuditLog CreateAuditLog(
        Guid afterSaleId,
        AfterSaleAuditAction action,
        AfterSaleStatus previousStatus,
        AfterSaleStatus currentStatus,
        string? remark)
    {
        var log = new AfterSaleAuditLog
        {
            Id = Guid.NewGuid(),
            AfterSaleId = afterSaleId,
            Action = action,
            PreviousStatus = previousStatus,
            CurrentStatus = currentStatus,
            AuditUserId = currentUserService.GetUserId(),
            AuditUserNameSnapshot = currentUserService.GetUserName() ?? string.Empty,
            AuditTime = DateTime.UtcNow,
            Remark = Normalize(remark)
        };
        log.ApplyCreateAudit(currentUserService);
        return log;
    }

    private static AfterSaleAuditAction? GetLatestAuditAction(AfterSale entity)
    {
        return entity.AuditLogs
            .OrderByDescending(x => x.AuditTime)
            .ThenByDescending(x => x.CreateTime)
            .ThenByDescending(x => x.Id)
            .Select(x => (AfterSaleAuditAction?)x.Action)
            .FirstOrDefault();
    }

    private static bool RequiresPhysicalHandling(IEnumerable<AfterSaleGoods> goods)
    {
        return goods.Any(x => x.AfterSaleType == AfterSaleType.ReturnAndRefund
                              || x.HandleType is AfterSaleHandleType.Replenishment or AfterSaleHandleType.Exchange);
    }

    private static bool RequiresFinancialAdjustment(AfterSaleHandleType handleType)
    {
        return handleType is not (
            AfterSaleHandleType.Replenishment
            or AfterSaleHandleType.Exchange
            or AfterSaleHandleType.CustomerCommunication);
    }

    private static decimal GetRoundedPositiveQuantity(decimal quantity, string goodsName)
    {
        var roundedQuantity = NumericPrecision.RoundQuantity(quantity);
        if (roundedQuantity <= 0m)
        {
            throw new BusinessException($"商品 {goodsName} 的售后数量按系统精度舍入后必须大于零");
        }

        return roundedQuantity;
    }

    private static void EnsurePositiveBaseQuantity(decimal baseQuantity, string goodsName)
    {
        if (baseQuantity <= 0m)
        {
            throw new BusinessException($"商品 {goodsName} 的基础单位售后数量按系统精度舍入后必须大于零");
        }
    }

    private static decimal CalculateSettlementPrice(
        decimal orderPrice,
        IEnumerable<AfterSaleGoods> goods,
        bool hasSourceOrder)
    {
        var refundAmount = NumericPrecision.RoundMoney(goods.Sum(x => x.RefundAmount));
        if (hasSourceOrder && refundAmount > orderPrice)
        {
            throw new BusinessException("售后退款或减免金额不能超过来源订单结算金额");
        }

        return NumericPrecision.RoundMoney(Math.Max(0m, orderPrice - refundAmount));
    }

    private static SaleOrderDetail GetSourceDetail(
        IReadOnlyDictionary<Guid, SaleOrderDetail> details,
        Guid detailId)
    {
        return details.GetValueOrDefault(detailId)
               ?? throw new BusinessException("部分来源订单商品行不存在或不属于当前订单");
    }

    private static void EnsureAllReferencesExist(
        IEnumerable<Guid> requestedIds,
        IEnumerable<Guid> existingIds,
        string message)
    {
        if (requestedIds.Except(existingIds).Any())
        {
            throw new BusinessException(message);
        }
    }

    private static void EnsureStatus(AfterSale entity, AfterSaleStatus requiredStatus, string actionName)
    {
        if (entity.AfterStatus != requiredStatus)
        {
            throw new BusinessException($"售后状态为 {entity.AfterStatus}，不能执行{actionName}操作");
        }
    }

    private static void EnsureRequiredRemark(string? remark, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(remark))
        {
            throw new BusinessException($"{fieldName}不能为空");
        }

        EnsureOptionalRemark(remark, fieldName);
    }

    private static void EnsureOptionalRemark(string? remark, string fieldName)
    {
        if (remark?.Trim().Length > 500)
        {
            throw new BusinessException($"{fieldName}不能超过 500 字符");
        }
    }




    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
