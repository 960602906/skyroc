namespace Application.DTOs.Finance;

/// <summary>
/// 作废供应商结算单请求，必须填写作废原因。
/// </summary>
public class VoidSupplierSettlementDto
{
    /// <summary>作废原因，记录回滚说明或异常处理背景。</summary>
    public string Remark { get; set; } = string.Empty;
}
