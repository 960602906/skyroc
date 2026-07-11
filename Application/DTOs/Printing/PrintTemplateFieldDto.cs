namespace Application.DTOs.Printing;
/// <summary>打印模板字段响应。</summary>
public class PrintTemplateFieldDto { /// <summary>字段主键。</summary>
public Guid Id { get; set; } /// <summary>稳定字段路径。</summary>
public string FieldKey { get; set; } = string.Empty; /// <summary>显示名称。</summary>
public string DisplayName { get; set; } = string.Empty; /// <summary>显示顺序。</summary>
public int DisplayOrder { get; set; } /// <summary>可选格式提示。</summary>
public string? Format { get; set; } }
