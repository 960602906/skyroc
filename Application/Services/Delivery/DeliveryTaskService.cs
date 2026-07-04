using Application.DTOs.Delivery;
using Application.Exceptions;
using Application.Extensions;
using Application.interfaces;
using Application.QueryParameters;
using AutoMapper;
using Domain.Entities;
using Domain.Entities.Delivery;
using Domain.Entities.Orders;
using Domain.Entities.Storage;
using Domain.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared.Constants;
using ValidationException = Application.Exceptions.ValidationException;

namespace Application.Services;

/// <summary>
/// 配送任务应用服务，事务化编排任务生成、司机路线调度、配送状态流转、客户验收和回单归档。
/// </summary>
public class DeliveryTaskService(
    IDeliveryTaskRepository deliveryTaskRepository,
    IStockOutOrderRepository stockOutOrderRepository,
    ISaleOrderRepository saleOrderRepository,
    IOrderReceiptRepository orderReceiptRepository,
    IDriverRepository driverRepository,
    IDeliveryRouteRepository deliveryRouteRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IValidator<AssignDeliveryDriverDto> assignDriverValidator,
    IValidator<IntelligentPlanDeliveryTasksDto> intelligentPlanValidator,
    IValidator<SignDeliveryTaskDto> signValidator,
    IValidator<ReturnOrderReceiptDto> returnReceiptValidator,
    ILogger<DeliveryTaskService> logger) : IDeliveryTaskService
{
    /// <inheritdoc />
    public Task<PagedResult<DeliveryTaskDto>> GetOrderTasksAsync(DeliveryTaskQueryParameters parameters)
    {
        return GetPagedAsync(parameters, false);
    }

    /// <inheritdoc />
    public Task<PagedResult<DeliveryTaskDto>> GetDriverTasksAsync(DeliveryTaskQueryParameters parameters)
    {
        return GetPagedAsync(parameters, true);
    }

    /// <inheritdoc />
    public async Task<DeliveryTaskDto> GetByIdAsync(Guid id)
    {
        return mapper.Map<DeliveryTaskDto>(await GetRequiredTaskAsync(id));
    }

    /// <inheritdoc />
    public async Task<DeliveryTaskDto> GenerateFromStockOutAsync(Guid stockOutOrderId)
    {
        if (stockOutOrderId == Guid.Empty)
        {
            throw new BusinessException("销售出库单 ID 不能为空");
        }

        Guid taskId = Guid.Empty;
        var created = false;
        await ExecuteInTransactionAsync(async () =>
        {
            var stockOut = await stockOutOrderRepository.GetByIdForUpdateAsync(stockOutOrderId)
                           ?? throw new NotFoundException("销售出库单不存在");
            if (stockOut.OrderType != StockOutOrderType.Sale)
            {
                throw new BusinessException("只有销售出库单可以生成配送任务");
            }

            if (stockOut.BusinessStatus != StockDocumentStatus.Audited)
            {
                throw new BusinessException("销售出库单审核通过后才能生成配送任务");
            }

            if (!stockOut.SaleOrderId.HasValue || !stockOut.CustomerId.HasValue)
            {
                throw new BusinessException("销售出库单缺少销售订单或客户，无法生成配送任务");
            }

            var existing = await deliveryTaskRepository.GetByStockOutOrderIdAsync(stockOut.Id);
            if (existing is not null)
            {
                taskId = existing.Id;
                return;
            }

            var saleOrder = await saleOrderRepository.GetByIdAsync(stockOut.SaleOrderId.Value)
                            ?? throw new BusinessException("销售出库单关联的销售订单不存在");
            var task = new DeliveryTask
            {
                Id = Guid.NewGuid(),
                TaskNo = await GenerateTaskNoAsync(),
                StockOutOrderId = stockOut.Id,
                SaleOrderId = saleOrder.Id,
                CustomerId = stockOut.CustomerId.Value,
                CustomerNameSnapshot = stockOut.CustomerNameSnapshot ?? saleOrder.CustomerNameSnapshot,
                ContactNameSnapshot = saleOrder.ContactNameSnapshot,
                ContactPhoneSnapshot = saleOrder.ContactPhoneSnapshot,
                DeliveryAddressSnapshot = saleOrder.DeliveryAddressSnapshot,
                WareId = stockOut.WareId,
                WareNameSnapshot = stockOut.WareNameSnapshot,
                DeliveryStatus = DeliveryTaskStatus.PendingAssign,
                OutTime = stockOut.OutTime,
                Remark = Normalize(stockOut.Remark)
            };
            ApplyCreateAudit(task);
            await deliveryTaskRepository.AddAsync(task);
            taskId = task.Id;
            created = true;
        });

        logger.LogInformation(
            created ? "销售出库生成配送任务成功: {StockOutOrderId}, {DeliveryTaskId}" : "销售出库已存在配送任务: {StockOutOrderId}, {DeliveryTaskId}",
            stockOutOrderId,
            taskId);
        return mapper.Map<DeliveryTaskDto>(await GetRequiredTaskAsync(taskId));
    }

    /// <inheritdoc />
    public async Task<List<DeliveryTaskDto>> AssignDriverAsync(AssignDeliveryDriverDto dto)
    {
        await ValidateAsync(assignDriverValidator, dto);
        var taskIds = dto.TaskIds.OrderBy(x => x).ToList();
        Driver? assignedDriver = null;
        await ExecuteInTransactionAsync(async () =>
        {
            var driver = await driverRepository.GetByIdForUpdateAsync(dto.DriverId)
                         ?? throw new BusinessException("司机不存在");
            if (driver.Status != Status.Enable)
            {
                throw new BusinessException("禁用司机不能分配配送任务");
            }

            assignedDriver = driver;
            foreach (var taskId in taskIds)
            {
                var task = await GetRequiredTaskForUpdateAsync(taskId);
                EnsureDispatchEditable(task, "分配司机");
                task.DriverId = driver.Id;
                task.DriverNameSnapshot = driver.Name;
                task.DriverPhoneSnapshot = driver.Phone;
                task.CarrierId = driver.CarrierId;
                task.CarrierNameSnapshot = driver.Carrier?.Name;
                task.AssignedTime = DateTime.UtcNow;
                task.DeliveryStatus = DeliveryTaskStatus.Assigned;
                ApplyUpdateAudit(task);
                await deliveryTaskRepository.UpdateAsync(task);
            }
        });

        logger.LogInformation("配送任务批量分配司机成功: {DriverId}, {TaskCount}", assignedDriver!.Id, taskIds.Count);
        return await LoadDtosAsync(taskIds);
    }

    /// <inheritdoc />
    public async Task<List<DeliveryTaskDto>> IntelligentPlanAsync(IntelligentPlanDeliveryTasksDto dto)
    {
        await ValidateAsync(intelligentPlanValidator, dto);
        var taskIds = dto.TaskIds.OrderBy(x => x).ToList();
        var tasks = new List<DeliveryTask>(taskIds.Count);
        await ExecuteInTransactionAsync(async () =>
        {
            foreach (var taskId in taskIds)
            {
                var task = await GetRequiredTaskForUpdateAsync(taskId);
                EnsureDispatchEditable(task, "规划路线");
                tasks.Add(task);
            }

            var relations = await deliveryRouteRepository.GetEnabledCustomerRelationsAsync(
                tasks.Select(x => x.CustomerId).Distinct().ToList());
            var selectedRoutes = relations
                .GroupBy(x => x.CustomerId)
                .ToDictionary(group => group.Key, group => group.First());
            var missingCustomers = tasks
                .Where(task => !selectedRoutes.ContainsKey(task.CustomerId))
                .Select(task => task.CustomerNameSnapshot)
                .Distinct()
                .ToList();
            if (missingCustomers.Count > 0)
            {
                throw new BusinessException($"以下客户未配置启用配送路线：{string.Join("、", missingCustomers)}");
            }

            var plannedTime = DateTime.UtcNow;
            foreach (var task in tasks)
            {
                var relation = selectedRoutes[task.CustomerId];
                task.RouteId = relation.RouteId;
                task.RouteNameSnapshot = relation.Route!.Name;
                task.RouteSequence = relation.Sort;
                task.PlannedTime = plannedTime;
                ApplyUpdateAudit(task);
                await deliveryTaskRepository.UpdateAsync(task);
            }
        });

        logger.LogInformation("配送任务智能路线规划成功: {TaskCount}", taskIds.Count);
        return await LoadDtosAsync(taskIds);
    }

    /// <inheritdoc />
    public async Task<DeliveryTaskDto> StartDeliveryAsync(Guid id)
    {
        Guid saleOrderId = Guid.Empty;
        await ExecuteInTransactionAsync(async () =>
        {
            var task = await GetRequiredTaskForUpdateAsync(id);
            if (task.DeliveryStatus != DeliveryTaskStatus.Assigned)
            {
                throw new BusinessException($"配送任务 {task.TaskNo} 只有已分配状态才能开始配送");
            }

            var saleOrder = await saleOrderRepository.GetByIdForUpdateAsync(task.SaleOrderId)
                            ?? throw new BusinessException("配送任务关联的销售订单不存在");
            if (saleOrder.OrderStatus is SaleOrderStatus.PendingAudit or SaleOrderStatus.Rejected or SaleOrderStatus.Signed)
            {
                throw new BusinessException($"销售订单 {saleOrder.OrderNo} 当前状态不允许开始配送");
            }

            task.DeliveryStatus = DeliveryTaskStatus.Delivering;
            task.StartedTime = DateTime.UtcNow;
            ApplyUpdateAudit(task);
            await deliveryTaskRepository.UpdateAsync(task);

            saleOrder.OrderStatus = SaleOrderStatus.Delivering;
            ApplyUpdateAudit(saleOrder);
            await saleOrderRepository.UpdateAsync(saleOrder);
            saleOrderId = saleOrder.Id;
        });

        logger.LogInformation("配送任务开始执行: {DeliveryTaskId}, {SaleOrderId}", id, saleOrderId);
        return mapper.Map<DeliveryTaskDto>(await GetRequiredTaskAsync(id));
    }

    /// <inheritdoc />
    public async Task<OrderReceiptDto> SignAsync(Guid id, SignDeliveryTaskDto dto)
    {
        await ValidateAsync(signValidator, dto);
        Guid receiptId = Guid.Empty;
        await ExecuteInTransactionAsync(async () =>
        {
            var task = await GetRequiredTaskForUpdateAsync(id);
            if (task.DeliveryStatus != DeliveryTaskStatus.Delivering)
            {
                throw new BusinessException($"配送任务 {task.TaskNo} 只有配送中状态才能签收");
            }

            if (await orderReceiptRepository.GetByDeliveryTaskIdAsync(task.Id) is not null)
            {
                throw new BusinessException($"配送任务 {task.TaskNo} 已存在签收回单");
            }

            var saleOrder = await saleOrderRepository.GetByIdForUpdateAsync(task.SaleOrderId)
                            ?? throw new BusinessException("配送任务关联的销售订单不存在");
            var checkDetails = BuildCheckDetails(task, dto.Details);
            var signedTime = DateTime.UtcNow;
            var receipt = new OrderReceipt
            {
                Id = Guid.NewGuid(),
                ReceiptNo = await GenerateReceiptNoAsync(),
                DeliveryTaskId = task.Id,
                SaleOrderId = task.SaleOrderId,
                StockOutOrderId = task.StockOutOrderId,
                SignerName = dto.SignerName.Trim(),
                SignedTime = signedTime,
                SignRemark = Normalize(dto.Remark),
                CheckDetails = checkDetails
            };
            ApplyCreateAudit(receipt);
            foreach (var detail in checkDetails)
            {
                detail.OrderReceiptId = receipt.Id;
                ApplyCreateAudit(detail);
            }

            await orderReceiptRepository.AddAsync(receipt);
            task.DeliveryStatus = DeliveryTaskStatus.Signed;
            task.SignedTime = signedTime;
            ApplyUpdateAudit(task);
            await deliveryTaskRepository.UpdateAsync(task);

            var hasIncompleteDeliveries = await deliveryTaskRepository.HasIncompleteDeliveriesAsync(
                task.SaleOrderId,
                task.Id);
            if (!hasIncompleteDeliveries && saleOrder.OutStorageStatus == OrderOutStorageStatus.Generated)
            {
                await ApplyCompletedOrderAcceptanceAsync(saleOrder, checkDetails);
            }

            receiptId = receipt.Id;
        });

        logger.LogInformation("配送任务签收成功: {DeliveryTaskId}, {OrderReceiptId}", id, receiptId);
        return mapper.Map<OrderReceiptDto>(await GetRequiredReceiptAsync(receiptId));
    }

    /// <inheritdoc />
    public async Task<OrderReceiptDto> ReturnReceiptAsync(Guid id, ReturnOrderReceiptDto dto)
    {
        await ValidateAsync(returnReceiptValidator, dto);
        Guid receiptId = Guid.Empty;
        await ExecuteInTransactionAsync(async () =>
        {
            var task = await GetRequiredTaskForUpdateAsync(id);
            if (task.DeliveryStatus != DeliveryTaskStatus.Signed)
            {
                throw new BusinessException($"配送任务 {task.TaskNo} 签收后才能归档回单");
            }

            var receipt = await orderReceiptRepository.GetByDeliveryTaskIdAsync(task.Id)
                          ?? throw new BusinessException("配送任务缺少签收回单");
            if (receipt.ReturnedTime.HasValue)
            {
                throw new BusinessException($"签收回单 {receipt.ReceiptNo} 已经归档，不能重复回单");
            }

            var saleOrder = await saleOrderRepository.GetByIdForUpdateAsync(task.SaleOrderId)
                            ?? throw new BusinessException("配送任务关联的销售订单不存在");
            receipt.ReceiptImageUrl = dto.ReceiptImageUrl.Trim();
            receipt.ReturnedTime = DateTime.UtcNow;
            receipt.ReturnRemark = Normalize(dto.Remark);
            ApplyUpdateAudit(receipt);
            await orderReceiptRepository.UpdateAsync(receipt);

            var hasIncompleteDeliveries = await deliveryTaskRepository.HasIncompleteDeliveriesAsync(
                task.SaleOrderId,
                task.Id);
            var hasUnreturnedReceipts = await orderReceiptRepository.HasUnreturnedReceiptsAsync(
                task.SaleOrderId,
                receipt.Id);
            if (!hasIncompleteDeliveries
                && !hasUnreturnedReceipts
                && saleOrder.OrderStatus == SaleOrderStatus.Signed)
            {
                saleOrder.ReturnStatus = OrderReturnStatus.Returned;
                ApplyUpdateAudit(saleOrder);
                await saleOrderRepository.UpdateAsync(saleOrder);
            }

            receiptId = receipt.Id;
        });

        logger.LogInformation("配送签收回单归档成功: {DeliveryTaskId}, {OrderReceiptId}", id, receiptId);
        return mapper.Map<OrderReceiptDto>(await GetRequiredReceiptAsync(receiptId));
    }

    private async Task<PagedResult<DeliveryTaskDto>> GetPagedAsync(
        DeliveryTaskQueryParameters parameters,
        bool driverTasksOnly)
    {
        var result = await deliveryTaskRepository.GetPagedAsync(
            parameters.QueryBuild(driverTasksOnly),
            parameters.Current,
            parameters.Size,
            x => x.OutTime,
            true);
        return mapper.ToPagedResult<DeliveryTask, DeliveryTaskDto>(result, parameters);
    }

    private async Task<List<DeliveryTaskDto>> LoadDtosAsync(IEnumerable<Guid> taskIds)
    {
        var orderedIds = taskIds.ToList();
        var tasks = await deliveryTaskRepository.GetByIdsAsync(orderedIds);
        var tasksById = tasks.ToDictionary(x => x.Id);
        return orderedIds.Select(id => mapper.Map<DeliveryTaskDto>(tasksById[id])).ToList();
    }

    private async Task<DeliveryTask> GetRequiredTaskAsync(Guid id)
    {
        return await deliveryTaskRepository.GetByIdAsync(id)
               ?? throw new NotFoundException("配送任务不存在");
    }

    private async Task<DeliveryTask> GetRequiredTaskForUpdateAsync(Guid id)
    {
        return await deliveryTaskRepository.GetByIdForUpdateAsync(id)
               ?? throw new NotFoundException("配送任务不存在");
    }

    private async Task<OrderReceipt> GetRequiredReceiptAsync(Guid id)
    {
        return await orderReceiptRepository.GetByIdAsync(id)
               ?? throw new NotFoundException("签收回单不存在");
    }

    private List<OrderCheckDetail> BuildCheckDetails(
        DeliveryTask task,
        IReadOnlyCollection<SignDeliveryCheckDetailDto> requestedDetails)
    {
        var outboundDetails = task.StockOutOrder.Details.OrderBy(x => x.Id).ToList();
        if (outboundDetails.Count == 0)
        {
            throw new BusinessException("配送任务来源销售出库单没有商品明细");
        }

        var requestedById = requestedDetails.ToDictionary(x => x.StockOutDetailId);
        var expectedIds = outboundDetails.Select(x => x.Id).ToHashSet();
        if (requestedById.Count != expectedIds.Count || !expectedIds.SetEquals(requestedById.Keys))
        {
            throw new BusinessException("签收验收明细必须完整覆盖本配送任务的全部销售出库商品行");
        }

        var result = new List<OrderCheckDetail>(outboundDetails.Count);
        foreach (var outbound in outboundDetails)
        {
            if (!outbound.SaleOrderDetailId.HasValue)
            {
                throw new BusinessException($"销售出库明细 {outbound.Id} 缺少来源订单明细");
            }

            if (outbound.ConversionRate <= 0)
            {
                throw new BusinessException($"销售出库明细 {outbound.Id} 的单位换算率无效");
            }

            var requested = requestedById[outbound.Id];
            var acceptedBaseQuantity = NumericPrecision.RoundQuantity(requested.AcceptedBaseQuantity);
            var deliveredBaseQuantity = NumericPrecision.RoundQuantity(outbound.BaseQuantity);
            if (acceptedBaseQuantity > deliveredBaseQuantity)
            {
                throw new BusinessException($"商品 {outbound.GoodsNameSnapshot} 的客户确认数量不能超过本次配送数量");
            }

            var acceptedQuantity = NumericPrecision.RoundQuantity(acceptedBaseQuantity / outbound.ConversionRate);
            result.Add(new OrderCheckDetail
            {
                Id = Guid.NewGuid(),
                SaleOrderDetailId = outbound.SaleOrderDetailId.Value,
                StockOutDetailId = outbound.Id,
                GoodsId = outbound.GoodsId,
                GoodsNameSnapshot = outbound.GoodsNameSnapshot,
                GoodsCodeSnapshot = outbound.GoodsCodeSnapshot,
                GoodsUnitId = outbound.GoodsUnitId,
                GoodsUnitNameSnapshot = outbound.GoodsUnitNameSnapshot,
                DeliveredBaseQuantity = deliveredBaseQuantity,
                AcceptedBaseQuantity = acceptedBaseQuantity,
                CheckStatus = requested.CheckStatus,
                AcceptedAmount = NumericPrecision.RoundMoney(acceptedQuantity * outbound.UnitPrice),
                Remark = Normalize(requested.Remark)
            });
        }

        return result;
    }

    private async Task ApplyCompletedOrderAcceptanceAsync(
        SaleOrder saleOrder,
        IReadOnlyCollection<OrderCheckDetail> currentDetails)
    {
        var existingDetails = await orderReceiptRepository.GetCheckDetailsBySaleOrderAsync(saleOrder.Id);
        var acceptedByOrderDetail = existingDetails
            .Concat(currentDetails)
            .GroupBy(x => x.SaleOrderDetailId)
            .ToDictionary(
                group => group.Key,
                group => new
                {
                    Quantity = NumericPrecision.RoundQuantity(group.Sum(x => x.AcceptedBaseQuantity)),
                    Amount = NumericPrecision.RoundMoney(group.Sum(x => x.AcceptedAmount)),
                    HasRejected = group.Any(x => x.CheckStatus == OrderCustomerCheckStatus.Rejected)
                });

        foreach (var orderDetail in saleOrder.Details)
        {
            acceptedByOrderDetail.TryGetValue(orderDetail.Id, out var accepted);
            var acceptedQuantity = accepted?.Quantity ?? 0m;
            if (acceptedQuantity > NumericPrecision.RoundQuantity(orderDetail.BaseQuantity))
            {
                throw new BusinessException($"商品 {orderDetail.GoodsNameSnapshot} 的累计验收数量超过订单数量");
            }

            orderDetail.CustomerCheckBaseQuantity = acceptedQuantity;
            orderDetail.CustomerCheckPrice = accepted?.Amount ?? 0m;
            orderDetail.CustomerCheckStatus = acceptedQuantity == NumericPrecision.RoundQuantity(orderDetail.BaseQuantity)
                                              && accepted is { HasRejected: false }
                ? OrderCustomerCheckStatus.Accepted
                : OrderCustomerCheckStatus.Rejected;
            ApplyUpdateAudit(orderDetail);
        }

        saleOrder.SettlementPrice = NumericPrecision.RoundMoney(
            saleOrder.Details.Sum(x => x.CustomerCheckPrice ?? 0m));
        saleOrder.OrderStatus = SaleOrderStatus.Signed;
        ApplyUpdateAudit(saleOrder);
        await saleOrderRepository.UpdateAsync(saleOrder);
    }

    private async Task<string> GenerateTaskNoAsync()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var suffix = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
            var taskNo = $"DT{DateTime.UtcNow:yyyyMMddHHmmssfff}{suffix}";
            if (!await deliveryTaskRepository.ExistsTaskNoAsync(taskNo))
            {
                return taskNo;
            }
        }

        throw new BusinessException("配送任务编号生成失败，请重试");
    }

    private async Task<string> GenerateReceiptNoAsync()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var suffix = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
            var receiptNo = $"OR{DateTime.UtcNow:yyyyMMddHHmmssfff}{suffix}";
            if (!await orderReceiptRepository.ExistsReceiptNoAsync(receiptNo))
            {
                return receiptNo;
            }
        }

        throw new BusinessException("签收回单编号生成失败，请重试");
    }

    private static void EnsureDispatchEditable(DeliveryTask task, string operation)
    {
        if (task.DeliveryStatus is not (DeliveryTaskStatus.PendingAssign or DeliveryTaskStatus.Assigned))
        {
            throw new BusinessException($"配送任务 {task.TaskNo} 当前状态不允许{operation}");
        }
    }

    private async Task ExecuteInTransactionAsync(Func<Task> action)
    {
        await unitOfWork.BeginTransactionAsync();
        try
        {
            await action();
            await unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            if (unitOfWork.HasActiveTransaction)
            {
                await unitOfWork.RollbackTransactionAsync();
            }

            throw;
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
