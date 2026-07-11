namespace Application.DTOs.Printing;
/// <summary>完整更新打印模板请求。</summary>
public class UpdatePrintTemplateDto : CreatePrintTemplateDto { /// <summary>模板主键。</summary>
public Guid Id { get; set; } }
