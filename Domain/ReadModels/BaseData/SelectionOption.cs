namespace Domain.ReadModels.BaseData;

/// <summary>
///     数据库侧轻量选择项投影，只承载选择控件展示所需字段。
/// </summary>
public class SelectionOption
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
