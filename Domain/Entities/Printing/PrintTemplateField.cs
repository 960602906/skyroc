namespace Domain.Entities.Printing;

/// <summary>
/// 打印模板字段定义，描述设计器可绑定的稳定数据路径及显示语义。
/// </summary>
public class PrintTemplateField : BaseEntity
{
    /// <summary>所属打印模板主键。</summary>
    public Guid PrintTemplateId { get; set; }

    /// <summary>与打印数据 JSON 对应的字段路径，例如 documentNo 或 details[].itemName。</summary>
    public string FieldKey { get; set; } = string.Empty;

    /// <summary>供模板设计器展示的业务字段名称。</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>字段在设计器字段面板中的升序显示位置，从零开始。</summary>
    public int DisplayOrder { get; set; }

    /// <summary>可选格式提示，例如 yyyy-MM-dd 或 0.00；由前端渲染器解释。</summary>
    public string? Format { get; set; }

    /// <summary>所属打印模板。</summary>
    public virtual PrintTemplate PrintTemplate { get; set; } = null!;
}
