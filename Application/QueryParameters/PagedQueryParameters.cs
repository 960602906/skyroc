using Shared.Constants;

namespace Application.QueryParameters;

/// <summary>
///     通用分页查询参数基类
/// </summary>
public abstract class PagedQueryParameters
{
    private int _pageNum = PagingConstants.DefaultPageNumber;
    private int _pageSize = PagingConstants.DefaultPageSize;

    /// <summary>
    ///     页码（从1开始）
    /// </summary>
    public int Current
    {
        get => _pageNum;
        set => _pageNum = Math.Max(PagingConstants.DefaultPageNumber, value);
    }

    /// <summary>
    ///     每页大小
    /// </summary>
    public int Size
    {
        get => _pageSize;
        set => _pageSize = Math.Clamp(value, PagingConstants.DefaultPageNumber, PagingConstants.MaxPageSize);
    }
}
