using System.ComponentModel.DataAnnotations;
using Shared.Constants;

namespace Application.QueryParameters;

/// <summary>
///     限量选择项搜索参数。
/// </summary>
public class SelectionOptionSearchQueryParameters
{
    /// <summary>
    ///     名称、编码或业务单号关键词；为空时返回确定排序的首批数据。
    /// </summary>
    [StringLength(SelectionOptionConstants.MaxKeywordLength)]
    public string? Keyword { get; set; }

    /// <summary>
    ///     返回数量，默认 20，最大 50。
    /// </summary>
    [Range(1, SelectionOptionConstants.MaxSearchLimit)]
    public int Limit { get; set; } = SelectionOptionConstants.DefaultSearchLimit;
}
