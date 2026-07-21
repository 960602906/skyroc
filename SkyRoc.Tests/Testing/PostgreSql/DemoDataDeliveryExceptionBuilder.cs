using Application.DTOs.Delivery;
using Application.Interfaces;
using Domain.Entities.Delivery;
using Domain.Entities.Storage;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace SkyRoc.Tests.Testing.PostgreSql;

/// <summary>
///     基于完整稳定键的受管溯源销售出库补齐配送异常任务，并经应用服务形成待处理与已处理异常事实。
/// </summary>
internal sealed class DemoDataDeliveryExceptionBuilder(
    ApplicationDbContext context,
    IDeliveryTaskService deliveryTaskService,
    IDeliveryExceptionService deliveryExceptionService,
    Guid auditUserId,
    string auditUsername)
{
    private const int TaskCount = 20;
    private const int ExceptionCountPerTask = 2;
    private const int HandledTaskCount = 10;
    private const string ManagedTaskPrefix = "SKYROC-DEMO-DELIVERY-EXCEPTION-TASK-";
    private const string ManagedExceptionPrefix = "SKYROC-DEMO-DELIVERY-EXCEPTION-";

    /// <summary>
    ///     补齐二十个异常配送任务和四十条异常，前十个任务完成异常闭环，后十个任务保留待处理事实。
    /// </summary>
    public async Task<DemoDataDeliveryExceptionGenerationResult> GenerateAsync(
        CancellationToken cancellationToken)
    {
        var sources = await LoadManagedSourcesAsync(cancellationToken);
        var drivers = await LoadManagedDriversAsync(cancellationToken);
        var routes = await LoadManagedRoutesAsync(cancellationToken);
        var expectedTaskRemarks = Enumerable.Range(1, TaskCount)
            .Select(CreateTaskRemark)
            .ToArray();
        var expectedDescriptions = Enumerable.Range(1, TaskCount * ExceptionCountPerTask)
            .Select(CreateExceptionDescription)
            .ToArray();

        await EnsureManagedKeyScopeAsync(expectedTaskRemarks, expectedDescriptions, cancellationToken);

        var existingTasks = await context.DeliveryTasks
            .AsNoTracking()
            .Where(task => sources.Select(source => source.Id).Contains(task.StockOutOrderId))
            .ToDictionaryAsync(task => task.StockOutOrderId, cancellationToken);
        var createdTasks = 0;
        var reusedTasks = 0;
        var managedTasks = new List<DeliveryTask>(TaskCount);

        for (var sequence = 1; sequence <= TaskCount; sequence++)
        {
            var source = sources[sequence - 1];
            var driver = drivers[sequence - 1];
            var route = routes[sequence - 1];
            var taskWasCreated = !existingTasks.TryGetValue(source.Id, out var task);
            if (taskWasCreated)
            {
                var created = await deliveryTaskService.GenerateFromStockOutAsync(source.Id);
                task = await LoadTaskAsync(created.Id, cancellationToken);
                createdTasks++;
            }
            else
            {
                reusedTasks++;
            }

            task = await PrepareTaskAsync(task!, source, driver, route, sequence, cancellationToken);
            managedTasks.Add(task);
        }

        await EnsureNoUnmanagedTaskExceptionsAsync(managedTasks, expectedDescriptions, cancellationToken);
        var existingExceptions = await context.DeliveryExceptions
            .AsNoTracking()
            .Where(exception => exception.Description.StartsWith(ManagedExceptionPrefix))
            .ToDictionaryAsync(exception => exception.Description, StringComparer.Ordinal, cancellationToken);
        var createdExceptions = 0;
        var reusedExceptions = 0;

        for (var taskIndex = 0; taskIndex < managedTasks.Count; taskIndex++)
        {
            var task = managedTasks[taskIndex];
            var taskExceptions = new List<DeliveryException>(ExceptionCountPerTask);
            for (var slot = 1; slot <= ExceptionCountPerTask; slot++)
            {
                var exceptionSequence = taskIndex * ExceptionCountPerTask + slot;
                var description = CreateExceptionDescription(exceptionSequence);
                if (!existingExceptions.TryGetValue(description, out var deliveryException))
                {
                    var created = await deliveryExceptionService.CreateAsync(new CreateDeliveryExceptionDto
                    {
                        DeliveryTaskId = task.Id,
                        Description = description
                    });
                    deliveryException = await LoadExceptionAsync(created.Id, cancellationToken);
                    existingExceptions.Add(description, deliveryException);
                    createdExceptions++;
                }
                else
                {
                    var trackedException = await context.DeliveryExceptions.SingleAsync(
                        item => item.Id == deliveryException.Id,
                        cancellationToken);
                    trackedException.CreateBy = auditUserId;
                    trackedException.CreateName = auditUsername;
                    await context.SaveChangesAsync(cancellationToken);
                    deliveryException = await LoadExceptionAsync(deliveryException.Id, cancellationToken);
                    existingExceptions[description] = deliveryException;
                    reusedExceptions++;
                }

                taskExceptions.Add(deliveryException);
            }

            var shouldBeHandled = taskIndex < HandledTaskCount;
            if (shouldBeHandled)
            {
                for (var slot = 0; slot < taskExceptions.Count; slot++)
                {
                    var deliveryException = taskExceptions[slot];
                    if (deliveryException.HandleStatus == DeliveryExceptionStatus.Pending)
                    {
                        var exceptionSequence = taskIndex * ExceptionCountPerTask + slot + 1;
                        await deliveryExceptionService.HandleAsync(
                            deliveryException.Id,
                            new HandleDeliveryExceptionDto
                            {
                                HandleRemark = CreateHandleRemark(exceptionSequence)
                            });
                        taskExceptions[slot] = await LoadExceptionAsync(
                            deliveryException.Id,
                            cancellationToken);
                    }
                }
            }

            task = await LoadTaskAsync(task.Id, cancellationToken);
            ValidateTaskFinalState(task, taskIndex + 1, shouldBeHandled);
            for (var slot = 0; slot < taskExceptions.Count; slot++)
            {
                var exceptionSequence = taskIndex * ExceptionCountPerTask + slot + 1;
                var deliveryException = taskExceptions[slot];
                if (shouldBeHandled)
                {
                    var trackedException = await context.DeliveryExceptions.SingleAsync(
                        item => item.Id == deliveryException.Id,
                        cancellationToken);
                    trackedException.UpdateBy = auditUserId;
                    trackedException.UpdateName = auditUsername;
                    trackedException.CreateBy = auditUserId;
                    trackedException.CreateName = auditUsername;
                    await context.SaveChangesAsync(cancellationToken);
                    deliveryException = await LoadExceptionAsync(deliveryException.Id, cancellationToken);
                    taskExceptions[slot] = deliveryException;
                }

                ValidateException(
                    deliveryException,
                    task,
                    exceptionSequence,
                    shouldBeHandled);
            }
        }

        return new DemoDataDeliveryExceptionGenerationResult(
            createdTasks,
            reusedTasks,
            createdExceptions,
            reusedExceptions);
    }

    private async Task<IReadOnlyList<StockOutOrder>> LoadManagedSourcesAsync(
        CancellationToken cancellationToken)
    {
        var expectedRemarks = Enumerable.Range(1, TaskCount)
            .Select(CreateTraceStockOutRemark)
            .ToArray();
        var prefix = $"{DemoDataStableKeyCatalog.ManagedPrefix}-TRACE-STOCK-OUT-";
        var candidates = await context.StockOutOrders
            .AsNoTracking()
            .Where(order => order.Remark != null && order.Remark.StartsWith(prefix))
            .OrderBy(order => order.Remark)
            .ToArrayAsync(cancellationToken);
        EnsureExactKeys(
            candidates.Select(order => order.Remark!),
            expectedRemarks,
            "溯源销售出库备注");
        if (candidates.Length != TaskCount)
        {
            throw new InvalidOperationException(
                $"配送异常需要 {TaskCount} 张受管溯源销售出库，当前为 {candidates.Length} 张。");
        }

        for (var index = 0; index < candidates.Length; index++)
        {
            var source = candidates[index];
            if (source.OrderType != StockOutOrderType.Sale
                || source.BusinessStatus != StockDocumentStatus.Audited
                || !source.SaleOrderId.HasValue
                || !source.CustomerId.HasValue
                || string.IsNullOrWhiteSpace(source.CustomerNameSnapshot)
                || string.IsNullOrWhiteSpace(source.WareNameSnapshot)
                || source.Status != Status.Enable)
            {
                throw new InvalidOperationException(
                    $"受管溯源销售出库 {CreateTraceStockOutRemark(index + 1)} 的类型、状态或业务快照已漂移。");
            }
        }

        return candidates;
    }

    private async Task<IReadOnlyList<Driver>> LoadManagedDriversAsync(
        CancellationToken cancellationToken)
    {
        var codes = Enumerable.Range(1, TaskCount)
            .Select(sequence => DemoDataStableKeyCatalog.Create("DRIVER", sequence))
            .ToArray();
        var drivers = await context.Drivers
            .AsNoTracking()
            .Where(driver => codes.Contains(driver.Code))
            .OrderBy(driver => driver.Code)
            .ToArrayAsync(cancellationToken);
        if (drivers.Length != TaskCount)
        {
            throw new InvalidOperationException(
                $"配送异常需要 {TaskCount} 名受管司机，当前为 {drivers.Length} 名。");
        }

        for (var index = 0; index < drivers.Length; index++)
        {
            if (drivers[index].Code != codes[index]
                || !drivers[index].CarrierId.HasValue
                || drivers[index].Status != Status.Enable)
            {
                throw new InvalidOperationException($"受管司机 {codes[index]} 的编码、承运商或启用状态已漂移。");
            }
        }

        return drivers;
    }

    private async Task<IReadOnlyList<DeliveryRoute>> LoadManagedRoutesAsync(
        CancellationToken cancellationToken)
    {
        var codes = Enumerable.Range(1, TaskCount)
            .Select(sequence => DemoDataStableKeyCatalog.Create("DELIVERY-ROUTE", sequence))
            .ToArray();
        var routes = await context.DeliveryRoutes
            .AsNoTracking()
            .Where(route => codes.Contains(route.Code))
            .OrderBy(route => route.Code)
            .ToArrayAsync(cancellationToken);
        if (routes.Length != TaskCount)
        {
            throw new InvalidOperationException(
                $"配送异常需要 {TaskCount} 条受管配送路线，当前为 {routes.Length} 条。");
        }

        for (var index = 0; index < routes.Length; index++)
        {
            if (routes[index].Code != codes[index] || routes[index].Status != Status.Enable)
            {
                throw new InvalidOperationException($"受管配送路线 {codes[index]} 的编码或启用状态已漂移。");
            }
        }

        return routes;
    }

    private async Task EnsureManagedKeyScopeAsync(
        IReadOnlyCollection<string> expectedTaskRemarks,
        IReadOnlyCollection<string> expectedDescriptions,
        CancellationToken cancellationToken)
    {
        var taskRemarks = await context.DeliveryTasks
            .AsNoTracking()
            .Where(task => task.Remark != null && task.Remark.StartsWith(ManagedTaskPrefix))
            .Select(task => task.Remark!)
            .ToArrayAsync(cancellationToken);
        EnsureExactKeys(taskRemarks, expectedTaskRemarks, "配送异常任务备注");

        var descriptions = await context.DeliveryExceptions
            .AsNoTracking()
            .Where(exception => exception.Description.StartsWith(ManagedExceptionPrefix))
            .Select(exception => exception.Description)
            .ToArrayAsync(cancellationToken);
        EnsureExactKeys(descriptions, expectedDescriptions, "配送异常描述");
    }

    private async Task<DeliveryTask> PrepareTaskAsync(
        DeliveryTask task,
        StockOutOrder source,
        Driver driver,
        DeliveryRoute route,
        int sequence,
        CancellationToken cancellationToken)
    {
        var trackedTask = await context.DeliveryTasks.SingleAsync(
            item => item.Id == task.Id,
            cancellationToken);
        trackedTask.CreateBy = auditUserId;
        trackedTask.CreateName = auditUsername;
        await context.SaveChangesAsync(cancellationToken);
        task = await LoadTaskAsync(task.Id, cancellationToken);

        ValidateTaskSource(task, source, sequence);
        var expectedRemark = CreateTaskRemark(sequence);
        if (task.Remark != expectedRemark)
        {
            if (task.Remark != source.Remark
                || task.CreateBy != auditUserId
                || task.CreateName != auditUsername
                || task.DeliveryStatus == DeliveryTaskStatus.Signed)
            {
                throw new InvalidOperationException(
                    $"受管来源 {source.Remark} 已存在无法确认归属的配送任务，拒绝覆盖其备注或履约状态。");
            }

            trackedTask = await context.DeliveryTasks.SingleAsync(
                item => item.Id == task.Id,
                cancellationToken);
            trackedTask.Remark = expectedRemark;
            trackedTask.UpdateBy = auditUserId;
            trackedTask.UpdateName = auditUsername;
            await context.SaveChangesAsync(cancellationToken);
            task = await LoadTaskAsync(task.Id, cancellationToken);
        }

        if (task.DeliveryStatus == DeliveryTaskStatus.PendingAssign)
        {
            await deliveryTaskService.AssignDriverAsync(new AssignDeliveryDriverDto
            {
                TaskIds = [task.Id],
                DriverId = driver.Id
            });
            task = await LoadTaskAsync(task.Id, cancellationToken);
        }

        if (task.DriverId != driver.Id || task.CarrierId != driver.CarrierId)
        {
            throw new InvalidOperationException(
                $"受管配送异常任务 {expectedRemark} 的司机或承运商已漂移。");
        }

        if (task.DeliveryStatus == DeliveryTaskStatus.Assigned && !task.RouteId.HasValue)
        {
            await deliveryTaskService.IntelligentPlanAsync(new IntelligentPlanDeliveryTasksDto
            {
                TaskIds = [task.Id]
            });
            task = await LoadTaskAsync(task.Id, cancellationToken);
        }

        if (task.RouteId != route.Id)
        {
            throw new InvalidOperationException(
                $"受管配送异常任务 {expectedRemark} 的配送路线已漂移。");
        }

        if (task.DeliveryStatus == DeliveryTaskStatus.Assigned)
        {
            await deliveryTaskService.StartDeliveryAsync(task.Id);
            task = await LoadTaskAsync(task.Id, cancellationToken);
        }

        if (task.DeliveryStatus is not (DeliveryTaskStatus.Delivering or DeliveryTaskStatus.Exception))
        {
            throw new InvalidOperationException(
                $"受管配送异常任务 {expectedRemark} 当前状态为 {task.DeliveryStatus}，不能安全生成异常。");
        }

        ValidateTaskSource(task, source, sequence);
        return task;
    }

    private async Task EnsureNoUnmanagedTaskExceptionsAsync(
        IReadOnlyCollection<DeliveryTask> tasks,
        IReadOnlyCollection<string> expectedDescriptions,
        CancellationToken cancellationToken)
    {
        var taskIds = tasks.Select(task => task.Id).ToArray();
        var unexpected = await context.DeliveryExceptions
            .AsNoTracking()
            .Where(exception => exception.DeliveryTaskId.HasValue
                                && taskIds.Contains(exception.DeliveryTaskId.Value)
                                && !expectedDescriptions.Contains(exception.Description))
            .Select(exception => exception.ExceptionNo)
            .OrderBy(exceptionNo => exceptionNo)
            .ToArrayAsync(cancellationToken);
        if (unexpected.Length > 0)
        {
            throw new InvalidOperationException(
                $"受管配送异常任务已存在非生成器管理的异常：{string.Join("、", unexpected)}，拒绝修改任务状态。");
        }
    }

    private async Task<DeliveryTask> LoadTaskAsync(Guid taskId, CancellationToken cancellationToken)
    {
        return await context.DeliveryTasks
            .AsNoTracking()
            .SingleAsync(task => task.Id == taskId, cancellationToken);
    }

    private async Task<DeliveryException> LoadExceptionAsync(
        Guid exceptionId,
        CancellationToken cancellationToken)
    {
        return await context.DeliveryExceptions
            .AsNoTracking()
            .SingleAsync(exception => exception.Id == exceptionId, cancellationToken);
    }

    private void ValidateTaskSource(DeliveryTask task, StockOutOrder source, int sequence)
    {
        if (task.StockOutOrderId != source.Id
            || task.SaleOrderId != source.SaleOrderId
            || task.CustomerId != source.CustomerId
            || task.CustomerNameSnapshot != source.CustomerNameSnapshot
            || task.WareId != source.WareId
            || task.WareNameSnapshot != source.WareNameSnapshot
            || task.OutTime != source.OutTime
            || string.IsNullOrWhiteSpace(task.TaskNo)
            || task.CreateTime is null
            || task.CreateBy != auditUserId
            || task.CreateName != auditUsername
            || task.Status != Status.Enable)
        {
            throw new InvalidOperationException(
                $"受管配送异常任务 {CreateTaskRemark(sequence)} 的出库来源、业务快照或创建审计已漂移。");
        }
    }

    private static void ValidateTaskFinalState(
        DeliveryTask task,
        int sequence,
        bool shouldBeHandled)
    {
        var expectedStatus = shouldBeHandled
            ? DeliveryTaskStatus.Delivering
            : DeliveryTaskStatus.Exception;
        if (task.Remark != CreateTaskRemark(sequence)
            || task.DeliveryStatus != expectedStatus
            || !task.DriverId.HasValue
            || !task.CarrierId.HasValue
            || !task.RouteId.HasValue
            || !task.AssignedTime.HasValue
            || !task.PlannedTime.HasValue
            || !task.StartedTime.HasValue
            || task.SignedTime.HasValue
            || task.UpdateTime is null
            || task.UpdateBy is null
            || string.IsNullOrWhiteSpace(task.UpdateName))
        {
            throw new InvalidOperationException(
                $"受管配送异常任务 {CreateTaskRemark(sequence)} 的履约状态、时间或更新审计已漂移。");
        }
    }

    private void ValidateException(
        DeliveryException deliveryException,
        DeliveryTask task,
        int sequence,
        bool shouldBeHandled)
    {
        var expectedStatus = shouldBeHandled
            ? DeliveryExceptionStatus.Handled
            : DeliveryExceptionStatus.Pending;
        var expectedHandleRemark = shouldBeHandled ? CreateHandleRemark(sequence) : null;
        if (deliveryException.Description != CreateExceptionDescription(sequence)
            || deliveryException.DeliveryTaskId != task.Id
            || deliveryException.DriverId != task.DriverId
            || deliveryException.CustomerId != task.CustomerId
            || string.IsNullOrWhiteSpace(deliveryException.ExceptionNo)
            || deliveryException.HandleStatus != expectedStatus
            || deliveryException.HandleRemark != expectedHandleRemark
            || deliveryException.HandleTime.HasValue != shouldBeHandled
            || deliveryException.CreateTime is null
            || deliveryException.CreateBy != auditUserId
            || deliveryException.CreateName != auditUsername
            || deliveryException.Status != Status.Enable)
        {
            throw new InvalidOperationException(
                $"受管配送异常 {CreateExceptionDescription(sequence)} 的任务关联、状态、处理结果或创建审计已漂移。");
        }

        if (shouldBeHandled
            && (deliveryException.UpdateTime is null
                || deliveryException.UpdateBy != auditUserId
                || deliveryException.UpdateName != auditUsername
                || deliveryException.HandleTime < deliveryException.CreateTime))
        {
            throw new InvalidOperationException(
                $"受管配送异常 {CreateExceptionDescription(sequence)} 的处理时间或更新审计已漂移。");
        }

        if (!shouldBeHandled
            && (deliveryException.UpdateBy.HasValue
                || deliveryException.UpdateName is not null
                || deliveryException.HandleTime.HasValue))
        {
            throw new InvalidOperationException(
                $"待处理受管配送异常 {CreateExceptionDescription(sequence)} 不应伪造处理事实。");
        }
    }

    private static void EnsureExactKeys(
        IEnumerable<string> actualKeys,
        IReadOnlyCollection<string> expectedKeys,
        string keyName)
    {
        var actual = actualKeys.ToArray();
        var duplicates = actual
            .GroupBy(key => key, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (duplicates.Length > 0)
        {
            throw new InvalidOperationException(
                $"检测到重复受管{keyName}：{string.Join("、", duplicates)}。");
        }

        var unknown = actual
            .Where(key => !expectedKeys.Contains(key, StringComparer.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (unknown.Length > 0)
        {
            throw new InvalidOperationException(
                $"检测到未知受管{keyName}：{string.Join("、", unknown)}。");
        }
    }

    private static string CreateTraceStockOutRemark(int sequence)
    {
        var stableKey = DemoDataStableKeyCatalog.Create("TRACE-STOCK-OUT", sequence);
        return $"{stableKey} 华东联调溯源销售出库{sequence:D2}：从已检测采购批次出库以形成真实溯源来源。";
    }

    private static string CreateTaskRemark(int sequence)
    {
        var stableKey = DemoDataStableKeyCatalog.Create("DELIVERY-EXCEPTION-TASK", sequence);
        return $"{stableKey} 华东联调异常配送任务{sequence:D2}：来源受管溯源销售出库，用于覆盖异常处理闭环。";
    }

    private static string CreateExceptionDescription(int sequence)
    {
        var stableKey = DemoDataStableKeyCatalog.Create("DELIVERY-EXCEPTION", sequence);
        var description = sequence % 2 == 0
            ? "客户验收时发现外包装轻微破损，需要调度复核交付方案。"
            : "配送途中道路临时管制，预计到达时间需要与客户重新确认。";
        return $"{stableKey} {description}";
    }

    private static string CreateHandleRemark(int sequence)
    {
        return $"华东联调异常处理{sequence:D2}：调度已联系客户和司机确认替代路线及验收安排，恢复配送。";
    }
}
