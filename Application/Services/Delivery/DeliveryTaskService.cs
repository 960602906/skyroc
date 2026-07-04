using Application.DTOs.Delivery;
using Application.Exceptions;
using Application.Extensions;
using Application.interfaces;
using Application.QueryParameters;
using AutoMapper;
using Domain.Entities;
using Domain.Entities.Delivery;
using Domain.Entities.Storage;
using Domain.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared.Constants;
using ValidationException = Application.Exceptions.ValidationException;

namespace Application.Services;

/// <summary>
/// 配送任务应用服务，事务化生成来源任务、分配司机并按客户路线执行批量规划。
/// </summary>
public class DeliveryTaskService(
    IDeliveryTaskRepository deliveryTaskRepository,
    IStockOutOrderRepository stockOutOrderRepository,
    ISaleOrderRepository saleOrderRepository,
    IDriverRepository driverRepository,
    IDeliveryRouteRepository deliveryRouteRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IValidator<AssignDeliveryDriverDto> assignDriverValidator,
    IValidator<IntelligentPlanDeliveryTasksDto> intelligentPlanValidator,
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
