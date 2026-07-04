using Application.DTOs.Delivery;
using Application.Exceptions;
using Application.Extensions;
using Application.interfaces;
using Application.QueryParameters;
using AutoMapper;
using Domain.Entities.Delivery;
using Domain.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared.Constants;
using ValidationException = Application.Exceptions.ValidationException;

namespace Application.Services;

/// <summary>
/// 配送异常应用服务，事务化登记异常并同步任务异常状态。
/// </summary>
public class DeliveryExceptionService(
    IDeliveryExceptionRepository deliveryExceptionRepository,
    IDeliveryTaskRepository deliveryTaskRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IValidator<CreateDeliveryExceptionDto> createValidator,
    ILogger<DeliveryExceptionService> logger) : IDeliveryExceptionService
{
    /// <inheritdoc />
    public async Task<PagedResult<DeliveryExceptionDto>> GetPagedAsync(
        DeliveryExceptionQueryParameters parameters)
    {
        var result = await deliveryExceptionRepository.GetPagedAsync(
            parameters.QueryBuild(),
            parameters.Current,
            parameters.Size,
            x => x.CreateTime!,
            true);
        return mapper.ToPagedResult<DeliveryException, DeliveryExceptionDto>(result, parameters);
    }

    /// <inheritdoc />
    public async Task<DeliveryExceptionDto> GetByIdAsync(Guid id)
    {
        var entity = await deliveryExceptionRepository.GetByIdAsync(id)
                     ?? throw new NotFoundException("配送异常不存在");
        return mapper.Map<DeliveryExceptionDto>(entity);
    }

    /// <inheritdoc />
    public async Task<DeliveryExceptionDto> CreateAsync(CreateDeliveryExceptionDto dto)
    {
        var validation = await createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        Guid exceptionId = Guid.Empty;
        await ExecuteInTransactionAsync(async () =>
        {
            var task = await deliveryTaskRepository.GetByIdForUpdateAsync(dto.DeliveryTaskId)
                       ?? throw new NotFoundException("配送任务不存在");
            if (!task.DriverId.HasValue)
            {
                throw new BusinessException("配送任务分配司机后才能登记异常");
            }

            if (task.DeliveryStatus is DeliveryTaskStatus.PendingAssign or DeliveryTaskStatus.Signed)
            {
                throw new BusinessException($"配送任务 {task.TaskNo} 当前状态不允许登记异常");
            }

            var entity = new DeliveryException
            {
                Id = Guid.NewGuid(),
                ExceptionNo = await GenerateExceptionNoAsync(),
                DeliveryTaskId = task.Id,
                DriverId = task.DriverId,
                CustomerId = task.CustomerId,
                Description = dto.Description.Trim(),
                HandleStatus = DeliveryExceptionStatus.Pending,
                CreateBy = currentUserService.GetUserId(),
                CreateName = currentUserService.GetUserName()
            };
            await deliveryExceptionRepository.AddAsync(entity);
            task.DeliveryStatus = DeliveryTaskStatus.Exception;
            task.UpdateBy = currentUserService.GetUserId();
            task.UpdateName = currentUserService.GetUserName();
            await deliveryTaskRepository.UpdateAsync(task);
            exceptionId = entity.Id;
        });

        logger.LogInformation("配送异常登记成功: {DeliveryExceptionId}, {DeliveryTaskId}", exceptionId, dto.DeliveryTaskId);
        return await GetByIdAsync(exceptionId);
    }

    private async Task<string> GenerateExceptionNoAsync()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var suffix = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
            var exceptionNo = $"DE{DateTime.UtcNow:yyyyMMddHHmmssfff}{suffix}";
            if (!await deliveryExceptionRepository.ExistsByExceptionNoAsync(exceptionNo))
            {
                return exceptionNo;
            }
        }

        throw new BusinessException("配送异常编号生成失败，请重试");
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
}
