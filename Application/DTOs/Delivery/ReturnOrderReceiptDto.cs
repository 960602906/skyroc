namespace Application.DTOs.Delivery;

/// <summary>
/// 回单归档请求，提交纸质扫描件或电子回单地址及归档说明。
/// </summary>
public class ReturnOrderReceiptDto
{
    /// <summary>
    /// 纸质扫描件或电子回单的可访问地址。
    /// </summary>
    public string ReceiptImageUrl { get; set; } = string.Empty;

    /// <summary>
    /// 回单归档说明，例如纸质件编号或缺页补充信息。
    /// </summary>
    public string? Remark { get; set; }
}
