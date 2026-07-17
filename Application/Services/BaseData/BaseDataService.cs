using System.Linq.Expressions;
using Application.DTOs;
using Application.Exceptions;
using Application.Extensions;
using Application.Interfaces;
using Application.QueryParameters;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared.Constants;
using ValidationException = Application.Exceptions.ValidationException;

namespace Application.Services;

/// <summary>
///     基础资料应用服务基类。
/// </summary>
public abstract class BaseDataService<TEntity, TDto, TCreateDto, TUpdateDto, TQuery>(
    IRepository<TEntity> repository,
    IUnitOfWork unitOfWork,
    ILogger logger,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IValidator<TCreateDto> createValidator,
    IValidator<TUpdateDto> updateValidator)
    : IBaseDataService<TDto, TCreateDto, TUpdateDto, TQuery>
    where TEntity : BaseEntity
    where TDto : class
    where TUpdateDto : IHasId
    where TQuery : PagedQueryParameters
{
    /// <summary>
    /// 当前基础资料实体的持久化仓储。
    /// </summary>
    protected IRepository<TEntity> Repository { get; } = repository;

    /// <summary>
    /// 提交跨仓储事务的工作单元。
    /// </summary>
    protected IUnitOfWork UnitOfWork { get; } = unitOfWork;

    /// <summary>
    /// 实体与应用 DTO 之间的映射器。
    /// </summary>
    protected IMapper Mapper { get; } = mapper;

    /// <summary>
    /// 提供当前登录用户的审计身份。
    /// </summary>
    protected ICurrentUserService CurrentUserService { get; } = currentUserService;

    /// <summary>
    /// 记录基础资料业务操作和异常。
    /// </summary>
    protected ILogger Logger { get; } = logger;

    /// <summary>
    /// 当前基础资料在错误消息和日志中的业务名称。
    /// </summary>
    protected abstract string DisplayName { get; }

    /// <summary>
    /// 根据查询参数构建可由数据库执行的筛选表达式。
    /// </summary>
    protected abstract Expression<Func<TEntity, bool>> BuildPredicate(TQuery parameters);

    /// <inheritdoc />
    public virtual async Task<PagedResult<TDto>> GetPagedAsync(TQuery parameters)
    {
        var pageData = await Repository.GetPagedAsync(
            BuildPredicate(parameters),
            parameters.Current,
            parameters.Size,
            x => x.CreateTime ?? DateTime.MinValue,
            true);
        return Mapper.ToPagedResult<TEntity, TDto>(pageData, parameters);
    }

    /// <inheritdoc />
    public virtual async Task<List<TDto>> GetAllAsync()
    {
        var entities = await Repository.GetAllAsync();
        return Mapper.Map<List<TDto>>(entities);
    }

    /// <inheritdoc />
    public virtual async Task<TDto> GetByIdAsync(Guid id)
    {
        var entity = await Repository.GetByIdAsync(id);
        if (entity is null)
        {
            throw new NotFoundException($"{DisplayName}不存在");
        }

        return Mapper.Map<TDto>(entity);
    }

    /// <inheritdoc />
    public virtual async Task<TDto> CreateAsync(TCreateDto dto)
    {
        var validationResult = await createValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        await ValidateCreateAsync(dto);
        var entity = Mapper.Map<TEntity>(dto);
        if (entity.Id == Guid.Empty)
        {
            entity.Id = Guid.NewGuid();
        }

        entity.ApplyCreateAudit(CurrentUserService);
        await Repository.AddAsync(entity);
        await AfterCreateAsync(entity, dto);
        await UnitOfWork.SaveChangesAsync();
        Logger.LogInformation("{DisplayName}创建成功: {Id}", DisplayName, entity.Id);
        return Mapper.Map<TDto>(entity);
    }

    /// <inheritdoc />
    public virtual async Task<TDto> UpdateAsync(Guid id, TUpdateDto dto)
    {
        var validationResult = await updateValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        if (dto.Id != id)
        {
            throw new BusinessException("请求 ID 与数据 ID 不一致");
        }

        await ValidateUpdateAsync(id, dto);
        var entity = await Repository.GetByIdAsync(id);
        if (entity is null)
        {
            throw new NotFoundException($"{DisplayName}不存在");
        }

        Mapper.Map(dto, entity);
        entity.ApplyUpdateAudit(CurrentUserService);
        await Repository.UpdateAsync(entity);
        await AfterUpdateAsync(entity, dto);
        await UnitOfWork.SaveChangesAsync();
        Logger.LogInformation("{DisplayName}更新成功: {Id}", DisplayName, id);
        return Mapper.Map<TDto>(entity);
    }

    /// <inheritdoc />
    public virtual async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await Repository.GetByIdAsync(id);
        if (entity is null)
        {
            throw new NotFoundException($"{DisplayName}不存在");
        }

        await ValidateDeleteAsync(id);
        await Repository.DeleteAsync(entity);
        await UnitOfWork.SaveChangesAsync();
        Logger.LogInformation("{DisplayName}删除成功: {Id}", DisplayName, id);
        return true;
    }

    /// <inheritdoc />
    public virtual async Task<bool> BatchDeleteAsync(List<Guid> ids)
    {
        if (ids.Count == 0)
        {
            throw new BusinessException($"请选择要删除的{DisplayName}");
        }

        foreach (var id in ids.Distinct())
        {
            await ValidateDeleteAsync(id);
        }

        await Repository.DeleteRangeAsync(ids.Distinct());
        await UnitOfWork.SaveChangesAsync();
        Logger.LogInformation("{DisplayName}批量删除成功: {Count}", DisplayName, ids.Count);
        return true;
    }

    /// <inheritdoc />
    public virtual async Task<TDto> ToggleStatusAsync(Guid id, Status status)
    {
        var entity = await Repository.GetByIdAsync(id);
        if (entity is null)
        {
            throw new NotFoundException($"{DisplayName}不存在");
        }

        entity.Status = status;
        entity.ApplyUpdateAudit(CurrentUserService);
        await Repository.UpdateAsync(entity);
        await UnitOfWork.SaveChangesAsync();
        return Mapper.Map<TDto>(entity);
    }

    /// <summary>
    /// 在创建实体前执行具体业务校验。
    /// </summary>
    protected virtual Task ValidateCreateAsync(TCreateDto dto)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 在更新实体前执行具体业务校验。
    /// </summary>
    protected virtual Task ValidateUpdateAsync(Guid id, TUpdateDto dto)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 在删除实体前校验关联关系和业务约束。
    /// </summary>
    protected virtual Task ValidateDeleteAsync(Guid id)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 在主实体创建后维护该业务拥有的关联数据。
    /// </summary>
    protected virtual Task AfterCreateAsync(TEntity entity, TCreateDto dto)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 在主实体更新后同步该业务拥有的关联数据。
    /// </summary>
    protected virtual Task AfterUpdateAsync(TEntity entity, TUpdateDto dto)
    {
        return Task.CompletedTask;
    }


}
