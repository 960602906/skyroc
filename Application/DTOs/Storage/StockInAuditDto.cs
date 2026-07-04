namespace Application.DTOs.Storage;

/// <summary>
/// 入库单审核或反审核操作请求，携带可选业务说明。
/// </summary>
public class StockInAuditDto
{
    /// <summary>
    /// 审核或反审核原因说明；写入生成的库存流水备注。
    /// </summary>
    public string? Remark { get; set; }
}
