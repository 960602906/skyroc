namespace Application.DTOs.Printing;
/// <summary>确认正式打印请求。</summary>
public class ConfirmPrintDto
{ /// <summary>已完成打印的来源主键，单次最多 100 个且不重复。</summary>
    public List<Guid> Ids { get; set; } = [];
}
