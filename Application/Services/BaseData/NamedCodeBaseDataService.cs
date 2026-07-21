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
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Shared.Constants;
using ValidationException = Application.Exceptions.ValidationException;

namespace Application.Services;

/// <summary>
///     带名称和编码唯一校验的基础资料服务基类。
/// </summary>
public abstract class NamedCodeBaseDataService<TEntity, TDto, TCreateDto, TUpdateDto, TQuery>(
    INamedCodeRepository<TEntity> repository,
    IUnitOfWork unitOfWork,
    ILogger logger,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IValidator<TCreateDto> createValidator,
    IValidator<TUpdateDto> updateValidator)
    : BaseDataService<TEntity, TDto, TCreateDto, TUpdateDto, TQuery>(
        repository,
        unitOfWork,
        logger,
        mapper,
        currentUserService,
        createValidator,
        updateValidator),
        INamedCodeBaseDataService<TDto, TCreateDto, TUpdateDto, TQuery>
    where TEntity : BaseEntity
    where TDto : class
    where TCreateDto : INamedCodeInput
    where TUpdateDto : IUpdateInput
    where TQuery : PagedQueryParameters
{
    /// <inheritdoc />
    protected INamedCodeRepository<TEntity> NamedCodeRepository { get; } = repository;

    /// <inheritdoc />
    public async Task<List<NamedCodeOptionDto>> GetOptionsAsync()
    {
        var options = await NamedCodeRepository.GetOptionsAsync();
        return Mapper.Map<List<NamedCodeOptionDto>>(options);
    }

    /// <inheritdoc />
    public async Task<SelectionOptionSearchResultDto> SearchSelectionOptionsAsync(
        SelectionOptionSearchQueryParameters parameters)
    {
        var options = await NamedCodeRepository.SearchSelectionOptionsAsync(
            parameters.Keyword,
            parameters.Limit + 1);
        var hasMore = options.Count > parameters.Limit;
        if (hasMore)
        {
            options.RemoveAt(options.Count - 1);
        }

        return new SelectionOptionSearchResultDto
        {
            Items = Mapper.Map<List<SelectionOptionDto>>(options),
            HasMore = hasMore
        };
    }

    /// <inheritdoc />
    public async Task<List<SelectionOptionDto>> ResolveSelectionOptionsAsync(IReadOnlyCollection<Guid> ids)
    {
        var distinctIds = ids.Where(x => x != Guid.Empty).Distinct().ToArray();
        if (distinctIds.Length > SelectionOptionConstants.MaxResolveCount)
        {
            throw new ValidationException([
                new ValidationFailure(nameof(ids), $"单次最多解析 {SelectionOptionConstants.MaxResolveCount} 个选择项")
            ]);
        }

        var options = await NamedCodeRepository.ResolveSelectionOptionsAsync(distinctIds);
        return Mapper.Map<List<SelectionOptionDto>>(options);
    }

    /// <inheritdoc />
    public async Task<List<SelectionOptionDto>> GetBoundedSelectionOptionsAsync()
    {
        var options = await NamedCodeRepository.GetBoundedSelectionOptionsAsync(
            SelectionOptionConstants.MaxBoundedCount + 1);
        if (options.Count > SelectionOptionConstants.MaxBoundedCount)
        {
            throw new ValidationException([
                new ValidationFailure(
                    "Options",
                    $"{DisplayName}选项超过 {SelectionOptionConstants.MaxBoundedCount} 条，请改用远程搜索")
            ]);
        }

        return Mapper.Map<List<SelectionOptionDto>>(options);
    }

    /// <inheritdoc />
    protected override async Task ValidateCreateAsync(TCreateDto dto)
    {
        if (await NamedCodeRepository.ExistsByCodeAsync(dto.Code!))
        {
            throw new BusinessException($"{DisplayName}编码已经存在");
        }

        if (await NamedCodeRepository.ExistsByNameAsync(dto.Name!))
        {
            throw new BusinessException($"{DisplayName}名称已经存在");
        }
    }

    /// <inheritdoc />
    protected override async Task ValidateUpdateAsync(Guid id, TUpdateDto dto)
    {
        if (await NamedCodeRepository.ExistsByCodeAsync(dto.Code!, id))
        {
            throw new BusinessException($"{DisplayName}编码已经存在");
        }

        if (await NamedCodeRepository.ExistsByNameAsync(dto.Name!, id))
        {
            throw new BusinessException($"{DisplayName}名称已经存在");
        }
    }
}
