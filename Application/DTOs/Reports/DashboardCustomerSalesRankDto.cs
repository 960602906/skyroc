namespace Application.DTOs.Reports;

/// <summary>
/// 首页客户销售排行响应，按已签收订单的客户验收金额降序排列。
/// </summary>
public class DashboardCustomerSalesRankDto
{
    /// <summary>客户主键。</summary>
    public Guid CustomerId { get; set; }

    /// <summary>订单生成时的客户名称快照。</summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>客户验收后的销售金额，按系统业务币种计量。</summary>
    public decimal SaleAmount { get; set; }

    /// <summary>参与排行的已签收订单数。</summary>
    public int OrderCount { get; set; }
}
