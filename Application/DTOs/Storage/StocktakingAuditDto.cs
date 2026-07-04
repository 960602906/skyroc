namespace Application.DTOs.Storage;

/// <summary>
/// 库存盘点审核请求，携带写入差异调整流水的可选业务说明。
/// </summary>
public class StocktakingAuditDto
{
    /// <summary>
    /// 审核和库存调整原因说明；为空时沿用盘点单备注。
    /// </summary>
    public string? Remark { get; set; }
}
