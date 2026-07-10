namespace Domain.ReadModels.Reports;

/// <summary>首页客户对账只读投影。</summary>
public sealed class DashboardReconciliationReadModel
{
    /// <summary>客户账单应收净额，按系统业务币种计量。</summary>
    public decimal ReceivableAmount { get; init; }

    /// <summary>客户账单已结金额，按系统业务币种计量。</summary>
    public decimal SettledAmount { get; init; }

    /// <summary>客户账单待结金额，等于各账单非负应收余额之和，按系统业务币种计量。</summary>
    public decimal PendingAmount { get; init; }

    /// <summary>参与统计的客户账单数。</summary>
    public int BillCount { get; init; }
}
