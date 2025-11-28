namespace Common.Constants;

/// <summary>
///     分页结果
/// </summary>
public class PagedResult<T>
{
    /// <summary>
    ///     数据列表
    /// </summary>
    public List<T>? Records { get; set; }

    /// <summary>
    ///     总记录数
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    ///     当前页码
    /// </summary>
    public int Current { get; set; }

    /// <summary>
    ///     每页大小
    /// </summary>
    public int Size { get; set; }
}