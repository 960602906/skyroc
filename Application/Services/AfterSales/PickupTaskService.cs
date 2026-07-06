using Application.DTOs.AfterSales;
using Application.Exceptions;
using Application.Extensions;
using Application.interfaces;
using Application.QueryParameters.AfterSales;
using AutoMapper;
using Domain.Entities.AfterSales;
using Domain.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared.Constants;
using ValidationException = Application.Exceptions.ValidationException;

namespace Application.Services;

/// <summary>
/// 售后取货任务服务，事务化执行司机分配、开始和完成状态机，并暴露退货入库衔接结果。
/// </summary>
public class PickupTaskService(
    IPickupTaskRepository pickupTaskRepository,
    IDriverRepository driverRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IValidator<AssignPickupTaskDto> assignValidator,
    ILogger<PickupTaskService> logger) : IPickupTaskService
{
    /// <inheritdoc />
    public async Task<PagedResult<PickupTaskDto>> GetPagedAsync(PickupTaskQueryParameters parameters)
    {
        var result = await pickupTaskRepository.GetPagedAsync(
            parameters.QueryBuild(),
            parameters.Current,
            parameters.Size,
            x => x.CreateTime!,
            true);
        return mapper.ToPagedResult<PickupTask, PickupTaskDto>(result, parameters);
    }

    /// <inheritdoc />
    public async Task<PickupTaskDto> AssignAsync(Guid id, AssignPickupTaskDto dto)
    {
        var validation = await assignValidator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        await ExecuteInTransactionAsync(async () =>
        {
            var driver = await driverRepository.GetByIdForUpdateAsync(dto.DriverId)
                         ?? throw new BusinessException("司机不存在");
            if (driver.Status != Status.Enable)
            {
                throw new BusinessException("禁用司机不能分配取货任务");
            }

            var task = await GetRequiredForUpdateAsync(id);
            if (task.PickupStatus is not (PickupTaskStatus.PendingAssign or PickupTaskStatus.PendingPickup))
            {
                throw new BusinessException($"取货任务 {task.TaskNo} 已开始执行，不能重新分配司机");
            }

            task.DriverId = driver.Id;
            task.DriverNameSnapshot = driver.Name;
            task.DriverPhoneSnapshot = driver.Phone;
            task.PlannedPickupTime = dto.PlannedPickupTime;
            task.AssignedTime = DateTime.UtcNow;
            task.PickupStatus = PickupTaskStatus.PendingPickup;
            task.Remark = Normalize(dto.Remark);
            ApplyUpdateAudit(task);
            await pickupTaskRepository.UpdateAsync(task);
        });

        logger.LogInformation("售后取货任务分配司机成功: {PickupTaskId}, {DriverId}", id, dto.DriverId);
        return mapper.Map<PickupTaskDto>(await GetRequiredAsync(id));
    }

    /// <inheritdoc />
    public async Task<PickupTaskDto> StartAsync(Guid id)
    {
        await ExecuteInTransactionAsync(async () =>
        {
            var task = await GetRequiredForUpdateAsync(id);
            if (task.PickupStatus != PickupTaskStatus.PendingPickup || !task.DriverId.HasValue)
            {
                throw new BusinessException($"取货任务 {task.TaskNo} 必须先分配司机才能开始取货");
            }

            task.PickupStatus = PickupTaskStatus.PickingUp;
            task.StartedTime = DateTime.UtcNow;
            ApplyUpdateAudit(task);
            await pickupTaskRepository.UpdateAsync(task);
        });

        logger.LogInformation("售后取货任务开始执行: {PickupTaskId}", id);
        return mapper.Map<PickupTaskDto>(await GetRequiredAsync(id));
    }

    /// <inheritdoc />
    public async Task<PickupTaskDto> CompleteAsync(Guid id)
    {
        await ExecuteInTransactionAsync(async () =>
        {
            var task = await GetRequiredForUpdateAsync(id);
            if (task.PickupStatus != PickupTaskStatus.PickingUp)
            {
                throw new BusinessException($"取货任务 {task.TaskNo} 只有取货中状态才能完成");
            }

            task.PickupStatus = PickupTaskStatus.Completed;
            task.CompletedTime = DateTime.UtcNow;
            ApplyUpdateAudit(task);
            await pickupTaskRepository.UpdateAsync(task);
        });

        logger.LogInformation("售后取货任务完成: {PickupTaskId}", id);
        return mapper.Map<PickupTaskDto>(await GetRequiredAsync(id));
    }

    private async Task<PickupTask> GetRequiredAsync(Guid id)
    {
        return await pickupTaskRepository.GetByIdAsync(id)
               ?? throw new NotFoundException("取货任务不存在");
    }

    private async Task<PickupTask> GetRequiredForUpdateAsync(Guid id)
    {
        return await pickupTaskRepository.GetByIdForUpdateAsync(id)
               ?? throw new NotFoundException("取货任务不存在");
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

    private void ApplyUpdateAudit(PickupTask task)
    {
        task.UpdateBy = currentUserService.GetUserId();
        task.UpdateName = currentUserService.GetUserName();
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
