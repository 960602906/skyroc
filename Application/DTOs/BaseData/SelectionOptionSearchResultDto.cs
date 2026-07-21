namespace Application.DTOs;

/// <summary>
///     限量选择项搜索结果，不执行总数统计。
/// </summary>
public class SelectionOptionSearchResultDto
{
    /// <summary>
    ///     当前关键词命中的前若干个轻量选择项。
    /// </summary>
    public List<SelectionOptionDto> Items { get; set; } = [];

    /// <summary>
    ///     是否仍有未返回的匹配项；为真时前端应提示继续输入关键词。
    /// </summary>
    public bool HasMore { get; set; }
}
