namespace Application.DTOs.AfterSales;

/// <summary>
/// 售后审核操作请求，用于记录审核意见、驳回原因或反审核说明。
/// </summary>
public class AfterSaleActionDto
{
    /// <summary>操作说明；驳回和反审核时必填，最长 500 字符。</summary>
    public string? Remark { get; set; }
}
