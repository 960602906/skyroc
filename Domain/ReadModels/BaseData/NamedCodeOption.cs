namespace Domain.ReadModels.BaseData;

/// <summary>
/// 带名称和编码的基础资料下拉选项投影，仅承载前端选择控件所需的主键、名称和编码，不加载明细导航属性。
/// </summary>
public class NamedCodeOption
{
    /// <summary>
    /// 基础资料主键。
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 基础资料名称，用于下拉选项显示文本。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 基础资料编码，可用于选项副标题或按编码检索。
    /// </summary>
    public string Code { get; set; } = string.Empty;
}
