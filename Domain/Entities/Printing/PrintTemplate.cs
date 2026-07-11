namespace Domain.Entities.Printing;

/// <summary>
/// 打印模板主表，保存前端设计器 JSON 与其可使用的业务字段定义。
/// </summary>
public class PrintTemplate : BaseEntity
{
    /// <summary>模板稳定业务编码，同一编码在全部业务类型中唯一。</summary>
    public string TemplateCode { get; set; } = string.Empty;

    /// <summary>供管理员选择和展示的模板名称。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>模板适用的单据类型，打印数据类型必须与其一致。</summary>
    public PrintBusinessType BusinessType { get; set; }

    /// <summary>前端打印设计器保存的 JSON 配置，不由后端解释或渲染。</summary>
    public string DesignJson { get; set; } = string.Empty;

    /// <summary>模板是否可被业务打印选择；停用模板保留历史设计但不提供给调用方。</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>模板中允许绑定的字段集合，按显示顺序组织。</summary>
    public virtual ICollection<PrintTemplateField> Fields { get; set; } = new List<PrintTemplateField>();
}
