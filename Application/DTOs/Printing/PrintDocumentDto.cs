using Domain.Entities.Printing;

namespace Application.DTOs.Printing;

/// <summary>通用打印单据数据。</summary>
public class PrintDocumentDto
{
    /// <summary>来源主键。</summary>
    public Guid Id { get; set; }

    /// <summary>单据类型。</summary>
    public PrintBusinessType BusinessType { get; set; }

    /// <summary>单据编号。</summary>
    public string DocumentNo { get; set; } = string.Empty;

    /// <summary>业务对象名称。</summary>
    public string? BusinessPartyName { get; set; }

    /// <summary>业务日期（UTC）。</summary>
    public DateTime BusinessTime { get; set; }

    /// <summary>金额合计，系统业务币种。</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>单据备注。</summary>
    public string? Remark { get; set; }

    /// <summary>商品或核销明细。</summary>
    public List<PrintDocumentDetailDto> Details { get; set; } = [];
}
