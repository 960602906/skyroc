namespace Application.DTOs;

/// <summary>
///     通用轻量选择项，不包含业务详情和导航数据。
/// </summary>
public class SelectionOptionDto
{
    /// <summary>
    ///     被选择业务记录的主键。
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     选择项的主要显示文本。
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    ///     编码、客户名称等辅助识别文本。
    /// </summary>
    public string? SecondaryText { get; set; }
}
