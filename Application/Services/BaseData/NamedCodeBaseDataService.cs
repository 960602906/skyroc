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
