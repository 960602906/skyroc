using Domain.Entities.Printing;
namespace Application.DTOs.Printing;
/// <summary>新增打印模板请求。</summary>
public class CreatePrintTemplateDto
{ /// <summary>全局唯一模板编码。</summary>
    public string TemplateCode { get; set; } = string.Empty; /// <summary>模板名称。</summary>
    public string Name { get; set; } = string.Empty; /// <summary>适用单据类型。</summary>
    public PrintBusinessType BusinessType { get; set; } /// <summary>设计器 JSON。</summary>
    public string DesignJson { get; set; } = string.Empty; /// <summary>是否立即启用。</summary>
    public bool IsEnabled { get; set; } = true; /// <summary>字段定义。</summary>
    public List<PrintTemplateFieldInputDto> Fields { get; set; } = [];
}
