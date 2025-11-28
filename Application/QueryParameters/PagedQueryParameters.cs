namespace Application.QueryParameters;

/// <summary>
///     通用分页查询参数基类
/// </summary>
public abstract class PagedQueryParameters
{
    private int _pageNum = 1;
    private int _pageSize = 10;

    /// <summary>
    ///     页码（从1开始）
    /// </summary>
    public int Current
    {
        get => _pageNum;
        set => _pageNum = Math.Max(1, value);
    }

    /// <summary>
    ///     每页大小
    /// </summary>
    public int Size
    {
        get => _pageSize;
        set => _pageSize = Math.Clamp(value, 1, 100);
    }
}