namespace Application.DTOs.Reports;

/// <summary>
/// 首页客户对账汇总响应，按客户账单业务日期聚合应收、已结和待结金额。
/// </summary>
public class DashboardReconciliationDto
{
    /// <summary>客户账单应收金额，包含售后调整后的净额，按系统业务币种计量。</summary>
    public decimal ReceivableAmount { get; set; }

    /// <summary>客户账单已结金额，按系统业务币种计量。</summary>
    public decimal SettledAmount { get; set; }

    /// <summary>客户账单待结金额，等于各账单非负应收余额之和，按系统业务币种计量。</summary>
    public decimal PendingAmount { get; set; }

    /// <summary>参与统计的客户账单数。</summary>
    public int BillCount { get; set; }
}
