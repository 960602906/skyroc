using Application.DTOs.Delivery;
using Application.Exceptions;
using Application.Extensions;
using Application.Interfaces;
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
    IDocumentNoGenerator documentNoGenerator,
    IValidator<CreateDeliveryExceptionDto> createValidator,
    IValidator<HandleDeliveryExceptionDto> handleValidator,
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
        await unitOfWork.ExecuteInTransactionAsync(async () =>
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
                ExceptionNo = await documentNoGenerator.NextAsync(
                    DocumentNoKind.DeliveryException,
                    no => deliveryExceptionRepository.ExistsByExceptionNoAsync(no)),
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

    /// <inheritdoc />
    public async Task<DeliveryExceptionDto> HandleAsync(Guid id, HandleDeliveryExceptionDto dto)
    {
        var validation = await handleValidator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var entity = await deliveryExceptionRepository.GetByIdForUpdateAsync(id)
                         ?? throw new NotFoundException("配送异常不存在");
            if (entity.HandleStatus != DeliveryExceptionStatus.Pending)
            {
                throw new BusinessException($"配送异常 {entity.ExceptionNo} 已处理，不能重复操作");
            }

            if (!entity.DeliveryTaskId.HasValue)
            {
                throw new BusinessException($"配送异常 {entity.ExceptionNo} 未关联配送任务");
            }

            var task = await deliveryTaskRepository.GetByIdForUpdateAsync(entity.DeliveryTaskId.Value)
                       ?? throw new BusinessException("配送异常关联的配送任务不存在");
            if (task.DeliveryStatus != DeliveryTaskStatus.Exception)
            {
                throw new BusinessException($"配送任务 {task.TaskNo} 当前状态不允许处理异常");
            }

            entity.HandleStatus = DeliveryExceptionStatus.Handled;
            entity.HandleRemark = dto.HandleRemark.Trim();
            entity.HandleTime = DateTime.UtcNow;
            entity.UpdateBy = currentUserService.GetUserId();
            entity.UpdateName = currentUserService.GetUserName();
            await deliveryExceptionRepository.UpdateAsync(entity);

            var hasOtherPending = await deliveryExceptionRepository.HasPendingExceptionsAsync(task.Id, entity.Id);
            if (!hasOtherPending)
            {
                task.DeliveryStatus = task.StartedTime.HasValue
                    ? DeliveryTaskStatus.Delivering
                    : DeliveryTaskStatus.Assigned;
                task.UpdateBy = currentUserService.GetUserId();
                task.UpdateName = currentUserService.GetUserName();
                await deliveryTaskRepository.UpdateAsync(task);
            }
        });

        logger.LogInformation("配送异常处理完成: {DeliveryExceptionId}", id);
        return await GetByIdAsync(id);
    }

}
