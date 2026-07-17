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
///     树形基础资料服务基类。
/// </summary>
public abstract class TreeBaseDataService<TEntity, TDto, TCreateDto, TUpdateDto, TQuery>(
    ITreeBaseDataRepository<TEntity> repository,
    IUnitOfWork unitOfWork,
    ILogger logger,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IValidator<TCreateDto> createValidator,
    IValidator<TUpdateDto> updateValidator)
    : NamedCodeBaseDataService<TEntity, TDto, TCreateDto, TUpdateDto, TQuery>(
            repository,
            unitOfWork,
            logger,
            mapper,
            currentUserService,
            createValidator,
            updateValidator),
        ITreeBaseDataService<TDto, TCreateDto, TUpdateDto, TQuery>
    where TEntity : BaseEntity
    where TDto : class, ITreeNodeDto<TDto>
    where TCreateDto : INamedCodeInput
    where TUpdateDto : IUpdateInput
    where TQuery : PagedQueryParameters
{
    private readonly ITreeBaseDataRepository<TEntity> _treeRepository = repository;

    /// <inheritdoc />
    public async Task<List<TDto>> GetTreeAsync()
    {
        var entities = await _treeRepository.GetAllTreeSourceAsync();
        var dtos = Mapper.Map<List<TDto>>(entities);
        return BuildTree(dtos);
    }

    /// <inheritdoc />
    protected override async Task ValidateDeleteAsync(Guid id)
    {
        if (await _treeRepository.HasChildrenAsync(id))
        {
            throw new BusinessException($"{DisplayName}下还有子级，不能删除");
        }
    }

    private static List<TDto> BuildTree(List<TDto> nodes)
    {
        var map = nodes.ToDictionary(x => x.Id);
        foreach (var node in nodes)
        {
            if (!node.ParentId.HasValue || !map.TryGetValue(node.ParentId.Value, out var parent))
            {
                continue;
            }

            parent.Children ??= [];
            parent.Children.Add(node);
        }

        return nodes
            .Where(x => x.ParentId is null || !map.ContainsKey(x.ParentId.Value))
            .OrderBy(x => x.Sort)
            .ToList();
    }
}
