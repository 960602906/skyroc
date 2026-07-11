namespace Application.DTOs.Printing;
/// <summary>打印单据行数据。</summary>
public class PrintDocumentDetailDto { /// <summary>商品或账单名称。</summary>
public string ItemName { get; set; } = string.Empty; /// <summary>商品或来源编号。</summary>
public string? ItemCode { get; set; } /// <summary>商品单位。</summary>
public string? UnitName { get; set; } /// <summary>数量。</summary>
public decimal Quantity { get; set; } /// <summary>单价或核销金额。</summary>
public decimal UnitPrice { get; set; } /// <summary>行金额。</summary>
public decimal TotalPrice { get; set; } /// <summary>行备注。</summary>
public string? Remark { get; set; } }
