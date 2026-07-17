namespace Application.DTOs;

/// <summary>
///     带名称和编码的基础资料下拉选项 DTO，仅包含前端选择控件所需的主键、名称和编码。
/// </summary>
public class NamedCodeOptionDto
{
    /// <summary>
    ///     基础资料主键。
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     基础资料名称，用作下拉选项显示文本。
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///     基础资料编码，可用于选项副标题或按编码检索。
    /// </summary>
    public string? Code { get; set; }
}
