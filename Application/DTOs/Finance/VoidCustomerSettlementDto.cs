namespace Application.DTOs.Finance;

/// <summary>
/// 作废客户结款凭证请求，用于记录必须填写的回滚原因。
/// </summary>
public class VoidCustomerSettlementDto
{
    /// <summary>作废原因，说明凭证回滚背景和人工处理依据。</summary>
    public string Remark { get; set; } = string.Empty;
}
