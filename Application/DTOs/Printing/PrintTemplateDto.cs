using Domain.Entities.Printing;
namespace Application.DTOs.Printing;
/// <summary>打印模板响应。</summary>
public class PrintTemplateDto { /// <summary>模板主键。</summary>
public Guid Id { get; set; } /// <summary>稳定编码。</summary>
public string TemplateCode { get; set; } = string.Empty; /// <summary>模板名称。</summary>
public string Name { get; set; } = string.Empty; /// <summary>适用单据类型。</summary>
public PrintBusinessType BusinessType { get; set; } /// <summary>设计器 JSON。</summary>
public string DesignJson { get; set; } = string.Empty; /// <summary>是否启用。</summary>
public bool IsEnabled { get; set; } /// <summary>可绑定字段。</summary>
public List<PrintTemplateFieldDto> Fields { get; set; } = []; }
