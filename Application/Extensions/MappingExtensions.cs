using Application.QueryParameters;
using AutoMapper;
using Common.Constants;

namespace Application.Extensions;

public static class MappingExtensions
{
    /// <summary>
    ///     将分页数据映射到 PagedResult
    /// </summary>
    private static PagedResult<TDto> ToPagedResult<TEntity, TDto>(
        this IMapper mapper,
        IEnumerable<TEntity> items,
        int totalCount,
        int pageNum,
        int pageSize) where TEntity : class where TDto : class
    {
        return new PagedResult<TDto>
        {
            Records = mapper.Map<List<TDto>>(items),
            Total = totalCount,
            Current = pageNum,
            Size = pageSize
        };
    }

    /// <summary>
    ///     重载：直接从 Tuple 映射
    /// </summary>
    public static PagedResult<TDto> ToPagedResult<TEntity, TDto>(
        this IMapper mapper,
        (IEnumerable<TEntity> items, int total) pagedData,
        int pageNum,
        int pageSize)
        where TEntity : class
        where TDto : class
    {
        return mapper.ToPagedResult<TEntity, TDto>(pagedData.items, pagedData.total, pageNum, pageSize);
    }

    /// <summary>
    ///     从查询参数对象直接映射（最简洁）
    /// </summary>
    public static PagedResult<TDto> ToPagedResult<TEntity, TDto>(
        this IMapper mapper,
        (IEnumerable<TEntity> items, int total) pagedData,
        PagedQueryParameters parameters)
        where TEntity : class
        where TDto : class
    {
        return mapper.ToPagedResult<TEntity, TDto>(
            pagedData.items,
            pagedData.total,
            parameters.Current,
            parameters.Size);
    }
}